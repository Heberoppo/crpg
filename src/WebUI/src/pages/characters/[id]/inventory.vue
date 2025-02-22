<script setup lang="ts">
import { UseDraggable as Draggable } from '@vueuse/components';

import { type UserItemsBySlot } from '@/models/user';
import type { EquippedItemId } from '@/models/character';
import { type ItemFlat, ItemType, WeaponClass } from '@/models/item';
import { AggregationConfig, AggregationView, SortingConfig } from '@/models/item-search';
import { extractItemFromUserItem } from '@/services/users-service';
import { updateCharacterItems, checkUpkeepIsHigh } from '@/services/characters-service';
import { useUserStore } from '@/stores/user';
import { sellUserItem, repairUserItem, upgradeUserItem } from '@/services/users-service';
import { getCompareItemsResult } from '@/services/item-service';
import { createItemIndex } from '@/services/item-search-service/indexator';
import { getSearchResult, getAggregationsConfig } from '@/services/item-search-service';
import { notify } from '@/services/notification-service';
import { t } from '@/services/translate-service';
import { scrollToTop } from '@/utils/scroll';
import { useItemDetail } from '@/composables/character/use-item-detail';
import { useStickySidebar } from '@/composables/use-sticky-sidebar';

import {
  characterKey,
  characterCharacteristicsKey,
  characterHealthPointsKey,
  characterItemsKey,
  characterItemsStatsKey,
  equippedItemsBySlotKey,
} from '@/symbols/character';
import { mainHeaderHeightKey } from '@/symbols/common';

definePage({
  props: true,
  meta: {
    layout: 'default',
    roles: ['User', 'Moderator', 'Admin'],
  },
});

const userStore = useUserStore();

const character = injectStrict(characterKey);
const { characterCharacteristics } = injectStrict(characterCharacteristicsKey);
const healthPoints = injectStrict(characterHealthPointsKey);
const { characterItems, loadCharacterItems } = injectStrict(characterItemsKey);
const itemsStats = injectStrict(characterItemsStatsKey);
const mainHeaderHeight = injectStrict(mainHeaderHeightKey);

const upkeepIsHigh = computed(() =>
  checkUpkeepIsHigh(userStore.user!.gold, itemsStats.value.averageRepairCostByHour)
);
const equippedItemsIds = computed(() => characterItems.value.map(ei => ei.userItem.id));

const changeEquippedItems = async (items: EquippedItemId[]) => {
  await updateCharacterItems(character.value.id, items);
  await loadCharacterItems(0, { id: character.value.id });
};

const onSellUserItem = async (itemId: number) => {
  // if the item sold is the last item in the active category,
  // you must reset the filter because that category is no longer in inventory
  if (filteredUserItems.value.length === 1) {
    filterByTypeModel.value = [];
  }
  await sellUserItem(itemId);
  await Promise.all([
    userStore.fetchUser(),
    userStore.fetchUserItems(),
    loadCharacterItems(0, { id: character.value.id }),
  ]);

  notify(t('character.inventory.item.sell.notify.success'));
};

const onRepairUserItem = async (itemId: number) => {
  await repairUserItem(itemId);
  await Promise.all([userStore.fetchUser(), userStore.fetchUserItems()]);
  notify(t('character.inventory.item.repair.notify.success'));
};

const onUpgradeUserItem = async (itemId: number) => {
  await upgradeUserItem(itemId);
  await Promise.all([
    userStore.fetchUser(),
    userStore.fetchUserItems(),
    loadCharacterItems(0, { id: character.value.id }),
  ]);
  notify(t('character.inventory.item.upgrade.notify.success'));
};

const flatItems = computed(() => createItemIndex(extractItemFromUserItem(userStore.userItems)));

const sortingConfig: SortingConfig = {
  rank_desc: {
    field: 'rank',
    order: 'desc',
  },
  type_asc: {
    field: 'type',
    order: 'asc',
  },
  price_asc: {
    field: 'price',
    order: 'asc',
  },
  price_desc: {
    field: 'price',
    order: 'desc',
  },
};

const filterByTypeModel = ref<ItemType[]>([]);
const filterByNameModel = ref<string>('');
const sortingModel = ref<string>('rank_desc');

const aggregationConfig = {
  type: {
    title: 'type',
    description: '',
    sort: 'term',
    size: 1000,
    conjunction: false,
    view: AggregationView.Radio,
    chosen_filters_on_top: false,
  },
} as AggregationConfig;

const searchResult = computed(() =>
  getSearchResult({
    items: flatItems.value,
    userItemsIds: [],
    aggregationConfig: aggregationConfig,
    sortingConfig: sortingConfig,
    sort: sortingModel.value,
    page: 1,
    perPage: 1000,
    query: filterByNameModel.value,
    filter: {
      type: filterByTypeModel.value,
    },
  })
);

const filteredUserItems = computed(() => {
  const foundedItemIds = searchResult.value.data.items.map(item => item.id);
  return userStore.userItems
    .filter(item => foundedItemIds.includes(item.item.id))
    .sort((a, b) => foundedItemIds.indexOf(a.item.id) - foundedItemIds.indexOf(b.item.id));
});

const totalItemsCost = computed(() =>
  filteredUserItems.value.reduce((out, item) => out + item.item.price, 0)
);

const equippedItemsBySlot = computed(() =>
  characterItems.value.reduce((out, ei) => {
    out[ei.slot] = ei.userItem; // TODO: fix ts err
    return out;
  }, {} as UserItemsBySlot)
);

provide(equippedItemsBySlotKey, equippedItemsBySlot);

interface CompareGroup {
  type: ItemType;
  weaponClass: WeaponClass | null;
  items: ItemFlat[];
}

const { openedItems, closeItemDetail, closeAll } = useItemDetail();

// // TODO: FIXME: to service
const compareItemsResult = computed(() => {
  const groupedItems = flatItems.value
    .filter(item => openedItems.value.some(oi => oi.id === item.id))
    .reduce((out, item) => {
      const currentEl = out.find(
        el => el.type === item.type && el.weaponClass === item.weaponClass
      );

      if (currentEl) {
        currentEl.items.push(item);
      } else {
        out.push({
          type: item.type,
          weaponClass: item.weaponClass,
          items: [item],
        });
      }

      return out;
    }, [] as CompareGroup[]);

  return groupedItems
    .filter(group => group.items.length >= 2) // there is no point in comparing 1 item
    .map(group => {
      return {
        type: group.type,
        weaponClass: group.weaponClass,
        compareResult: getCompareItemsResult(
          group.items,
          getAggregationsConfig(group.type, group.weaponClass)
        ),
      };
    });
});

const aside = ref<HTMLDivElement | null>(null);
const { top: stickySidebarTop } = useStickySidebar(aside, mainHeaderHeight.value + 16, 16);

const computeDetailCardYPosition = (y: number) => {
  // we cannot automatically determine the height of the card, so we take the maximum possible value
  // think about it, but it's fine as it is
  const cardHeight = 700;

  const yDiff = window.innerHeight - y;
  const needOffset = yDiff < cardHeight;

  if (!needOffset) {
    return y;
  }

  return y + yDiff - cardHeight;
};

onBeforeRouteLeave(() => {
  closeAll();
  return true;
});

await userStore.fetchUserItems();
</script>

<template>
  <div class="relative grid grid-cols-12 gap-5">
    <!-- <div class="fixed top-4 right-10 z-20 rounded-lg bg-white p-4 shadow-lg"> -->
    <!-- {{ JSON.stringify({ focusedItemId, availableSlots, toSlot, fromSlot }) }} -->
    <!-- {{ JSON.stringify(openedItems) }} -->
    <!-- </div> -->

    <div class="col-span-5">
      <template v-if="userStore.userItems.length !== 0">
        <div class="inventoryGrid relative grid h-full gap-x-3 gap-y-4">
          <div style="grid-area: filter">
            <div ref="aside" class="sticky" :style="{ top: `${stickySidebarTop}px` }">
              <CharacterInventoryFilter
                v-model="filterByTypeModel"
                :buckets="searchResult.data.aggregations.type.buckets"
                @click="scrollToTop"
              />
            </div>
          </div>

          <div class="grid grid-cols-3 gap-4 2xl:grid-cols-4" style="grid-area: sort">
            <div class="col-span-2 2xl:col-span-3">
              <OInput
                v-model="filterByNameModel"
                type="text"
                expanded
                clearable
                :placeholder="$t('action.search')"
                icon="search"
                rounded
                size="sm"
              />
            </div>

            <VDropdown class="col-span-1" :triggers="['click']" placement="bottom-end">
              <OButton
                variant="secondary"
                size="sm"
                type="button"
                expanded
                icon-right="chevron-down"
                :label="$t(`character.inventory.sort.${sortingModel}`)"
                v-tooltip="$t('action.sort')"
              />

              <template #popper="{ hide }">
                <DropdownItem
                  v-for="sort in Object.keys(sortingConfig)"
                  :checked="sort === sortingModel"
                  @click="
                    () => {
                      sortingModel = sort;
                      hide();
                    }
                  "
                >
                  {{ $t(`character.inventory.sort.${sort}`) }}
                </DropdownItem>
              </template>
            </VDropdown>
          </div>

          <div class="relative h-full" style="grid-area: items">
            <CharacterInventoryItemList
              :items="filteredUserItems"
              :equippedItemsIds="equippedItemsIds"
            />
          </div>

          <div
            class="sticky bottom-4 left-0 z-10 flex w-full justify-center rounded-lg bg-base-200 bg-opacity-60 p-4 backdrop-blur-lg"
            style="grid-area: footer"
          >
            <i18n-t
              scope="global"
              keypath="character.inventory.total.tpl"
              tag="div"
              :plural="filteredUserItems.length"
            >
              <template #count>
                <i18n-t
                  scope="global"
                  keypath="character.inventory.total.count"
                  tag="span"
                  :plural="filteredUserItems.length"
                >
                  <template #count>
                    <span class="font-bold text-content-100">
                      {{ filteredUserItems.length }}
                    </span>
                  </template>
                </i18n-t>
              </template>

              <template #sum>
                <i18n-t scope="global" tag="span" keypath="character.inventory.total.sum">
                  <template #sum>
                    <Coin :value="totalItemsCost" />
                  </template>
                </i18n-t>
              </template>
            </i18n-t>
          </div>
        </div>
      </template>

      <ResultNotFound
        v-else
        class="rounded-xl border border-dashed border-border-300"
        :message="$t('character.inventory.empty')"
      />
    </div>

    <div class="sticky left-0 col-span-5 self-start" :style="{ top: `${mainHeaderHeight + 16}px` }">
      <CharacterInventoryDoll @change="changeEquippedItems" />
    </div>

    <div
      class="sticky col-span-2 grid grid-cols-1 items-start gap-2 self-start rounded-lg border border-border-200 py-2 text-2xs"
      :style="{ top: `${mainHeaderHeight + 16}px` }"
    >
      <SimpleTableRow :label="$t('character.stats.price.title')">
        <div class="inline-flex gap-1.5 align-middle">
          <SvgSpriteImg name="coin" viewBox="0 0 18 18" class="w-4" />
          <span class="text-xs font-bold text-primary">{{ $n(itemsStats.price) }}</span>
        </div>
      </SimpleTableRow>

      <SimpleTableRow :label="$t('character.stats.avgRepairCost.title')">
        <div class="inline-flex gap-1.5 align-middle">
          <SvgSpriteImg name="coin" viewBox="0 0 18 18" class="w-4" />
          <ClosableTooltip :disabled="!upkeepIsHigh" shown placement="top">
            <span
              class="text-xs font-bold"
              :class="[upkeepIsHigh ? 'text-status-danger' : 'text-primaryr']"
            >
              {{ $n(itemsStats.averageRepairCostByHour) }} / {{ $t('dateTime.hours.short') }}
            </span>
            <template #popper>
              <div class="prose prose-invert">
                <h4 class="text-content-100">
                  {{ $t('character.highUpkeepWarning.title') }}
                </h4>
                <div v-html="$t('character.highUpkeepWarning.desc')"></div>
              </div>
            </template>
          </ClosableTooltip>
        </div>
      </SimpleTableRow>

      <CharacterStats
        :characteristics="characterCharacteristics"
        :weight="itemsStats.weight"
        :longestWeaponLength="itemsStats.longestWeaponLength"
        :healthPoints="healthPoints"
      />
    </div>

    <Teleport to="body">
      <Draggable
        v-for="oi in openedItems"
        :key="oi.id"
        :initial-value="{
          x: oi.bound.x + oi.bound.width + 8,
          y: computeDetailCardYPosition(oi.bound.y),
        }"
        class="fixed z-50 cursor-move select-none rounded-lg bg-base-300 p-4 shadow-lg"
      >
        <OButton
          class="!absolute right-2 top-2 z-10 cursor-pointer"
          iconRight="close"
          rounded
          size="2xs"
          variant="secondary"
          @click="closeItemDetail(oi.id)"
        />

        <CharacterInventoryItemDetail
          class="w-72"
          :compareResult="compareItemsResult.find(cr => cr.type === flatItems.find(fi => fi.id === oi.id)!.type)?.compareResult"
          :item="flatItems.find(fi => fi.id === oi.id)!"
          :userItem="userStore.userItems.find(ui => ui.id === oi.userId)!"
          :equipped="equippedItemsIds.includes(oi.userId)"
          @sell="
            () => {
              closeItemDetail(oi.id);
              onSellUserItem(oi.userId);
            }
          "
          @repair="
            () => {
              closeItemDetail(oi.id);
              onRepairUserItem(oi.userId);
            }
          "
          @upgrade="
            () => {
              closeItemDetail(oi.id);
              onUpgradeUserItem(oi.userId);
            }
          "
        />
      </Draggable>
    </Teleport>
  </div>
</template>

<style lang="css">
.inventoryGrid {
  grid-template-areas:
    '...... sort'
    'filter items'
    'filter footer';
  grid-template-columns: auto 1fr;
  grid-template-rows: auto 1fr auto;
}
</style>

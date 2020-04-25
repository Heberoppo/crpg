import {
  Action, getModule, Module, Mutation, VuexModule,
} from 'vuex-module-decorators';
import store from '@/store';
import * as userService from '@/services/users-service';
import User from '@/models/user';
import Character from '@/models/character';
import Item from '@/models/item';
import ItemSlot from '@/models/item-slot';
import { setCharacterItem } from '@/services/characters-service';
import CharacterItems from '@/models/character-items';

@Module({ store, dynamic: true, name: 'user' })
class UserModule extends VuexModule {
  isSignedIn: boolean = false;
  user: User | null = null;
  ownedItems: Item[] = [];
  characters: Character[] = [];

  @Mutation
  signIn() {
    this.isSignedIn = true;
  }

  @Mutation
  signOut() {
    this.isSignedIn = false;
    this.user = null;
    this.ownedItems = [];
    this.characters = [];
  }

  @Mutation
  setUser(user: User) {
    this.user = user;
  }

  @Mutation
  substractGold(loss: number) {
    this.user!.gold -= loss;
  }

  @Mutation
  setOwnedItems(ownedItems: Item[]) {
    this.ownedItems = ownedItems;
  }

  @Mutation
  addOwnedItem(item: Item) {
    this.ownedItems.push(item);
  }

  @Mutation
  setCharacters(characters: Character[]) {
    this.characters = characters;
  }

  @Mutation
  setCharacterItem({ characterItems, slot, item } : { characterItems: CharacterItems, slot: ItemSlot, item: Item }) {
    setCharacterItem(characterItems, slot, item);
  }

  @Mutation
  replaceCharacter(character: Character) {
    const idx = this.characters.findIndex(c => c.id === character.id);
    this.characters.splice(idx, 1, character);
  }

  @Mutation
  removeCharacter(character: Character) {
    const idx = this.characters.findIndex(c => c.id === character.id);
    this.characters.splice(idx, 1);
  }

  @Action({ commit: 'setUser' })
  getUser() {
    return userService.getUser();
  }

  @Action({ commit: 'setOwnedItems' })
  getOwnedItems() {
    return userService.getOwnedItems();
  }

  @Action
  renameCharacter({ character, newName } : { character: Character, newName: string }) {
    this.replaceCharacter({
      ...character,
      name: newName,
    });
    return userService.updateCharacter(character.id, { name: newName });
  }

  @Action
  replaceItem({ character, slot, item } : { character: Character, slot: ItemSlot, item: Item }) {
    const { items } = character;
    this.setCharacterItem({ characterItems: items, slot, item });
    return userService.updateItems(character.id, {
      headItemId: items.headItem !== null ? items.headItem!.id : null,
      capeItemId: items.capeItem !== null ? items.capeItem!.id : null,
      bodyItemId: items.bodyItem !== null ? items.bodyItem!.id : null,
      handItemId: items.handItem !== null ? items.handItem!.id : null,
      legItemId: items.legItem !== null ? items.legItem!.id : null,
      horseHarnessItemId: items.horseHarnessItem !== null ? items.horseHarnessItem!.id : null,
      horseItemId: items.horseItem !== null ? items.horseItem!.id : null,
      weapon1ItemId: items.weapon1Item !== null ? items.weapon1Item!.id : null,
      weapon2ItemId: items.weapon2Item !== null ? items.weapon2Item!.id : null,
      weapon3ItemId: items.weapon3Item !== null ? items.weapon3Item!.id : null,
      weapon4ItemId: items.weapon4Item !== null ? items.weapon4Item!.id : null,
    });
  }

  @Action
  async buyItem(item: Item) {
    await userService.buyItem(item.id);
    this.addOwnedItem(item);
    this.substractGold(item.value);
  }

  @Action({ commit: 'setCharacters' })
  getCharacters() : Promise<Character[]> {
    return userService.getCharacters();
  }

  @Action({ commit: 'replaceCharacter' })
  retireCharacter(character: Character): Promise<void> {
    return userService.retireCharacter(character.id);
  }

  @Action
  deleteCharacter(character: Character): Promise<void> {
    this.removeCharacter(character);
    return userService.deleteCharacter(character.id);
  }
}

export default getModule(UserModule);
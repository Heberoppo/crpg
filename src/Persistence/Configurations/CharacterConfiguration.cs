using Crpg.Domain.Entities;
using Crpg.Domain.Entities.Characters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Crpg.Persistence.Configurations
{
    public class CharacterConfiguration : IEntityTypeConfiguration<Character>
    {
        public void Configure(EntityTypeBuilder<Character> builder)
        {
            builder.HasIndex(c => new { c.UserId, c.Name }).IsUnique();
            builder.OwnsOne(c => c.Statistics, ConfigureCharacterStatistics);
        }

        private static void ConfigureCharacterStatistics(OwnedNavigationBuilder<Character, CharacterStatistics> builder)
        {
            builder.OwnsOne(cs => cs.Attributes, ConfigureCharacterAttributes);
            builder.OwnsOne(cs => cs.Skills, ConfigureCharacterSkills);
            builder.OwnsOne(cs => cs.WeaponProficiencies, ConfigureCharacterWeaponProficiencies);
        }

        private static void ConfigureCharacterAttributes(OwnedNavigationBuilder<CharacterStatistics, CharacterAttributes> builder)
        {
            // Default names are prefixed with character_attributes_.
            builder.Property(a => a.Points).HasColumnName("attribute_points");
            builder.Property(a => a.Strength).HasColumnName("strength");
            builder.Property(a => a.Agility).HasColumnName("agility");
        }

        private static void ConfigureCharacterSkills(OwnedNavigationBuilder<CharacterStatistics, CharacterSkills> builder)
        {
            // Default names are prefixed with character_skills_.
            builder.Property(s => s.Points).HasColumnName("skill_points");
            builder.Property(s => s.IronFlesh).HasColumnName("iron_flesh");
            builder.Property(s => s.PowerStrike).HasColumnName("power_strike");
            builder.Property(s => s.PowerDraw).HasColumnName("power_draw");
            builder.Property(s => s.PowerThrow).HasColumnName("power_throw");
            builder.Property(s => s.Athletics).HasColumnName("athletics");
            builder.Property(s => s.Riding).HasColumnName("riding");
            builder.Property(s => s.WeaponMaster).HasColumnName("weapon_master");
            builder.Property(s => s.MountedArchery).HasColumnName("mounted_archery");
            builder.Property(s => s.Shield).HasColumnName("shield");
        }

        private static void ConfigureCharacterWeaponProficiencies(OwnedNavigationBuilder<CharacterStatistics, CharacterWeaponProficiencies> builder)
        {
            // Default names are prefixed with character_weapon_proficiencies.
            builder.Property(wp => wp.Points).HasColumnName("weapon_proficiency_points");
            builder.Property(wp => wp.OneHanded).HasColumnName("one_handed");
            builder.Property(wp => wp.TwoHanded).HasColumnName("two_handed");
            builder.Property(wp => wp.Polearm).HasColumnName("polearm");
            builder.Property(wp => wp.Bow).HasColumnName("bow");
            builder.Property(wp => wp.Throwing).HasColumnName("throwing");
            builder.Property(wp => wp.Crossbow).HasColumnName("crossbow");
        }
    }
}

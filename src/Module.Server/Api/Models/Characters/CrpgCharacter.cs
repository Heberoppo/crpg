﻿using Crpg.Module.Api.Models.Items;

namespace Crpg.Module.Api.Models.Characters;

// Copy of Crpg.Application.Characters.Models.CharacterViewModel
internal class CrpgCharacter
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Generation { get; set; }
    public int Level { get; set; }
    public int Experience { get; set; }
    public CrpgCharacterStatistics Statistics { get; set; } = new();
    public IList<CrpgEquippedItem> EquippedItems { get; set; } = Array.Empty<CrpgEquippedItem>();
}
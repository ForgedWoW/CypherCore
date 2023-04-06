// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage.Structs.A;

namespace Forged.MapServer.Entities.Items;

public class AzeriteData
{
    public List<uint> AzeriteItemMilestonePowers { get; set; } = new();
    public uint KnowledgeLevel { get; set; }
    public uint Level { get; set; }
    public AzeriteItemSelectedEssencesData[] SelectedAzeriteEssences { get; set; } = new AzeriteItemSelectedEssencesData[4];
    public List<AzeriteEssencePowerRecord> UnlockedAzeriteEssences { get; set; } = new();
    public ulong Xp { get; set; }
}
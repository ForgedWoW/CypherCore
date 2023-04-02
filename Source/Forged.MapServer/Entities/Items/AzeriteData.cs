// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage.Structs.A;

namespace Forged.MapServer.Entities.Items;

public class AzeriteData
{
    public List<uint> AzeriteItemMilestonePowers = new();
    public uint KnowledgeLevel;
    public uint Level;
    public AzeriteItemSelectedEssencesData[] SelectedAzeriteEssences = new AzeriteItemSelectedEssencesData[4];
    public List<AzeriteEssencePowerRecord> UnlockedAzeriteEssences = new();
    public ulong Xp;
}
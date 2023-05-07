// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellXSpellVisualRecord
{
    public int ActiveIconFileID;
    public uint CasterPlayerConditionID;
    public ushort CasterUnitConditionID;
    public byte DifficultyID;
    public int Flags;
    public uint Id;
    public int Priority;
    public float Probability;
    public int SpellIconFileID;
    public uint SpellID;
    public uint SpellVisualID;
    public uint ViewerPlayerConditionID;
    public ushort ViewerUnitConditionID;
}
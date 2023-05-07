// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.D;

public sealed record DungeonEncounterRecord
{
    public sbyte Bit;
    public int CompleteWorldStateID;
    public int DifficultyID;
    public int Faction;
    public int Flags;
    public uint Id;
    public short MapID;
    public LocalizedString Name;
    public int OrderIndex;
    public int SpellIconFileID;
}
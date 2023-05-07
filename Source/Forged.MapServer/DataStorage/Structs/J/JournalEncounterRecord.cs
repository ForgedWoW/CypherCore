// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.J;

public sealed record JournalEncounterRecord
{
    public LocalizedString Description;
    public sbyte DifficultyMask;
    public ushort DungeonEncounterID;
    public ushort FirstSectionID;
    public int Flags;
    public uint Id;
    public ushort JournalInstanceID;
    public Vector2 Map;
    public uint MapDisplayConditionID;
    public LocalizedString Name;
    public uint OrderIndex;
    public ushort UiMapID;
}
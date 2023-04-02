// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellTargetRestrictionsRecord
{
    public float ConeDegrees;
    public byte DifficultyID;
    public uint Id;
    public uint MaxTargetLevel;
    public byte MaxTargets;
    public uint SpellID;
    public ushort TargetCreatureType;
    public uint Targets;
    public float Width;
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellVisualRecord
{
    public uint AnimEventSoundID;
    public uint CasterSpellVisualID;
    public ushort DamageNumberDelay;
    public int Flags;
    public uint HostileSpellVisualID;
    public uint Id;
    public uint LowViolenceSpellVisualID;
    public sbyte MissileAttachment;
    public float[] MissileCastOffset = new float[3];
    public uint MissileCastPositionerID;
    public sbyte MissileDestinationAttachment;
    public float[] MissileImpactOffset = new float[3];
    public uint MissileImpactPositionerID;
    public int MissileTargetingKit;
    public uint RaidSpellVisualMissileSetID;
    public int ReducedUnexpectedCameraMovementSpellVisualID;
    public ushort SpellVisualMissileSetID;
}
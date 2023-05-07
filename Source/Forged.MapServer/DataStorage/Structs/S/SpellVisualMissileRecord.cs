// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellVisualMissileRecord
{
    public uint AnimKitID;
    public sbyte Attachment;
    public float[] CastOffset = new float[3];
    public ushort CastPositionerID;
    public sbyte ClutterLevel;
    public int DecayTimeAfterImpact;
    public sbyte DestinationAttachment;
    public uint Flags;
    public ushort FollowGroundApproach;
    public uint FollowGroundDropSpeed;
    public int FollowGroundHeight;
    public uint Id;
    public float[] ImpactOffset = new float[3];
    public ushort ImpactPositionerID;
    public uint SoundEntriesID;
    public ushort SpellMissileMotionID;
    public ushort SpellVisualEffectNameID;
    public uint SpellVisualMissileSetID;
}
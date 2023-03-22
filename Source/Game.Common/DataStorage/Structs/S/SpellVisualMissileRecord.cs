﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.DataStorage;

public sealed class SpellVisualMissileRecord
{
	public float[] CastOffset = new float[3];
	public float[] ImpactOffset = new float[3];
	public uint Id;
	public ushort SpellVisualEffectNameID;
	public uint SoundEntriesID;
	public sbyte Attachment;
	public sbyte DestinationAttachment;
	public ushort CastPositionerID;
	public ushort ImpactPositionerID;
	public int FollowGroundHeight;
	public uint FollowGroundDropSpeed;
	public ushort FollowGroundApproach;
	public uint Flags;
	public ushort SpellMissileMotionID;
	public uint AnimKitID;
	public sbyte ClutterLevel;
	public int DecayTimeAfterImpact;
	public uint SpellVisualMissileSetID;
}
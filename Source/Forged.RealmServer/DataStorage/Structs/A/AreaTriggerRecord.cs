// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;

namespace Forged.RealmServer.DataStorage;

public sealed class AreaTriggerRecord
{
	public Vector3 Pos;
	public uint Id;
	public ushort ContinentID;
	public sbyte PhaseUseFlags;
	public ushort PhaseID;
	public ushort PhaseGroupID;
	public float Radius;
	public float BoxLength;
	public float BoxWidth;
	public float BoxHeight;
	public float BoxYaw;
	public sbyte ShapeType;
	public short ShapeID;
	public short AreaTriggerActionSetID;
	public sbyte Flags;
}
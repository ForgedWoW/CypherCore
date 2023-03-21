// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Framework.GameMath;

namespace Forged.RealmServer.Collision;

public class ModelMinimalData
{
	public byte Flags;
	public byte AdtId;
	public uint Id;
	public Vector3 IPos;
	public float IScale;
	public AxisAlignedBox IBound;
	public string Name;
}
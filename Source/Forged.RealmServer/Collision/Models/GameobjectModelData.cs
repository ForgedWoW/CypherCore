// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Framework.GameMath;

namespace Forged.RealmServer.Collision;

public class GameobjectModelData
{
	public readonly string Name;
	public readonly bool IsWmo;

	public AxisAlignedBox Bound;

	public GameobjectModelData(string name, Vector3 lowBound, Vector3 highBound, bool isWmo)
	{
		Bound = new AxisAlignedBox(lowBound, highBound);
		Name = name;
		IsWmo = isWmo;
	}
}
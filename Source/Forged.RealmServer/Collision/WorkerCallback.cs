// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Framework.GameMath;

namespace Forged.RealmServer.Collision;

public class WorkerCallback
{
	public virtual void Invoke(Vector3 point, int entry) { }
	public virtual void Invoke(Vector3 point, GameObjectModel obj) { }

	public virtual bool Invoke(Ray ray, int entry, ref float distance, bool pStopAtFirstHit)
	{
		return false;
	}

	public virtual bool Invoke(Ray r, IModel obj, ref float distance)
	{
		return false;
	}
}
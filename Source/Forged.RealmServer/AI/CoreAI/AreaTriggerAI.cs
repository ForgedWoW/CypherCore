// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Forged.RealmServer.AI;

public class AreaTriggerAI
{
	protected AreaTrigger At;

	public AreaTriggerAI(AreaTrigger a)
	{
		At = a;
	}

	// Called when the AreaTrigger has just been initialized, just before added to map
	public virtual void OnInitialize() { }

	// Called when the AreaTrigger has just been created
	public virtual void OnCreate() { }

	// Called on each AreaTrigger update
	public virtual void OnUpdate(uint diff) { }

	// Called when the AreaTrigger reach splineIndex
	public virtual void OnSplineIndexReached(int splineIndex) { }

	// Called when the AreaTrigger reach its destination
	public virtual void OnDestinationReached() { }

	// Called when an unit enter the AreaTrigger
	public virtual void OnUnitEnter(Unit unit) { }

	// Called when an unit exit the AreaTrigger, or when the AreaTrigger is removed
	public virtual void OnUnitExit(Unit unit) { }

	// Called when the AreaTrigger is removed
	public virtual void OnRemove() { }

	public virtual void OnPeriodicProc() { }
}
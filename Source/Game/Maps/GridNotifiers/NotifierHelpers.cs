// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Maps;

public static class NotifierHelpers
{
	public static void CreatureUnitRelocationWorker(Creature c, Unit u)
	{
		if (!u.IsAlive() || !c.IsAlive() || c == u || u.IsInFlight())
			return;

		if (!c.HasUnitState(UnitState.Sightless))
		{
			if (c.IsAIEnabled() && c.CanSeeOrDetect(u, false, true))
			{
				c.GetAI().MoveInLineOfSight_Safe(u);
			}
			else
			{
				if (u.IsTypeId(TypeId.Player) && u.HasStealthAura() && c.IsAIEnabled() && c.CanSeeOrDetect(u, false, true, true))
					c.GetAI().TriggerAlert(u);
			}
		}
	}
}
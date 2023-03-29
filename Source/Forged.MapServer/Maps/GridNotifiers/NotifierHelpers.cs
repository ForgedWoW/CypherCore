// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public static class NotifierHelpers
{
    public static void CreatureUnitRelocationWorker(Creature c, Unit u)
    {
        if (!u.IsAlive || !c.IsAlive || c == u || u.IsInFlight)
            return;

        if (!c.HasUnitState(UnitState.Sightless))
        {
            if (c.IsAIEnabled && c.Visibility.CanSeeOrDetect(u, false, true))
            {
                c.AI.MoveInLineOfSight_Safe(u);
            }
            else
            {
                if (u.IsTypeId(TypeId.Player) && u.HasStealthAura && c.IsAIEnabled && c.Visibility.CanSeeOrDetect(u, false, true, true))
                    c.AI.TriggerAlert(u);
            }
        }
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Creatures;

namespace Forged.MapServer.Globals;

public class DefaultCreatureBaseStats : CreatureBaseStats
{
    public DefaultCreatureBaseStats()
    {
        BaseMana = 0;
        AttackPower = 0;
        RangedAttackPower = 0;
    }
}
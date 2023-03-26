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
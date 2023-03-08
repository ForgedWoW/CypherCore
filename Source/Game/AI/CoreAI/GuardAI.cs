// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.AI
{
    public class GuardAI : ScriptedAI
    {
        public GuardAI(Creature creature) : base(creature) { }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            DoMeleeAttackIfReady();
        }

        public override bool CanSeeAlways(WorldObject obj)
        {
            Unit unit = obj.AsUnit;
            if (unit != null)
                if (unit.IsControlledByPlayer && me.IsEngagedBy(unit))
                    return true;

            return false;
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            if (!me.IsAlive)
            {
                me.                MotionMaster.MoveIdle();
                me.CombatStop(true);
                EngagementOver();
                return;
            }

            Log.outTrace(LogFilter.ScriptsAi, $"GuardAI::EnterEvadeMode: {me.GUID} enters evade mode.");

            me.RemoveAllAuras();
            me.CombatStop(true);
            EngagementOver();

            me.
            MotionMaster.MoveTargetedHome();
        }

        public override void JustDied(Unit killer)
        {
            if (killer != null)
            {
                Player player = killer.GetCharmerOrOwnerPlayerOrPlayerItself();
                if (player != null)
                    me.SendZoneUnderAttackMessage(player);
            }
        }
    }
}

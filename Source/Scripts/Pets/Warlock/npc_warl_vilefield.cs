// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ICreature;
using Scripts.Spells.Warlock;

namespace Scripts.Pets
{
    namespace Warlock
    {
        [CreatureScript(135816)]
        public class npc_warl_vilefield : SmartAI
        {
            public npc_warl_vilefield(Creature creature) : base(creature)
            {
                if (!me.TryGetOwner(out Player owner))
                    return;

                creature.SetLevel(owner.GetLevel());
                creature.UpdateLevelDependantStats();
                creature.SetReactState(ReactStates.Aggressive);
                creature.SetCreatorGUID(owner.GetGUID());

                var summon = creature.ToTempSummon();

                if (summon != null)
                {
                    summon.SetCanFollowOwner(true);
                    summon.GetMotionMaster().Clear();
                    summon.GetMotionMaster().MoveFollow(owner, SharedConst.PetFollowDist, summon.GetFollowAngle());
                    StartAttackOnOwnersInCombatWith();
                }
            }

            public override void Reset()
            {
                Unit owner = me.GetOwner();

                if (owner == null)
                    return;

                me.SetMaxHealth(owner.GetMaxHealth());
                me.SetHealth(me.GetMaxHealth());
                me.SetControlled(true, UnitState.Root);
            }
        }
    }
}
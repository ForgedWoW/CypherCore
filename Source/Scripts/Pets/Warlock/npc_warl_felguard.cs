// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Pets
{
    namespace Warlock
    {
        [CreatureScript(17252)]
        public class npc_warl_felguard : SmartAI
        {
            public npc_warl_felguard(Creature creature) : base(creature)
            {
                if (!Me.TryGetOwner(out Player owner))
                    return;

                creature.SetLevel(owner.Level);
                creature.UpdateLevelDependantStats();
                creature.ReactState = ReactStates.Assist;
                creature.SetCreatorGUID(owner.GUID);

                var summon = creature.ToTempSummon();

                if (summon != null)
                {
                    summon.SetCanFollowOwner(true);
                    summon.MotionMaster.Clear();
                    summon.MotionMaster.MoveFollow(owner, SharedConst.PetFollowDist, summon.FollowAngle);
                    StartAttackOnOwnersInCombatWith();
                }
            }

            public override void Reset()
            {
                var owner = Me.OwnerUnit;

                if (owner == null)
                    return;

                Me.SetMaxHealth(owner.MaxHealth);
                Me.SetHealth(Me.MaxHealth);
                Me.SetControlled(true, UnitState.Root);
            }
        }
    }
}
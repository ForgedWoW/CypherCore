// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using Scripts.Spells.Warlock;

namespace Scripts.Pets
{
    namespace Warlock
    {
        // Darkglare - 103673
        [CreatureScript(103673)]
        public class npc_pet_warlock_darkglare : SmartAI
        {
            public npc_pet_warlock_darkglare(Creature creature) : base(creature)
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

            public override void UpdateAI(uint UnnamedParameter)
            {
                var owner = Me.OwnerUnit;

                if (owner == null)
                    return;

                var target = Me.GetAttackerForHelper();

                if (target != null)
                {
                    target.RemoveAura(WarlockSpells.DOOM, owner.GUID);
                    Me.CastSpell(target, WarlockSpells.EYE_LASER, new CastSpellExtraArgs(TriggerCastFlags.None).SetOriginalCaster(owner.GUID));
                }
            }
        }
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.SmartScripts;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.Pets
{
    namespace Warlock
    {
        // 107024 - Fel Lord
        [CreatureScript(107024)]
        public class NPCWarlFelLord : SmartAI
        {
            public NPCWarlFelLord(Creature creature) : base(creature)
            {
                if (!Me.TryGetOwner(out Player owner))
                    return;

                creature.SetLevel(owner.Level);
                creature.UpdateLevelDependantStats();
                creature.ReactState = ReactStates.Aggressive;
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

            //public override void UpdateAI(uint UnnamedParameter)
            //{
            //    if (me.HasUnitState(UnitState.Casting))
            //        return;

            //    me.SpellFactory.CastSpell(me, WarlockSpells.FEL_LORD_CLEAVE, false);
            //}
        }
    }
}
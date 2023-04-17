// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.SmartScripts;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;
using Scripts.Spells.Warlock;

namespace Scripts.Pets
{
    namespace Warlock
    {
        // Wild Imp - 99739
        [CreatureScript(55659)]
        public class NPCPetWarlockWildImp : SmartAI
        {
            private ObjectGuid _targetGUID = new();

            public NPCPetWarlockWildImp(Creature creature) : base(creature)
            {
                var owner = Me.OwnerUnit;

                if (Me.OwnerUnit)
                {
                    Me.SetLevel(owner.Level);
                    Me.SetMaxHealth(owner.MaxHealth / 3);
                    Me.SetHealth(owner.Health / 3);

                    if (owner.IsPlayer)
                    {
                        var p = owner.AsPlayer;
                        p.AddAura(296553, p);
                    }
                }


                creature.UpdateLevelDependantStats();
                creature.ReactState = ReactStates.Aggressive;
                creature.SetCreatorGUID(owner.GUID);

                var summon = creature.ToTempSummon();

                if (summon != null)
                {
                    summon.SetCanFollowOwner(true);
                    summon.MotionMaster.Clear();
                    summon.MotionMaster.MoveFollow(owner, SharedConst.PetFollowDist, summon.FollowAngle);
                }
            }

            public override void UpdateAI(uint unnamedParameter)
            {
                var owner = Me.OwnerUnit;

                if (owner == null)
                    return;

                var target = GetTarget();
                var newtargetGUID = owner.Target;

                if (newtargetGUID.IsEmpty ||
                    newtargetGUID == _targetGUID)
                {
                    CastSpellOnTarget(owner, target);

                    return;
                }

                var newTarget = ObjectAccessor.Instance.GetUnit(Me, newtargetGUID);

                if (ObjectAccessor.Instance.GetUnit(Me, newtargetGUID))
                    if (target != newTarget &&
                        Me.IsValidAttackTarget(newTarget))
                        target = newTarget;

                CastSpellOnTarget(owner, target);
            }

            public override void OnDespawn()
            {
                var caster = Me.OwnerUnit;

                if (caster == null) return;

                if (caster.GetCreatureListWithEntryInGrid(55659).Count == 0)
                    caster.RemoveAura(296553);
            }

            private Unit GetTarget()
            {
                return ObjectAccessor.Instance.GetUnit(Me, _targetGUID);
            }

            private void CastSpellOnTarget(Unit owner, Unit target)
            {
                if (target != null &&
                    Me.IsValidAttackTarget(target) &&
                    !Me.HasUnitState(UnitState.Casting) &&
                    !Me.VariableStorage.GetValue("controlled", false))
                {
                    _targetGUID = target.GUID;
                    Me.SpellFactory.CastSpell(target, WarlockSpells.FEL_FIREBOLT, new CastSpellExtraArgs(TriggerCastFlags.IgnorePowerAndReagentCost).SetOriginalCaster(owner.GUID));
                }
            }
        }
    }
}
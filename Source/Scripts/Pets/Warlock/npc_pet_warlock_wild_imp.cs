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
		// Wild Imp - 99739
		[CreatureScript(55659)]
		public class npc_pet_warlock_wild_imp : SmartAI
		{
			private ObjectGuid _targetGUID = new();

			public npc_pet_warlock_wild_imp(Creature creature) : base(creature)
			{
				var owner = me.OwnerUnit;

				if (me.OwnerUnit)
				{
					me.SetLevel(owner.Level);
					me.SetMaxHealth(owner.MaxHealth / 3);
					me.SetHealth(owner.Health / 3);

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

			public override void UpdateAI(uint UnnamedParameter)
			{
				var owner = me.OwnerUnit;

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

				var newTarget = ObjectAccessor.Instance.GetUnit(me, newtargetGUID);

				if (ObjectAccessor.Instance.GetUnit(me, newtargetGUID))
					if (target != newTarget &&
						me.IsValidAttackTarget(newTarget))
						target = newTarget;

				CastSpellOnTarget(owner, target);
			}

			public override void OnDespawn()
			{
				var caster = me.OwnerUnit;

				if (caster == null) return;

				if (caster.GetCreatureListWithEntryInGrid(55659).Count == 0)
					caster.RemoveAura(296553);
			}

			private Unit GetTarget()
			{
				return ObjectAccessor.Instance.GetUnit(me, _targetGUID);
			}

			private void CastSpellOnTarget(Unit owner, Unit target)
			{
				if (target != null &&
					me.IsValidAttackTarget(target) &&
					!me.HasUnitState(UnitState.Casting) &&
					!me.VariableStorage.GetValue("controlled", false))
				{
					_targetGUID = target.GUID;
					me.CastSpell(target, WarlockSpells.FEL_FIREBOLT, new CastSpellExtraArgs(TriggerCastFlags.IgnorePowerAndReagentCost).SetOriginalCaster(owner.GUID));
				}
			}
		}
	}
}
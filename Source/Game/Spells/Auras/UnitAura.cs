// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;

namespace Game.Spells;

public class UnitAura : Aura
{
	readonly Dictionary<ObjectGuid, uint> _staticApplications = new(); // non-area auras

	DiminishingGroup _mAuraDrGroup; // Diminishing

	public UnitAura(AuraCreateInfo createInfo) : base(createInfo)
	{
		_mAuraDrGroup = DiminishingGroup.None;
		LoadScripts();
		_InitEffects(createInfo.AuraEffectMask, createInfo.Caster, createInfo.BaseAmount);
		OwnerAsUnit._AddAura(this, createInfo.Caster);
	}

	public override void _ApplyForTarget(Unit target, Unit caster, AuraApplication aurApp)
	{
		base._ApplyForTarget(target, caster, aurApp);

		// register aura diminishing on apply
		if (_mAuraDrGroup != DiminishingGroup.None)
			target.ApplyDiminishingAura(_mAuraDrGroup, true);
	}

	public override void _UnapplyForTarget(Unit target, Unit caster, AuraApplication aurApp)
	{
		base._UnapplyForTarget(target, caster, aurApp);

		// unregister aura diminishing (and store last time)
		if (_mAuraDrGroup != DiminishingGroup.None)
			target.ApplyDiminishingAura(_mAuraDrGroup, false);
	}

	public override void Remove(AuraRemoveMode removeMode = AuraRemoveMode.Default)
	{
		if (IsRemoved)
			return;

		OwnerAsUnit.RemoveOwnedAura(this, removeMode);
		base.Remove(removeMode);
	}

	public override void FillTargetMap(ref Dictionary<Unit, uint> targets, Unit caster)
	{
		var refe = caster;

		if (refe == null)
			refe = OwnerAsUnit;

		// add non area aura targets
		// static applications go through spell system first, so we assume they meet conditions
		foreach (var targetPair in _staticApplications)
		{
			var target = Global.ObjAccessor.GetUnit(OwnerAsUnit, targetPair.Key);

			if (target == null && targetPair.Key == OwnerAsUnit.GUID)
				target = OwnerAsUnit;

			if (target)
				targets.Add(target, targetPair.Value);
		}

		foreach (var spellEffectInfo in SpellInfo.Effects)
		{
			if (!HasEffect(spellEffectInfo.EffectIndex))
				continue;

			// area auras only
			if (spellEffectInfo.Effect == SpellEffectName.ApplyAura)
				continue;

			// skip area update if owner is not in world!
			if (!OwnerAsUnit.IsInWorld)
				continue;

			if (OwnerAsUnit.HasUnitState(UnitState.Isolated))
				continue;

			List<Unit> units = new();
			var condList = spellEffectInfo.ImplicitTargetConditions;

			var radius = spellEffectInfo.CalcRadius(refe);
			var extraSearchRadius = 0.0f;

			var selectionType = SpellTargetCheckTypes.Default;

			switch (spellEffectInfo.Effect)
			{
				case SpellEffectName.ApplyAreaAuraParty:
				case SpellEffectName.ApplyAreaAuraPartyNonrandom:
					selectionType = SpellTargetCheckTypes.Party;

					break;
				case SpellEffectName.ApplyAreaAuraRaid:
					selectionType = SpellTargetCheckTypes.Raid;

					break;
				case SpellEffectName.ApplyAreaAuraFriend:
					selectionType = SpellTargetCheckTypes.Ally;

					break;
				case SpellEffectName.ApplyAreaAuraEnemy:
					selectionType = SpellTargetCheckTypes.Enemy;
					extraSearchRadius = radius > 0.0f ? SharedConst.ExtraCellSearchRadius : 0.0f;

					break;
				case SpellEffectName.ApplyAreaAuraPet:
					if (condList == null || Global.ConditionMgr.IsObjectMeetToConditions(OwnerAsUnit, refe, condList))
						units.Add(OwnerAsUnit);

					goto case SpellEffectName.ApplyAreaAuraOwner;
				/* fallthrough */
				case SpellEffectName.ApplyAreaAuraOwner:
				{
					var owner = OwnerAsUnit.CharmerOrOwner;

					if (owner != null)
						if (OwnerAsUnit.IsWithinDistInMap(owner, radius))
							if (condList == null || Global.ConditionMgr.IsObjectMeetToConditions(owner, refe, condList))
								units.Add(owner);

					break;
				}
				case SpellEffectName.ApplyAuraOnPet:
				{
					var pet = Global.ObjAccessor.GetUnit(OwnerAsUnit, OwnerAsUnit.PetGUID);

					if (pet != null)
						if (condList == null || Global.ConditionMgr.IsObjectMeetToConditions(pet, refe, condList))
							units.Add(pet);

					break;
				}
				case SpellEffectName.ApplyAreaAuraSummons:
				{
					if (condList == null || Global.ConditionMgr.IsObjectMeetToConditions(OwnerAsUnit, refe, condList))
						units.Add(OwnerAsUnit);

					selectionType = SpellTargetCheckTypes.Summoned;

					break;
				}
			}

			if (selectionType != SpellTargetCheckTypes.Default)
			{
				WorldObjectSpellAreaTargetCheck check = new(radius, OwnerAsUnit.Location, refe, OwnerAsUnit, SpellInfo, selectionType, condList, SpellTargetObjectTypes.Unit);
				UnitListSearcher searcher = new(OwnerAsUnit, units, check, GridType.All);
				Cell.VisitGrid(OwnerAsUnit, searcher, radius + extraSearchRadius);

				// by design WorldObjectSpellAreaTargetCheck allows not-in-world units (for spells) but for auras it is not acceptable
				units.RemoveAll(unit => !unit.IsSelfOrInSameMap(OwnerAsUnit));
			}

			foreach (var unit in units)
			{
				if (!targets.ContainsKey(unit))
					targets[unit] = 0;

				targets[unit] |= 1u << spellEffectInfo.EffectIndex;
			}
		}
	}

	public void AddStaticApplication(Unit target, uint effMask)
	{
		// only valid for non-area auras
		foreach (var spellEffectInfo in SpellInfo.Effects)
			if ((effMask & (1u << spellEffectInfo.EffectIndex)) != 0 && !spellEffectInfo.IsEffect(SpellEffectName.ApplyAura))
				effMask &= ~(1u << spellEffectInfo.EffectIndex);

		if (effMask == 0)
			return;

		if (!_staticApplications.ContainsKey(target.GUID))
			_staticApplications[target.GUID] = 0;

		_staticApplications[target.GUID] |= effMask;
	}

	// Allow Apply Aura Handler to modify and access m_AuraDRGroup
	public void SetDiminishGroup(DiminishingGroup group)
	{
		_mAuraDrGroup = group;
	}

	public DiminishingGroup GetDiminishGroup()
	{
		return _mAuraDrGroup;
	}
}
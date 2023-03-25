﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.DataStorage;

namespace Game.Entities;

public partial class Player
{
	readonly float[] parry_cap =
	{
		65.631440f,  // Warrior
		65.631440f,  // Paladin
		145.560408f, // Hunter
		145.560408f, // Rogue
		0.0f,        // Priest
		65.631440f,  // DK
		145.560408f, // Shaman
		0.0f,        // Mage
		0.0f,        // Warlock
		90.6425f,    // Monk
		0.0f,        // Druid
		65.631440f,  // Demon Hunter
		0.0f,        // Evoker
		0.0f,        // Adventurer
	};

	readonly float[] dodge_cap =
	{
		65.631440f,  // Warrior            
		65.631440f,  // Paladin
		145.560408f, // Hunter
		145.560408f, // Rogue
		150.375940f, // Priest
		65.631440f,  // DK
		145.560408f, // Shaman
		150.375940f, // Mage
		150.375940f, // Warlock
		145.560408f, // Monk
		116.890707f, // Druid
		145.560408f, // Demon Hunter
		145.560408f, // Evoker
		0.0f,        // Adventurer
	};

	public override bool UpdateAllStats()
	{
		for (var i = Stats.Strength; i < Stats.Max; ++i)
		{
			var value = GetTotalStatValue(i);
			SetStat(i, (int)value);
		}

		UpdateArmor();
		// calls UpdateAttackPowerAndDamage() in UpdateArmor for SPELL_AURA_MOD_ATTACK_POWER_OF_ARMOR
		UpdateAttackPowerAndDamage(true);
		UpdateMaxHealth();

		for (var i = PowerType.Mana; i < PowerType.Max; ++i)
			UpdateMaxPower(i);

		UpdateAllRatings();
		UpdateAllCritPercentages();
		UpdateSpellCritChance();
		UpdateBlockPercentage();
		UpdateParryPercentage();
		UpdateDodgePercentage();
		UpdateSpellDamageAndHealingBonus();
		UpdateManaRegen();
		UpdateExpertise(WeaponAttackType.BaseAttack);
		UpdateExpertise(WeaponAttackType.OffAttack);
		RecalculateRating(CombatRating.ArmorPenetration);
		UpdateAllResistances();

		return true;
	}

	public override bool UpdateStats(Stats stat)
	{
		// value = ((base_value * base_pct) + total_value) * total_pct
		var value = GetTotalStatValue(stat);

		SetStat(stat, (int)value);

		if (stat == Stats.Stamina || stat == Stats.Intellect || stat == Stats.Strength)
		{
			var pet = CurrentPet;

			if (pet != null)
				pet.UpdateStats(stat);
		}

		switch (stat)
		{
			case Stats.Agility:
				UpdateAllCritPercentages();
				UpdateDodgePercentage();

				break;
			case Stats.Stamina:
				UpdateMaxHealth();

				break;
			case Stats.Intellect:
				UpdateSpellCritChance();

				break;
			default:
				break;
		}

		if (stat == Stats.Strength)
		{
			UpdateAttackPowerAndDamage(false);
		}
		else if (stat == Stats.Agility)
		{
			UpdateAttackPowerAndDamage(false);
			UpdateAttackPowerAndDamage(true);
		}

		UpdateArmor();
		UpdateSpellDamageAndHealingBonus();
		UpdateManaRegen();

		return true;
	}

	public override void UpdateResistances(SpellSchools school)
	{
		if (school > SpellSchools.Normal)
		{
			base.UpdateResistances(school);

			var pet = CurrentPet;

			if (pet != null)
				pet.UpdateResistances(school);
		}
		else
		{
			UpdateArmor();
		}
	}

	public void ApplyModTargetResistance(int mod, bool apply)
	{
		ApplyModUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModTargetResistance), mod, apply);
	}

	public void ApplyModTargetPhysicalResistance(int mod, bool apply)
	{
		ApplyModUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModTargetPhysicalResistance), mod, apply);
	}

	public void RecalculateRating(CombatRating cr)
	{
		ApplyRatingMod(cr, 0, true);
	}

	public void ApplyModDamageDonePos(SpellSchools school, int mod, bool apply)
	{
		ApplyModUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModDamageDonePos, (int)school), mod, apply);
	}

	public void ApplyModDamageDoneNeg(SpellSchools school, int mod, bool apply)
	{
		ApplyModUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModDamageDoneNeg, (int)school), mod, apply);
	}

	public void ApplyModDamageDonePercent(SpellSchools school, float pct, bool apply)
	{
		ApplyPercentModUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModDamageDonePercent, (int)school), pct, apply);
	}

	public void SetModDamageDonePercent(SpellSchools school, float pct)
	{
		SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModDamageDonePercent, (int)school), pct);
	}

	public void ApplyRatingMod(CombatRating combatRating, int value, bool apply)
	{
		_baseRatingValue[(int)combatRating] += (apply ? value : -value);

		UpdateRating(combatRating);
	}

	public override void CalculateMinMaxDamage(WeaponAttackType attType, bool normalized, bool addTotalPct, out double min_damage, out double max_damage)
	{
		UnitMods unitMod;

		switch (attType)
		{
			case WeaponAttackType.BaseAttack:
			default:
				unitMod = UnitMods.DamageMainHand;

				break;
			case WeaponAttackType.OffAttack:
				unitMod = UnitMods.DamageOffHand;

				break;
			case WeaponAttackType.RangedAttack:
				unitMod = UnitMods.DamageRanged;

				break;
		}

		var attackPowerMod = Math.Max(GetAPMultiplier(attType, normalized), 0.25f);

		var baseValue = GetFlatModifierValue(unitMod, UnitModifierFlatType.Base) + GetTotalAttackPowerValue(attType, false) / 3.5f * attackPowerMod;
		var basePct = GetPctModifierValue(unitMod, UnitModifierPctType.Base);
		var totalValue = GetFlatModifierValue(unitMod, UnitModifierFlatType.Total);
		var totalPct = addTotalPct ? GetPctModifierValue(unitMod, UnitModifierPctType.Total) : 1.0f;

		var weaponMinDamage = GetWeaponDamageRange(attType, WeaponDamageRange.MinDamage);
		var weaponMaxDamage = GetWeaponDamageRange(attType, WeaponDamageRange.MaxDamage);

		double versaDmgMod = 1.0f;

		MathFunctions.AddPct(ref versaDmgMod, GetRatingBonusValue(CombatRating.VersatilityDamageDone) + (float)GetTotalAuraModifier(AuraType.ModVersatility));

		var shapeshift = CliDB.SpellShapeshiftFormStorage.LookupByKey(ShapeshiftForm);

		if (shapeshift != null && shapeshift.CombatRoundTime != 0)
		{
			weaponMinDamage = weaponMinDamage * shapeshift.CombatRoundTime / 1000.0f / attackPowerMod;
			weaponMaxDamage = weaponMaxDamage * shapeshift.CombatRoundTime / 1000.0f / attackPowerMod;
		}
		else if (!CanUseAttackType(attType)) //check if player not in form but still can't use (disarm case)
		{
			//cannot use ranged/off attack, set values to 0
			if (attType != WeaponAttackType.BaseAttack)
			{
				min_damage = 0;
				max_damage = 0;

				return;
			}

			weaponMinDamage = SharedConst.BaseMinDamage;
			weaponMaxDamage = SharedConst.BaseMaxDamage;
		}

		min_damage = ((baseValue + weaponMinDamage) * basePct + totalValue) * totalPct * versaDmgMod;
		max_damage = ((baseValue + weaponMaxDamage) * basePct + totalValue) * totalPct * versaDmgMod;
	}

	public void UpdateAllCritPercentages()
	{
		var value = 5.0f;

		SetBaseModPctValue(BaseModGroup.CritPercentage, value);
		SetBaseModPctValue(BaseModGroup.OffhandCritPercentage, value);
		SetBaseModPctValue(BaseModGroup.RangedCritPercentage, value);

		UpdateCritPercentage(WeaponAttackType.BaseAttack);
		UpdateCritPercentage(WeaponAttackType.OffAttack);
		UpdateCritPercentage(WeaponAttackType.RangedAttack);
	}

	public void UpdateManaRegen()
	{
		var manaIndex = GetPowerIndex(PowerType.Mana);

		if (manaIndex == (int)PowerType.Max)
			return;

		// Get base of Mana Pool in sBaseMPGameTable
		Global.ObjectMgr.GetPlayerClassLevelInfo(Class, Level, out var basemana);
		double base_regen = basemana / 100.0f;

		base_regen += GetTotalAuraModifierByMiscValue(AuraType.ModPowerRegen, (int)PowerType.Mana);

		// Apply PCT bonus from SPELL_AURA_MOD_POWER_REGEN_PERCENT
		base_regen *= GetTotalAuraMultiplierByMiscValue(AuraType.ModPowerRegenPercent, (int)PowerType.Mana);

		// Apply PCT bonus from SPELL_AURA_MOD_MANA_REGEN_PCT
		base_regen *= GetTotalAuraMultiplierByMiscValue(AuraType.ModManaRegenPct, (int)PowerType.Mana);

		SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.PowerRegenFlatModifier, (int)manaIndex), (float)base_regen);
		SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.PowerRegenInterruptedFlatModifier, (int)manaIndex), (float)base_regen);
	}

	public void UpdateSpellDamageAndHealingBonus()
	{
		// Magic damage modifiers implemented in Unit.SpellDamageBonusDone
		// This information for client side use only
		// Get healing bonus for all schools
		SetUpdateFieldStatValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModHealingDonePos), (int)SpellBaseHealingBonusDone(SpellSchoolMask.All));
		// Get damage bonus for all schools
		var modDamageAuras = GetAuraEffectsByType(AuraType.ModDamageDone);

		for (var i = (int)SpellSchools.Holy; i < (int)SpellSchools.Max; ++i)
		{
			SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModDamageDoneNeg, i),
								(int)modDamageAuras.Aggregate(0f,
															(negativeMod, aurEff) =>
															{
																if (aurEff.Amount < 0 && Convert.ToBoolean(aurEff.MiscValue & (1 << i)))
																	negativeMod += (float)aurEff.Amount;

																return negativeMod;
															}));

			SetUpdateFieldStatValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModDamageDonePos, i),
									(int)(SpellBaseDamageBonusDone((SpellSchoolMask)(1 << i)) - ActivePlayerData.ModDamageDoneNeg[i]));
		}

		if (HasAuraType(AuraType.OverrideAttackPowerBySpPct))
		{
			UpdateAttackPowerAndDamage();
			UpdateAttackPowerAndDamage(true);
		}
	}

	public uint GetBaseSpellPowerBonus()
	{
		return _baseSpellPower;
	}

	public override void UpdateAttackPowerAndDamage(bool ranged = false)
	{
		float val2;
		float level = Level;

		var entry = CliDB.ChrClassesStorage.LookupByKey(Class);
		var unitMod = ranged ? UnitMods.AttackPowerRanged : UnitMods.AttackPower;

		if (!HasAuraType(AuraType.OverrideAttackPowerBySpPct))
		{
			if (!ranged)
			{
				var strengthValue = Math.Max((GetStat(Stats.Strength)) * entry.AttackPowerPerStrength, 0.0f);
				var agilityValue = Math.Max((GetStat(Stats.Agility)) * entry.AttackPowerPerAgility, 0.0f);

				var form = CliDB.SpellShapeshiftFormStorage.LookupByKey((uint)ShapeshiftForm);

				// Directly taken from client, SHAPESHIFT_FLAG_AP_FROM_STRENGTH ?
				if (form != null && Convert.ToBoolean((uint)form.Flags & 0x20))
					agilityValue += Math.Max(GetStat(Stats.Agility) * entry.AttackPowerPerStrength, 0.0f);

				val2 = strengthValue + agilityValue;
			}
			else
			{
				val2 = (level + Math.Max(GetStat(Stats.Agility), 0.0f)) * entry.RangedAttackPowerPerAgility;
			}
		}
		else
		{
			int minSpellPower = ActivePlayerData.ModHealingDonePos;

			for (var i = SpellSchools.Holy; i < SpellSchools.Max; ++i)
				minSpellPower = Math.Min(minSpellPower, ActivePlayerData.ModDamageDonePos[(int)i]);

			val2 = MathFunctions.CalculatePct(minSpellPower, ActivePlayerData.OverrideAPBySpellPowerPercent);
		}

		SetStatFlatModifier(unitMod, UnitModifierFlatType.Base, val2);

		var base_attPower = GetFlatModifierValue(unitMod, UnitModifierFlatType.Base) * GetPctModifierValue(unitMod, UnitModifierPctType.Base);
		var attPowerMod = GetFlatModifierValue(unitMod, UnitModifierFlatType.Total);
		var attPowerMultiplier = GetPctModifierValue(unitMod, UnitModifierPctType.Total) - 1.0f;

		if (ranged)
		{
			SetRangedAttackPower((int)base_attPower);
			SetRangedAttackPowerModPos((int)attPowerMod);
			SetRangedAttackPowerMultiplier((int)attPowerMultiplier);
		}
		else
		{
			SetAttackPower((int)base_attPower);
			SetAttackPowerModPos((int)attPowerMod);
			SetAttackPowerMultiplier((int)attPowerMultiplier);
		}

		var pet = CurrentPet; //update pet's AP
		var guardian = GetGuardianPet();

		//automatically update weapon damage after attack power modification
		if (ranged)
		{
			UpdateDamagePhysical(WeaponAttackType.RangedAttack);

			if (pet != null && pet.IsHunterPet) // At ranged attack change for hunter pet
				pet.UpdateAttackPowerAndDamage();
		}
		else
		{
			UpdateDamagePhysical(WeaponAttackType.BaseAttack);
			var offhand = GetWeaponForAttack(WeaponAttackType.OffAttack, true);

			if (offhand)
				if (CanDualWield || offhand.Template.HasFlag(ItemFlags3.AlwaysAllowDualWield))
					UpdateDamagePhysical(WeaponAttackType.OffAttack);

			if (HasAuraType(AuraType.OverrideSpellPowerByApPct))
				UpdateSpellDamageAndHealingBonus();

			if (pet != null && pet.IsPetGhoul()) // At melee attack power change for DK pet
				pet.UpdateAttackPowerAndDamage();

			if (guardian != null && guardian.IsSpiritWolf()) // At melee attack power change for Shaman feral spirit
				guardian.UpdateAttackPowerAndDamage();
		}
	}

	public override void UpdateArmor()
	{
		var unitMod = UnitMods.Armor;

		var value = GetFlatModifierValue(unitMod, UnitModifierFlatType.Base); // base armor
		value *= GetPctModifierValue(unitMod, UnitModifierPctType.Base);      // armor percent

		// SPELL_AURA_MOD_ARMOR_PCT_FROM_STAT counts as base armor
		GetTotalAuraModifier(AuraType.ModArmorPctFromStat,
							aurEff =>
							{
								var miscValue = aurEff.MiscValue;
								var stat = (miscValue != -2) ? (Stats)miscValue : GetPrimaryStat();

								value += MathFunctions.CalculatePct((float)GetStat(stat), aurEff.Amount);

								return true;
							});

		var baseValue = value;

		value += GetFlatModifierValue(unitMod, UnitModifierFlatType.Total); // bonus armor from auras and items
		value *= GetPctModifierValue(unitMod, UnitModifierPctType.Total);
		value *= GetTotalAuraMultiplier(AuraType.ModBonusArmorPct);

		SetArmor((int)value, (int)(value - baseValue));

		var pet = CurrentPet;

		if (pet)
			pet.UpdateArmor();

		UpdateAttackPowerAndDamage(); // armor dependent auras update for SPELL_AURA_MOD_ATTACK_POWER_OF_ARMOR
	}

	public void UpdateRating(CombatRating cr)
	{
		var amount = _baseRatingValue[(int)cr];

		foreach (var aurEff in GetAuraEffectsByType(AuraType.ModCombatRatingFromCombatRating))
			if ((aurEff.MiscValueB & (1 << (int)cr)) != 0)
			{
				short? highestRating = null;

				for (byte dependentRating = 0; dependentRating < (int)CombatRating.Max; ++dependentRating)
					if ((aurEff.MiscValue & (1 << dependentRating)) != 0)
						highestRating = (short)Math.Max(highestRating.HasValue ? highestRating.Value : _baseRatingValue[dependentRating], _baseRatingValue[dependentRating]);

				if (highestRating != 0)
					amount += MathFunctions.CalculatePct(highestRating.Value, aurEff.Amount);
			}

		foreach (var aurEff in GetAuraEffectsByType(AuraType.ModRatingPct))
			if (Convert.ToBoolean(aurEff.MiscValue & (1 << (int)cr)))
				amount += MathFunctions.CalculatePct(amount, aurEff.Amount);

		if (amount < 0)
			amount = 0;

		var oldRating = ActivePlayerData.CombatRatings[(int)cr];
		SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.CombatRatings, (int)cr), (uint)amount);

		var affectStats = CanModifyStats();

		switch (cr)
		{
			case CombatRating.Amplify:
			case CombatRating.DefenseSkill:
				break;
			case CombatRating.Dodge:
				UpdateDodgePercentage();

				break;
			case CombatRating.Parry:
				UpdateParryPercentage();

				break;
			case CombatRating.Block:
				UpdateBlockPercentage();

				break;
			case CombatRating.HitMelee:
				UpdateMeleeHitChances();

				break;
			case CombatRating.HitRanged:
				UpdateRangedHitChances();

				break;
			case CombatRating.HitSpell:
				UpdateSpellHitChances();

				break;
			case CombatRating.CritMelee:
				if (affectStats)
				{
					UpdateCritPercentage(WeaponAttackType.BaseAttack);
					UpdateCritPercentage(WeaponAttackType.OffAttack);
				}

				break;
			case CombatRating.CritRanged:
				if (affectStats)
					UpdateCritPercentage(WeaponAttackType.RangedAttack);

				break;
			case CombatRating.CritSpell:
				if (affectStats)
					UpdateSpellCritChance();

				break;
			case CombatRating.Corruption:
			case CombatRating.CorruptionResistance:
				UpdateCorruption();

				break;
			case CombatRating.HasteMelee:
			case CombatRating.HasteRanged:
			case CombatRating.HasteSpell:
			{
				// explicit affected values
				var multiplier = GetRatingMultiplier(cr);
				var oldVal = ApplyRatingDiminishing(cr, oldRating * multiplier);
				var newVal = ApplyRatingDiminishing(cr, amount * multiplier);

				switch (cr)
				{
					case CombatRating.HasteMelee:
						ApplyAttackTimePercentMod(WeaponAttackType.BaseAttack, oldVal, false);
						ApplyAttackTimePercentMod(WeaponAttackType.OffAttack, oldVal, false);
						ApplyAttackTimePercentMod(WeaponAttackType.BaseAttack, newVal, true);
						ApplyAttackTimePercentMod(WeaponAttackType.OffAttack, newVal, true);

						if (Class == PlayerClass.Deathknight)
							UpdateAllRunesRegen();

						break;
					case CombatRating.HasteRanged:
						ApplyAttackTimePercentMod(WeaponAttackType.RangedAttack, oldVal, false);
						ApplyAttackTimePercentMod(WeaponAttackType.RangedAttack, newVal, true);

						break;
					case CombatRating.HasteSpell:
						ApplyCastTimePercentMod(oldVal, false);
						ApplyCastTimePercentMod(newVal, true);

						break;
					default:
						break;
				}

				break;
			}
			case CombatRating.Expertise:
				if (affectStats)
				{
					UpdateExpertise(WeaponAttackType.BaseAttack);
					UpdateExpertise(WeaponAttackType.OffAttack);
				}

				break;
			case CombatRating.ArmorPenetration:
				if (affectStats)
					UpdateArmorPenetration(amount);

				break;
			case CombatRating.Mastery:
				UpdateMastery();

				break;
			case CombatRating.VersatilityDamageDone:
				UpdateVersatilityDamageDone();

				break;
			case CombatRating.VersatilityHealingDone:
				UpdateHealingDonePercentMod();

				break;
		}
	}

	public void UpdateMastery()
	{
		if (!CanUseMastery())
		{
			SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Mastery), 0.0f);

			return;
		}

		var value = GetTotalAuraModifier(AuraType.Mastery);
		value += GetRatingBonusValue(CombatRating.Mastery);
		SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Mastery), (float)value);

		var chrSpec = CliDB.ChrSpecializationStorage.LookupByKey(GetPrimarySpecialization());

		if (chrSpec == null)
			return;

		foreach (var masterySpellId in chrSpec.MasterySpellID)
		{
			var aura = GetAura(masterySpellId);

			if (aura != null)
				foreach (var spellEffectInfo in aura.SpellInfo.Effects)
				{
					var mult = spellEffectInfo.BonusCoefficient;

					if (MathFunctions.fuzzyEq(mult, 0.0f))
						continue;

					aura.GetEffect(spellEffectInfo.EffectIndex).ChangeAmount(value * mult);
				}
		}
	}

	public void UpdateVersatilityDamageDone()
	{
		// No proof that CR_VERSATILITY_DAMAGE_DONE is allways = ActivePlayerData::Versatility
		SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Versatility), (int)ActivePlayerData.CombatRatings[(int)CombatRating.VersatilityDamageDone]);

		if (Class == PlayerClass.Hunter)
			UpdateDamagePhysical(WeaponAttackType.RangedAttack);
		else
			UpdateDamagePhysical(WeaponAttackType.BaseAttack);
	}

	public void UpdateHealingDonePercentMod()
	{
		double value = 1.0f;

		MathFunctions.AddPct(ref value, GetRatingBonusValue(CombatRating.VersatilityHealingDone) + GetTotalAuraModifier(AuraType.ModVersatility));

		foreach (var auraEffect in GetAuraEffectsByType(AuraType.ModHealingDonePercent))
			MathFunctions.AddPct(ref value, auraEffect.Amount);

		var val = (float)value;

		for (var i = 0; i < (int)SpellSchools.Max; ++i)
			SetUpdateFieldStatValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModHealingDonePercent, i), val);
	}

	public void UpdateParryPercentage()
	{
		// No parry
		double value = 0.0f;
		var pclass = (int)Class - 1;

		if (CanParry && parry_cap[pclass] > 0.0f)
		{
			double nondiminishing = 5.0f;
			// Parry from rating
			var diminishing = GetRatingBonusValue(CombatRating.Parry);
			// Parry from SPELL_AURA_MOD_PARRY_PERCENT aura
			nondiminishing += GetTotalAuraModifier(AuraType.ModParryPercent);

			// apply diminishing formula to diminishing parry chance
			value = CalculateDiminishingReturns(parry_cap, Class, nondiminishing, diminishing);

			if (WorldConfig.GetBoolValue(WorldCfg.StatsLimitsEnable))
				value = value > WorldConfig.GetFloatValue(WorldCfg.StatsLimitsParry) ? WorldConfig.GetFloatValue(WorldCfg.StatsLimitsParry) : value;
		}

		SetUpdateFieldStatValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ParryPercentage), (float)value);
	}

	public void UpdateDodgePercentage()
	{
		double diminishing = 0.0f, nondiminishing = 0.0f;
		GetDodgeFromAgility(diminishing, nondiminishing);
		// Dodge from SPELL_AURA_MOD_DODGE_PERCENT aura
		nondiminishing += GetTotalAuraModifier(AuraType.ModDodgePercent);
		// Dodge from rating
		diminishing += GetRatingBonusValue(CombatRating.Dodge);
		// apply diminishing formula to diminishing dodge chance
		var value = CalculateDiminishingReturns(dodge_cap, Class, nondiminishing, diminishing);

		if (WorldConfig.GetBoolValue(WorldCfg.StatsLimitsEnable))
			value = value > WorldConfig.GetFloatValue(WorldCfg.StatsLimitsDodge) ? WorldConfig.GetFloatValue(WorldCfg.StatsLimitsDodge) : value;

		SetUpdateFieldStatValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.DodgePercentage), (float)value);
	}

	public void UpdateBlockPercentage()
	{
		// No block
		double value = 0.0f;

		if (CanBlock)
		{
			// Base value
			value = 5.0f;
			// Increase from SPELL_AURA_MOD_BLOCK_PERCENT aura
			value += GetTotalAuraModifier(AuraType.ModBlockPercent);
			// Increase from rating
			value += GetRatingBonusValue(CombatRating.Block);

			if (WorldConfig.GetBoolValue(WorldCfg.StatsLimitsEnable))
				value = value > WorldConfig.GetFloatValue(WorldCfg.StatsLimitsBlock) ? WorldConfig.GetFloatValue(WorldCfg.StatsLimitsBlock) : value;
		}

		SetUpdateFieldStatValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.BlockPercentage), (float)value);
	}

	public void UpdateCritPercentage(WeaponAttackType attType)
	{
		static float applyCritLimit(double value)
		{
			if (WorldConfig.GetBoolValue(WorldCfg.StatsLimitsEnable))
				value = value > WorldConfig.GetFloatValue(WorldCfg.StatsLimitsCrit) ? WorldConfig.GetFloatValue(WorldCfg.StatsLimitsCrit) : value;

			return (float)value;
		}

		switch (attType)
		{
			case WeaponAttackType.OffAttack:
				SetUpdateFieldStatValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.OffhandCritPercentage),
										applyCritLimit(GetBaseModValue(BaseModGroup.OffhandCritPercentage, BaseModType.FlatMod) + GetBaseModValue(BaseModGroup.OffhandCritPercentage, BaseModType.PctMod) + GetRatingBonusValue(CombatRating.CritMelee)));

				break;
			case WeaponAttackType.RangedAttack:
				SetUpdateFieldStatValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.RangedCritPercentage),
										applyCritLimit(GetBaseModValue(BaseModGroup.RangedCritPercentage, BaseModType.FlatMod) + GetBaseModValue(BaseModGroup.RangedCritPercentage, BaseModType.PctMod) + GetRatingBonusValue(CombatRating.CritRanged)));

				break;
			case WeaponAttackType.BaseAttack:
			default:
				SetUpdateFieldStatValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.CritPercentage),
										applyCritLimit(GetBaseModValue(BaseModGroup.CritPercentage, BaseModType.FlatMod) + GetBaseModValue(BaseModGroup.CritPercentage, BaseModType.PctMod) + GetRatingBonusValue(CombatRating.CritMelee)));

				break;
		}
	}

	public void UpdateExpertise(WeaponAttackType attack)
	{
		if (attack == WeaponAttackType.RangedAttack)
			return;

		var expertise = (int)GetRatingBonusValue(CombatRating.Expertise);

		var weapon = GetWeaponForAttack(attack, true);

		expertise += (int)GetTotalAuraModifier(AuraType.ModExpertise, aurEff => aurEff.SpellInfo.IsItemFitToSpellRequirements(weapon));

		if (expertise < 0)
			expertise = 0;

		switch (attack)
		{
			case WeaponAttackType.BaseAttack:
				SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.MainhandExpertise), expertise);

				break;
			case WeaponAttackType.OffAttack:
				SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.OffhandExpertise), expertise);

				break;
			default: break;
		}
	}

	public void UpdateSpellCritChance()
	{
		// For others recalculate it from:
		double crit = 5.0f;
		// Increase crit from SPELL_AURA_MOD_SPELL_CRIT_CHANCE
		crit += GetTotalAuraModifier(AuraType.ModSpellCritChance);
		// Increase crit from SPELL_AURA_MOD_CRIT_PCT
		crit += GetTotalAuraModifier(AuraType.ModCritPct);
		// Increase crit from spell crit ratings
		crit += GetRatingBonusValue(CombatRating.CritSpell);

		// Store crit value
		SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.SpellCritPercentage), (float)crit);
	}

	public void UpdateMeleeHitChances()
	{
		ModMeleeHitChance = 7.5f + GetRatingBonusValue(CombatRating.HitMelee);
	}

	public void UpdateRangedHitChances()
	{
		ModRangedHitChance = 7.5f + GetRatingBonusValue(CombatRating.HitRanged);
	}

	public void UpdateSpellHitChances()
	{
		ModSpellHitChance = 15.0f + GetTotalAuraModifier(AuraType.ModSpellHitChance);
		ModSpellHitChance += GetRatingBonusValue(CombatRating.HitSpell);
	}

	public override void UpdateMaxHealth()
	{
		var unitMod = UnitMods.Health;

		var value = GetFlatModifierValue(unitMod, UnitModifierFlatType.Base) + GetCreateHealth();
		value *= GetPctModifierValue(unitMod, UnitModifierPctType.Base);
		value += GetFlatModifierValue(unitMod, UnitModifierFlatType.Total) + GetHealthBonusFromStamina();
		value *= GetPctModifierValue(unitMod, UnitModifierPctType.Total);

		SetMaxHealth(value);
	}

	public override uint GetPowerIndex(PowerType powerType)
	{
		return Global.DB2Mgr.GetPowerIndexByClass(powerType, Class);
	}

	public override void UpdateMaxPower(PowerType power)
	{
		var powerIndex = GetPowerIndex(power);

		if (powerIndex == (uint)PowerType.Max || powerIndex >= (uint)PowerType.MaxPerClass)
			return;

		var unitMod = UnitMods.PowerStart + (int)power;

		var value = GetFlatModifierValue(unitMod, UnitModifierFlatType.Base) + GetCreatePowerValue(power);
		value *= GetPctModifierValue(unitMod, UnitModifierPctType.Base);
		value += GetFlatModifierValue(unitMod, UnitModifierFlatType.Total);
		value *= GetPctModifierValue(unitMod, UnitModifierPctType.Total);

		SetMaxPower(power, (int)Math.Round(value));
	}

	public void ApplySpellPenetrationBonus(int amount, bool apply)
	{
		ApplyModTargetResistance(-amount, apply);
		_spellPenetrationItemMod += apply ? amount : -amount;
	}

	public bool _ModifyUInt32(bool apply, ref uint baseValue, ref int amount)
	{
		// If amount is negative, change sign and value of apply.
		if (amount < 0)
		{
			apply = !apply;
			amount = -amount;
		}

		if (apply)
		{
			baseValue += (uint)amount;
		}
		else
		{
			// Make sure we do not get public uint overflow.
			if (amount > baseValue)
				amount = (int)baseValue;

			baseValue -= (uint)amount;
		}

		return apply;
	}

	void _ApplyAllStatBonuses()
	{
		SetCanModifyStats(false);

		_ApplyAllAuraStatMods();
		_ApplyAllItemMods();
		ApplyAllAzeriteItemMods(true);

		SetCanModifyStats(true);

		UpdateAllStats();
	}

	void _RemoveAllStatBonuses()
	{
		SetCanModifyStats(false);

		ApplyAllAzeriteItemMods(false);
		_RemoveAllItemMods();
		_RemoveAllAuraStatMods();

		SetCanModifyStats(true);

		UpdateAllStats();
	}

	void UpdateAllRatings()
	{
		for (CombatRating cr = 0; cr < CombatRating.Max; ++cr)
			UpdateRating(cr);
	}

	void UpdateCorruption()
	{
		var effectiveCorruption = GetRatingBonusValue(CombatRating.Corruption) - GetRatingBonusValue(CombatRating.CorruptionResistance);

		foreach (var corruptionEffect in CliDB.CorruptionEffectsStorage.Values)
		{
			if (((CorruptionEffectsFlag)corruptionEffect.Flags).HasAnyFlag(CorruptionEffectsFlag.Disabled))
				continue;

			if (effectiveCorruption < corruptionEffect.MinCorruption)
			{
				RemoveAura(corruptionEffect.Aura);

				continue;
			}

			var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(corruptionEffect.PlayerConditionID);

			if (playerCondition != null)
				if (!ConditionManager.IsPlayerMeetingCondition(this, playerCondition))
				{
					RemoveAura(corruptionEffect.Aura);

					continue;
				}

			CastSpell(this, corruptionEffect.Aura, true);
		}
	}

	void UpdateArmorPenetration(int amount)
	{
		// Store Rating Value
		SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.CombatRatings, (int)CombatRating.ArmorPenetration), (uint)amount);
	}

	double CalculateDiminishingReturns(float[] capArray, PlayerClass playerClass, double nonDiminishValue, double diminishValue)
	{
		float[] m_diminishing_k =
		{
			0.9560f, // Warrior
			0.9560f, // Paladin
			0.9880f, // Hunter
			0.9880f, // Rogue
			0.9830f, // Priest
			0.9560f, // DK
			0.9880f, // Shaman
			0.9830f, // Mage
			0.9830f, // Warlock
			0.9830f, // Monk
			0.9720f, // Druid
			0.9830f, // Demon Hunter
			0.9880f, // Evoker
			1.0f,    // Adventurer
		};

		//  1     1     k              cx
		// --- = --- + --- <=> x' = --------
		//  x'    c     x            x + ck

		// where:
		// k  is m_diminishing_k for that class
		// c  is capArray for that class
		// x  is chance before DR (diminishValue)
		// x' is chance after DR (our result)

		var classIdx = (byte)playerClass - 1u;

		var k = m_diminishing_k[classIdx];
		var c = capArray[classIdx];

		var result = c * diminishValue / (diminishValue + c * k);
		result += nonDiminishValue;

		return result;
	}

	float GetGameTableColumnForCombatRating(GtCombatRatingsRecord row, CombatRating rating)
	{
		switch (rating)
		{
			case CombatRating.Amplify:
				return row.Amplify;
			case CombatRating.DefenseSkill:
				return row.DefenseSkill;
			case CombatRating.Dodge:
				return row.Dodge;
			case CombatRating.Parry:
				return row.Parry;
			case CombatRating.Block:
				return row.Block;
			case CombatRating.HitMelee:
				return row.HitMelee;
			case CombatRating.HitRanged:
				return row.HitRanged;
			case CombatRating.HitSpell:
				return row.HitSpell;
			case CombatRating.CritMelee:
				return row.CritMelee;
			case CombatRating.CritRanged:
				return row.CritRanged;
			case CombatRating.CritSpell:
				return row.CritSpell;
			case CombatRating.Corruption:
				return row.Corruption;
			case CombatRating.CorruptionResistance:
				return row.CorruptionResistance;
			case CombatRating.Speed:
				return row.Speed;
			case CombatRating.ResilienceCritTaken:
				return row.ResilienceCritTaken;
			case CombatRating.ResiliencePlayerDamage:
				return row.ResiliencePlayerDamage;
			case CombatRating.Lifesteal:
				return row.Lifesteal;
			case CombatRating.HasteMelee:
				return row.HasteMelee;
			case CombatRating.HasteRanged:
				return row.HasteRanged;
			case CombatRating.HasteSpell:
				return row.HasteSpell;
			case CombatRating.Avoidance:
				return row.Avoidance;
			case CombatRating.Studiness:
				return row.Sturdiness;
			case CombatRating.Unused7:
				return row.Unused7;
			case CombatRating.Expertise:
				return row.Expertise;
			case CombatRating.ArmorPenetration:
				return row.ArmorPenetration;
			case CombatRating.Mastery:
				return row.Mastery;
			case CombatRating.PvpPower:
				return row.PvPPower;
			case CombatRating.Cleave:
				return row.Cleave;
			case CombatRating.VersatilityDamageDone:
				return row.VersatilityDamageDone;
			case CombatRating.VersatilityHealingDone:
				return row.VersatilityHealingDone;
			case CombatRating.VersatilityDamageTaken:
				return row.VersatilityDamageTaken;
			case CombatRating.Unused12:
				return row.Unused12;
			default:
				break;
		}

		return 1.0f;
	}

	Stats GetPrimaryStat()
	{
		byte primaryStatPriority;
		var specialization = CliDB.ChrSpecializationStorage.LookupByKey(GetPrimarySpecialization());

		if (specialization != null)
			primaryStatPriority = (byte)specialization.PrimaryStatPriority;
		else
			primaryStatPriority = CliDB.ChrClassesStorage.LookupByKey(Class).PrimaryStatPriority;


		if (primaryStatPriority >= 4)
			return Stats.Strength;

		if (primaryStatPriority >= 2)
			return Stats.Agility;

		return Stats.Intellect;
	}

	float GetHealthBonusFromStamina()
	{
		// Taken from PaperDollFrame.lua - 6.0.3.19085
		var ratio = 10.0f;
		var hpBase = CliDB.HpPerStaGameTable.GetRow(Level);

		if (hpBase != null)
			ratio = hpBase.Health;

		var stamina = GetStat(Stats.Stamina);

		return stamina * ratio;
	}

	void ApplyManaRegenBonus(int amount, bool apply)
	{
		_ModifyUInt32(apply, ref _baseManaRegen, ref amount);
		UpdateManaRegen();
	}

	void ApplyHealthRegenBonus(int amount, bool apply)
	{
		_ModifyUInt32(apply, ref _baseHealthRegen, ref amount);
	}

	void ApplySpellPowerBonus(int amount, bool apply)
	{
		if (HasAuraType(AuraType.OverrideSpellPowerByApPct))
			return;

		apply = _ModifyUInt32(apply, ref _baseSpellPower, ref amount);

		// For speed just update for client
		ApplyModUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModHealingDonePos), amount, apply);

		for (var spellSchool = SpellSchools.Holy; spellSchool < SpellSchools.Max; ++spellSchool)
			ApplyModDamageDonePos(spellSchool, amount, apply);

		if (HasAuraType(AuraType.OverrideAttackPowerBySpPct))
		{
			UpdateAttackPowerAndDamage();
			UpdateAttackPowerAndDamage(true);
		}
	}
}
using System;
using Framework.Constants;

namespace Game.Entities;

public partial class Creature
{
	public override bool UpdateStats(Stats stat)
	{
		return true;
	}

	public override bool UpdateAllStats()
	{
		UpdateMaxHealth();
		UpdateAttackPowerAndDamage();
		UpdateAttackPowerAndDamage(true);

		for (var i = PowerType.Mana; i < PowerType.Max; ++i)
			UpdateMaxPower(i);

		UpdateAllResistances();

		return true;
	}

	public override void UpdateArmor()
	{
		var baseValue = GetFlatModifierValue(UnitMods.Armor, UnitModifierFlatType.Base);
		var value = GetTotalAuraModValue(UnitMods.Armor);
		SetArmor((int)baseValue, (int)(value - baseValue));
	}

	public override void UpdateMaxHealth()
	{
		var value = GetTotalAuraModValue(UnitMods.Health);
		SetMaxHealth(value);
	}

	public override uint GetPowerIndex(PowerType powerType)
	{
		if (powerType == GetPowerType())
			return 0;

		if (powerType == PowerType.AlternatePower)
			return 1;

		if (powerType == PowerType.ComboPoints)
			return 2;

		return (uint)PowerType.Max;
	}

	public override void UpdateMaxPower(PowerType power)
	{
		if (GetPowerIndex(power) == (uint)PowerType.Max)
			return;

		var unitMod = UnitMods.PowerStart + (int)power;

		var value = GetFlatModifierValue(unitMod, UnitModifierFlatType.Base) + GetCreatePowerValue(power);
		value *= GetPctModifierValue(unitMod, UnitModifierPctType.Base);
		value += GetFlatModifierValue(unitMod, UnitModifierFlatType.Total);
		value *= GetPctModifierValue(unitMod, UnitModifierPctType.Total);

		SetMaxPower(power, (int)Math.Round(value));
	}

	public override void UpdateAttackPowerAndDamage(bool ranged = false)
	{
		var unitMod = ranged ? UnitMods.AttackPowerRanged : UnitMods.AttackPower;

		var baseAttackPower = GetFlatModifierValue(unitMod, UnitModifierFlatType.Base) * GetPctModifierValue(unitMod, UnitModifierPctType.Base);
		var attackPowerMultiplier = GetPctModifierValue(unitMod, UnitModifierPctType.Total) - 1.0f;

		if (ranged)
		{
			SetRangedAttackPower((int)baseAttackPower);
			SetRangedAttackPowerMultiplier((float)attackPowerMultiplier);
		}
		else
		{
			SetAttackPower((int)baseAttackPower);
			SetAttackPowerMultiplier((float)attackPowerMultiplier);
		}

		//automatically update weapon damage after attack power modification
		if (ranged)
		{
			UpdateDamagePhysical(WeaponAttackType.RangedAttack);
		}
		else
		{
			UpdateDamagePhysical(WeaponAttackType.BaseAttack);
			UpdateDamagePhysical(WeaponAttackType.OffAttack);
		}
	}

	public override void CalculateMinMaxDamage(WeaponAttackType attType, bool normalized, bool addTotalPct, out double minDamage, out double maxDamage)
	{
		float variance;
		UnitMods unitMod;

		switch (attType)
		{
			case WeaponAttackType.BaseAttack:
			default:
				variance = GetCreatureTemplate().BaseVariance;
				unitMod = UnitMods.DamageMainHand;

				break;
			case WeaponAttackType.OffAttack:
				variance = GetCreatureTemplate().BaseVariance;
				unitMod = UnitMods.DamageOffHand;

				break;
			case WeaponAttackType.RangedAttack:
				variance = GetCreatureTemplate().RangeVariance;
				unitMod = UnitMods.DamageRanged;

				break;
		}

		if (attType == WeaponAttackType.OffAttack && !HaveOffhandWeapon())
		{
			minDamage = 0.0f;
			maxDamage = 0.0f;

			return;
		}

		var weaponMinDamage = GetWeaponDamageRange(attType, WeaponDamageRange.MinDamage);
		var weaponMaxDamage = GetWeaponDamageRange(attType, WeaponDamageRange.MaxDamage);

		if (!CanUseAttackType(attType)) // disarm case
		{
			weaponMinDamage = 0.0f;
			weaponMaxDamage = 0.0f;
		}

		var attackPower = GetTotalAttackPowerValue(attType, false);
		var attackSpeedMulti = Math.Max(GetAPMultiplier(attType, normalized), 0.25f);

		var baseValue = GetFlatModifierValue(unitMod, UnitModifierFlatType.Base) + (attackPower / 3.5f) * variance;
		var basePct = GetPctModifierValue(unitMod, UnitModifierPctType.Base) * attackSpeedMulti;
		var totalValue = GetFlatModifierValue(unitMod, UnitModifierFlatType.Total);
		var totalPct = addTotalPct ? GetPctModifierValue(unitMod, UnitModifierPctType.Total) : 1.0f;
		var dmgMultiplier = GetCreatureTemplate().ModDamage; // = ModDamage * _GetDamageMod(rank);

		minDamage = ((weaponMinDamage + baseValue) * dmgMultiplier * basePct + totalValue) * totalPct;
		maxDamage = ((weaponMaxDamage + baseValue) * dmgMultiplier * basePct + totalValue) * totalPct;
	}
}
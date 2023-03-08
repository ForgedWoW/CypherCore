// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Networking.Packets;
using Game.Scripting.Interfaces.IPlayer;
using Game.Spells;

namespace Game.Entities;

public partial class Unit
{
	public void HandleStatFlatModifier(UnitMods unitMod, UnitModifierFlatType modifierType, double amount, bool apply)
	{
		if (unitMod >= UnitMods.End || modifierType >= UnitModifierFlatType.End)
		{
			Log.outError(LogFilter.Unit, "ERROR in HandleStatFlatModifier(): non-existing UnitMods or wrong UnitModifierFlatType!");

			return;
		}

		if (amount == 0)
			return;

		switch (modifierType)
		{
			case UnitModifierFlatType.Base:
			case UnitModifierFlatType.BasePCTExcludeCreate:
			case UnitModifierFlatType.Total:
				AuraFlatModifiersGroup[(int)unitMod][(int)modifierType] += apply ? amount : -amount;

				break;
			default:
				break;
		}

		UpdateUnitMod(unitMod);
	}

	public void ApplyStatPctModifier(UnitMods unitMod, UnitModifierPctType modifierType, double pct)
	{
		if (unitMod >= UnitMods.End || modifierType >= UnitModifierPctType.End)
		{
			Log.outError(LogFilter.Unit, "ERROR in ApplyStatPctModifier(): non-existing UnitMods or wrong UnitModifierPctType!");

			return;
		}

		if (pct == 0)
			return;

		switch (modifierType)
		{
			case UnitModifierPctType.Base:
			case UnitModifierPctType.Total:
				MathFunctions.AddPct(ref AuraPctModifiersGroup[(int)unitMod][(int)modifierType], pct);

				break;
			default:
				break;
		}

		UpdateUnitMod(unitMod);
	}

	public void SetStatFlatModifier(UnitMods unitMod, UnitModifierFlatType modifierType, double val)
	{
		if (AuraFlatModifiersGroup[(int)unitMod][(int)modifierType] == val)
			return;

		AuraFlatModifiersGroup[(int)unitMod][(int)modifierType] = val;
		UpdateUnitMod(unitMod);
	}

	public void SetStatPctModifier(UnitMods unitMod, UnitModifierPctType modifierType, double val)
	{
		if (AuraPctModifiersGroup[(int)unitMod][(int)modifierType] == val)
			return;

		AuraPctModifiersGroup[(int)unitMod][(int)modifierType] = val;
		UpdateUnitMod(unitMod);
	}

	public double GetFlatModifierValue(UnitMods unitMod, UnitModifierFlatType modifierType)
	{
		if (unitMod >= UnitMods.End || modifierType >= UnitModifierFlatType.End)
		{
			Log.outError(LogFilter.Unit, "attempt to access non-existing modifier value from UnitMods!");

			return 0.0f;
		}

		return AuraFlatModifiersGroup[(int)unitMod][(int)modifierType];
	}

	public double GetPctModifierValue(UnitMods unitMod, UnitModifierPctType modifierType)
	{
		if (unitMod >= UnitMods.End || modifierType >= UnitModifierPctType.End)
		{
			Log.outError(LogFilter.Unit, "attempt to access non-existing modifier value from UnitMods!");

			return 0.0f;
		}

		return AuraPctModifiersGroup[(int)unitMod][(int)modifierType];
	}

	public int ModifyPower(PowerType power, double dVal, bool withPowerUpdate = true)
	{
		return ModifyPower(power, (int)dVal, withPowerUpdate);
	}

	// returns negative amount on power reduction
	public int ModifyPower(PowerType power, int dVal, bool withPowerUpdate = true)
	{
		var gain = 0;

		if (dVal == 0)
			return 0;

		if (dVal > 0)
			dVal *= (int)GetTotalAuraMultiplierByMiscValue(AuraType.ModPowerGainPct, (int)power);

		var curPower = GetPower(power);

		var val = (dVal + curPower);

		if (val <= GetMinPower(power))
		{
			SetPower(power, GetMinPower(power), withPowerUpdate);

			return -curPower;
		}

		var maxPower = GetMaxPower(power);

		if (val < maxPower)
		{
			SetPower(power, val, withPowerUpdate);
			gain = val - curPower;
		}
		else if (curPower != maxPower)
		{
			SetPower(power, maxPower, withPowerUpdate);
			gain = maxPower - curPower;
		}

		return gain;
	}

	public void UpdateStatBuffMod(Stats stat)
	{
		double modPos = 0.0f;
		double modNeg = 0.0f;
		double factor = 0.0f;

		var unitMod = UnitMods.StatStart + (int)stat;

		// includes value from items and enchantments
		var modValue = GetFlatModifierValue(unitMod, UnitModifierFlatType.Base);

		if (modValue > 0.0f)
			modPos += modValue;
		else
			modNeg += modValue;

		if (IsGuardian)
		{
			modValue = ((Guardian)this).GetBonusStatFromOwner(stat);

			if (modValue > 0.0f)
				modPos += modValue;
			else
				modNeg += modValue;
		}

		// SPELL_AURA_MOD_STAT_BONUS_PCT only affects BASE_VALUE
		modPos = MathFunctions.CalculatePct(modPos, Math.Max(GetFlatModifierValue(unitMod, UnitModifierFlatType.BasePCTExcludeCreate), -100.0f));
		modNeg = MathFunctions.CalculatePct(modNeg, Math.Max(GetFlatModifierValue(unitMod, UnitModifierFlatType.BasePCTExcludeCreate), -100.0f));

		modPos += GetTotalAuraModifier(AuraType.ModStat,
										aurEff =>
										{
											if ((aurEff.MiscValue < 0 || aurEff.MiscValue == (int)stat) && aurEff.Amount > 0)
												return true;

											return false;
										});

		modNeg += GetTotalAuraModifier(AuraType.ModStat,
										aurEff =>
										{
											if ((aurEff.MiscValue < 0 || aurEff.MiscValue == (int)stat) && aurEff.Amount < 0)
												return true;

											return false;
										});

		factor = GetTotalAuraMultiplier(AuraType.ModPercentStat,
										aurEff =>
										{
											if (aurEff.MiscValue == -1 || aurEff.MiscValue == (int)stat)
												return true;

											return false;
										});

		factor *= GetTotalAuraMultiplier(AuraType.ModTotalStatPercentage,
										aurEff =>
										{
											if (aurEff.MiscValue == -1 || aurEff.MiscValue == (int)stat)
												return true;

											return false;
										});

		modPos *= factor;
		modNeg *= factor;

		_floatStatPosBuff[(int)stat] = modPos;
		_floatStatNegBuff[(int)stat] = modNeg;

		UpdateStatBuffModForClient(stat);
	}

	public virtual bool UpdateStats(Stats stat)
	{
		return false;
	}

	public virtual bool UpdateAllStats()
	{
		return false;
	}

	public virtual void UpdateResistances(SpellSchools school)
	{
		if (school > SpellSchools.Normal)
		{
			var unitMod = UnitMods.ResistanceStart + (int)school;
			var value = MathFunctions.CalculatePct(GetFlatModifierValue(unitMod, UnitModifierFlatType.Base), Math.Max(GetFlatModifierValue(unitMod, UnitModifierFlatType.BasePCTExcludeCreate), -100.0f));
			value *= GetPctModifierValue(unitMod, UnitModifierPctType.Base);

			var baseValue = value;

			value += GetFlatModifierValue(unitMod, UnitModifierFlatType.Total);
			value *= GetPctModifierValue(unitMod, UnitModifierPctType.Total);

			SetResistance(school, (int)value);
			SetBonusResistanceMod(school, (int)(value - baseValue));
		}
		else
		{
			UpdateArmor();
		}
	}

	public virtual void UpdateArmor() { }

	public virtual void UpdateMaxHealth() { }

	public virtual void UpdateMaxPower(PowerType power) { }

	public virtual void UpdateAttackPowerAndDamage(bool ranged = false) { }

	public virtual void UpdateDamagePhysical(WeaponAttackType attType)
	{
		CalculateMinMaxDamage(attType, false, true, out var minDamage, out var maxDamage);

		switch (attType)
		{
			case WeaponAttackType.BaseAttack:
			default:
				SetUpdateFieldStatValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.MinDamage), (float)minDamage);
				SetUpdateFieldStatValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.MaxDamage), (float)maxDamage);

				break;
			case WeaponAttackType.OffAttack:
				SetUpdateFieldStatValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.MinOffHandDamage), (float)minDamage);
				SetUpdateFieldStatValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.MaxOffHandDamage), (float)maxDamage);

				break;
			case WeaponAttackType.RangedAttack:
				SetUpdateFieldStatValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.MinRangedDamage), (float)minDamage);
				SetUpdateFieldStatValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.MaxRangedDamage), (float)maxDamage);

				break;
		}
	}

	public virtual void CalculateMinMaxDamage(WeaponAttackType attType, bool normalized, bool addTotalPct, out double minDamage, out double maxDamage)
	{
		minDamage = 0f;
		maxDamage = 0f;
	}

	public void UpdateAllResistances()
	{
		for (var i = SpellSchools.Normal; i < SpellSchools.Max; ++i)
			UpdateResistances(i);
	}

	//Stats
	public float GetStat(Stats stat)
	{
		return UnitData.Stats[(int)stat];
	}

	public void SetStat(Stats stat, int val)
	{
		SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.Stats, (int)stat), val);
	}

	public uint GetCreateMana()
	{
		return UnitData.BaseMana;
	}

	public void SetCreateMana(uint val)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.BaseMana), val);
	}

	public uint GetArmor()
	{
		return (uint)GetResistance(SpellSchools.Normal);
	}

	public void SetArmor(int val, int bonusVal)
	{
		SetResistance(SpellSchools.Normal, val);
		SetBonusResistanceMod(SpellSchools.Normal, bonusVal);
	}

	public float GetCreateStat(Stats stat)
	{
		return CreateStats[(int)stat];
	}

	public void SetCreateStat(Stats stat, float val)
	{
		CreateStats[(int)stat] = val;
	}

	public float GetPosStat(Stats stat)
	{
		return UnitData.StatPosBuff[(int)stat];
	}

	public float GetNegStat(Stats stat)
	{
		return UnitData.StatNegBuff[(int)stat];
	}

	public int GetResistance(SpellSchools school)
	{
		return UnitData.Resistances[(int)school];
	}

	public int GetBonusResistanceMod(SpellSchools school)
	{
		return UnitData.BonusResistanceMods[(int)school];
	}

	public int GetResistance(SpellSchoolMask mask)
	{
		int? resist = null;

		for (var i = (int)SpellSchools.Normal; i < (int)SpellSchools.Max; ++i)
		{
			var schoolResistance = GetResistance((SpellSchools)i);

			if (Convert.ToBoolean((int)mask & (1 << i)) && (!resist.HasValue || resist.Value > schoolResistance))
				resist = schoolResistance;
		}

		// resist value will never be negative here
		return resist.HasValue ? resist.Value : 0;
	}

	public void SetResistance(SpellSchools school, int val)
	{
		SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.Resistances, (int)school), val);
	}

	public void SetBonusResistanceMod(SpellSchools school, int val)
	{
		SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.BonusResistanceMods, (int)school), val);
	}

	public void SetModCastingSpeed(float castingSpeed)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ModCastingSpeed), castingSpeed);
	}

	public void SetModSpellHaste(float spellHaste)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ModSpellHaste), spellHaste);
	}

	public void SetModHaste(float haste)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ModHaste), haste);
	}

	public void SetModRangedHaste(float rangedHaste)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ModRangedHaste), rangedHaste);
	}

	public void SetModHasteRegen(float hasteRegen)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ModHasteRegen), hasteRegen);
	}

	public void SetModTimeRate(float timeRate)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ModTimeRate), timeRate);
	}

	public void InitStatBuffMods()
	{
		for (var stat = Stats.Strength; stat < Stats.Max; ++stat)
		{
			_floatStatPosBuff[(int)stat] = 0.0f;
			_floatStatNegBuff[(int)stat] = 0.0f;
			UpdateStatBuffModForClient(stat);
		}
	}

	public bool CanModifyStats()
	{
		return _canModifyStats;
	}

	public void SetCanModifyStats(bool modifyStats)
	{
		_canModifyStats = modifyStats;
	}

	public double GetTotalStatValue(Stats stat)
	{
		var unitMod = UnitMods.StatStart + (int)stat;

		var value = MathFunctions.CalculatePct(GetFlatModifierValue(unitMod, UnitModifierFlatType.Base), Math.Max(GetFlatModifierValue(unitMod, UnitModifierFlatType.BasePCTExcludeCreate), -100.0f));
		value += GetCreateStat(stat);
		value *= GetPctModifierValue(unitMod, UnitModifierPctType.Base);
		value += GetFlatModifierValue(unitMod, UnitModifierFlatType.Total);
		value *= GetPctModifierValue(unitMod, UnitModifierPctType.Total);

		return value;
	}

	//Health  
	public uint GetCreateHealth()
	{
		return UnitData.BaseHealth;
	}

	public void SetCreateHealth(uint val)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.BaseHealth), val);
	}

	public long GetHealth()
	{
		return UnitData.Health;
	}


	public void SetHealth(float val)
	{
		SetHealth((long)val);
	}

	public void SetHealth(double val)
	{
		SetHealth((long)val);
	}

	public void SetHealth(int val)
	{
		SetHealth((long)val);
	}

	public void SetHealth(uint val)
	{
		SetHealth((long)val);
	}

	public void SetHealth(long val)
	{
		if (DeathState == DeathState.JustDied || DeathState == DeathState.Corpse)
		{
			val = 0;
		}
		else if (IsTypeId(TypeId.Player) && DeathState == DeathState.Dead)
		{
			val = 1;
		}
		else
		{
			var maxHealth = GetMaxHealth();

			if (maxHealth < val)
				val = maxHealth;
		}

		var oldVal = GetHealth();
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Health), val);

		TriggerOnHealthChangeAuras(oldVal, val);

		// group update
		var player = AsPlayer;

		if (player)
		{
			if (player.GetGroup())
				player.SetGroupUpdateFlag(GroupUpdateFlags.CurHp);
		}
		else if (IsPet)
		{
			var pet = AsCreature.AsPet;

			if (pet.IsControlled())
				pet.SetGroupUpdateFlag(GroupUpdatePetFlags.CurHp);
		}
	}

	public long GetMaxHealth()
	{
		return UnitData.MaxHealth;
	}

	public void SetMaxHealth(double val)
	{
		SetMaxHealth((long)val);
	}

	public void SetMaxHealth(long val)
	{
		if (val == 0)
			val = 1;

		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.MaxHealth), val);
		var health = GetHealth();

		// group update
		if (IsTypeId(TypeId.Player))
		{
			if (AsPlayer.GetGroup())
				AsPlayer.SetGroupUpdateFlag(GroupUpdateFlags.MaxHp);
		}
		else if (IsPet)
		{
			var pet = AsCreature.AsPet;

			if (pet.IsControlled())
				pet.SetGroupUpdateFlag(GroupUpdatePetFlags.MaxHp);
		}

		if (val < health)
			SetHealth(val);
	}

	public float GetHealthPct()
	{
		return GetMaxHealth() != 0 ? 100.0f * GetHealth() / GetMaxHealth() : 0.0f;
	}

	public void SetFullHealth()
	{
		SetHealth(GetMaxHealth());
	}

	public bool IsFullHealth()
	{
		return GetHealth() == GetMaxHealth();
	}

	public bool HealthBelowPct(double pct)
	{
		return GetHealth() < CountPctFromMaxHealth(pct);
	}

	public bool HealthBelowPct(int pct)
	{
		return GetHealth() < CountPctFromMaxHealth(pct);
	}

	public bool HealthBelowPctDamaged(int pct, double damage)
	{
		return GetHealth() - damage < CountPctFromMaxHealth(pct);
	}

	public bool HealthBelowPctDamaged(double pct, double damage)
	{
		return GetHealth() - damage < CountPctFromMaxHealth(pct);
	}

	public bool HealthAbovePct(double pct)
	{
		return GetHealth() > CountPctFromMaxHealth(pct);
	}

	public bool HealthAbovePct(int pct)
	{
		return GetHealth() > CountPctFromMaxHealth(pct);
	}

	public long CountPctFromMaxHealth(double pct)
	{
		return MathFunctions.CalculatePct(GetMaxHealth(), pct);
	}

	public long CountPctFromMaxHealth(int pct)
	{
		return MathFunctions.CalculatePct(GetMaxHealth(), pct);
	}

	public long CountPctFromCurHealth(double pct)
	{
		return MathFunctions.CalculatePct(GetHealth(), pct);
	}

	public int CountPctFromMaxPower(PowerType power, double pct)
	{
		return MathFunctions.CalculatePct(GetMaxPower(power), pct);
	}

	public virtual float GetHealthMultiplierForTarget(WorldObject target)
	{
		return 1.0f;
	}

	public virtual float GetDamageMultiplierForTarget(WorldObject target)
	{
		return 1.0f;
	}

	public virtual float GetArmorMultiplierForTarget(WorldObject target)
	{
		return 1.0f;
	}

	//Powers
	public PowerType GetPowerType()
	{
		return (PowerType)(byte)UnitData.DisplayPower;
	}

	public void SetPowerType(PowerType powerType, bool sendUpdate = true)
	{
		if (GetPowerType() == powerType)
			return;

		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.DisplayPower), (byte)powerType);

		if (!sendUpdate)
			return;

		var thisPlayer = AsPlayer;

		if (thisPlayer != null)
			if (thisPlayer.GetGroup())
				thisPlayer.SetGroupUpdateFlag(GroupUpdateFlags.PowerType);
		/*else if (IsPet()) TODO 6.x
		{
		    Pet pet = ToCreature().ToPet();
		    if (pet.isControlled())
		        pet.SetGroupUpdateFlag(GROUP_UPDATE_FLAG_PET_POWER_TYPE);
		}*/

		// Update max power
		UpdateMaxPower(powerType);

		// Update current power
		switch (powerType)
		{
			case PowerType.Mana: // Keep the same (druid form switching...)
			case PowerType.Energy:
				break;
			case PowerType.Rage: // Reset to zero
				SetPower(PowerType.Rage, 0);

				break;
			case PowerType.Focus: // Make it full
				SetFullPower(powerType);

				break;
			default:
				break;
		}
	}

	public void SetOverrideDisplayPowerId(uint powerDisplayId)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.OverrideDisplayPowerID), powerDisplayId);
	}

	public void SetMaxPower(PowerType powerType, int val)
	{
		var powerIndex = GetPowerIndex(powerType);

		if (powerIndex == (int)PowerType.Max || powerIndex >= (int)PowerType.MaxPerClass)
			return;

		var cur_power = GetPower(powerType);
		SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.MaxPower, (int)powerIndex), (uint)val);

		// group update
		if (IsTypeId(TypeId.Player))
			if (AsPlayer.GetGroup())
				AsPlayer.SetGroupUpdateFlag(GroupUpdateFlags.MaxPower);
		/*else if (IsPet()) TODO 6.x
		{
		    Pet pet = ToCreature().ToPet();
		    if (pet.isControlled())
		        pet.SetGroupUpdateFlag(GROUP_UPDATE_FLAG_PET_MAX_POWER);
		}*/

		if (val < cur_power)
			SetPower(powerType, val);
	}

	public void SetPower(PowerType powerType, float val, bool withPowerUpdate = true, bool isRegen = false)
	{
		SetPower(powerType, (int)val, withPowerUpdate, isRegen);
	}

	public void SetPower(PowerType powerType, int val, bool withPowerUpdate = true, bool isRegen = false)
	{
		var powerIndex = GetPowerIndex(powerType);

		if (powerIndex == (int)PowerType.Max || powerIndex >= (int)PowerType.MaxPerClass)
			return;

		var maxPower = GetMaxPower(powerType);

		if (maxPower < val)
			val = maxPower;

		var oldPower = UnitData.Power[(int)powerIndex];

		if (TryGetAsPlayer(out var player))
		{
			var newVal = val;
			Global.ScriptMgr.ForEach<IPlayerOnModifyPower>(player.Class, p => p.OnModifyPower(player, powerType, oldPower, ref val, isRegen));
			val = newVal;
		}

		SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.Power, (int)powerIndex), val);

		if (IsInWorld && withPowerUpdate)
		{
			PowerUpdate packet = new();
			packet.Guid = GUID;
			packet.Powers.Add(new PowerUpdatePower(val, (byte)powerType));
			SendMessageToSet(packet, IsTypeId(TypeId.Player));
		}

		TriggerOnPowerChangeAuras(powerType, oldPower, val);

		// group update
		if (IsTypeId(TypeId.Player))
			if (player.GetGroup())
				player.SetGroupUpdateFlag(GroupUpdateFlags.CurPower);
		/*else if (IsPet()) TODO 6.x
		{
		    Pet pet = ToCreature().ToPet();
		    if (pet.isControlled())
		        pet.SetGroupUpdateFlag(GROUP_UPDATE_FLAG_PET_CUR_POWER);
		}*/

		if (IsPlayer)
			Global.ScriptMgr.ForEach<IPlayerOnAfterModifyPower>(player.Class, p => p.OnAfterModifyPower(player, powerType, oldPower, val, isRegen));
	}

	public void SetFullPower(PowerType powerType)
	{
		SetPower(powerType, GetMaxPower(powerType));
	}

	public int GetPower(PowerType powerType)
	{
		var powerIndex = GetPowerIndex(powerType);

		if (powerIndex == (int)PowerType.Max || powerIndex >= (int)PowerType.MaxPerClass)
			return 0;

		return UnitData.Power[(int)powerIndex];
	}

	public int GetMaxPower(PowerType powerType)
	{
		var powerIndex = GetPowerIndex(powerType);

		if (powerIndex == (int)PowerType.Max || powerIndex >= (int)PowerType.MaxPerClass)
			return 0;

		return (int)(uint)UnitData.MaxPower[(int)powerIndex];
	}

	public int GetCreatePowerValue(PowerType powerType)
	{
		if (powerType == PowerType.Mana)
			return (int)GetCreateMana();

		var powerTypeEntry = Global.DB2Mgr.GetPowerTypeEntry(powerType);

		if (powerTypeEntry != null)
			return powerTypeEntry.MaxBasePower;

		return 0;
	}

	public virtual uint GetPowerIndex(PowerType powerType)
	{
		return 0;
	}

	public float GetPowerPct(PowerType powerType)
	{
		return GetMaxPower(powerType) != 0 ? 100.0f * GetPower(powerType) / GetMaxPower(powerType) : 0.0f;
	}

	public bool CanApplyResilience()
	{
		return !IsVehicle && OwnerGUID.IsPlayer;
	}

	public static void ApplyResilience(Unit victim, ref double damage)
	{
		// player mounted on multi-passenger mount is also classified as vehicle
		if (victim.IsVehicle && !victim.IsPlayer)
			return;

		Unit target = null;

		if (victim.IsPlayer)
		{
			target = victim;
		}
		else // victim->GetTypeId() == TYPEID_UNIT
		{
			var owner = victim.GetOwner();

			if (owner != null)
				if (owner.IsPlayer)
					target = owner;
		}

		if (!target)
			return;

		damage -= target.GetDamageReduction(damage);
	}

	public double CalculateAOEAvoidance(double damage, uint schoolMask, ObjectGuid casterGuid)
	{
		damage = (damage * GetTotalAuraMultiplierByMiscMask(AuraType.ModAoeDamageAvoidance, schoolMask));

		if (casterGuid.IsAnyTypeCreature)
			damage = (damage * GetTotalAuraMultiplierByMiscMask(AuraType.ModCreatureAoeDamageAvoidance, schoolMask));

		return damage;
	}

	public void SetAttackPower(int attackPower)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.AttackPower), attackPower);
	}

	public void SetAttackPowerModPos(int attackPowerMod)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.AttackPowerModPos), attackPowerMod);
	}

	public void SetAttackPowerModNeg(int attackPowerMod)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.AttackPowerModNeg), attackPowerMod);
	}

	public void SetAttackPowerMultiplier(float attackPowerMult)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.AttackPowerMultiplier), attackPowerMult);
	}

	public void SetRangedAttackPower(int attackPower)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.RangedAttackPower), attackPower);
	}

	public void SetRangedAttackPowerModPos(int attackPowerMod)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.RangedAttackPowerModPos), attackPowerMod);
	}

	public void SetRangedAttackPowerModNeg(int attackPowerMod)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.RangedAttackPowerModNeg), attackPowerMod);
	}

	public void SetRangedAttackPowerMultiplier(float attackPowerMult)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.RangedAttackPowerMultiplier), attackPowerMult);
	}

	public void SetMainHandWeaponAttackPower(int attackPower)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.MainHandWeaponAttackPower), attackPower);
	}

	public void SetOffHandWeaponAttackPower(int attackPower)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.OffHandWeaponAttackPower), attackPower);
	}

	public void SetRangedWeaponAttackPower(int attackPower)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.RangedWeaponAttackPower), attackPower);
	}

	//Chances
	public override double MeleeSpellMissChance(Unit victim, WeaponAttackType attType, SpellInfo spellInfo)
	{
		if (spellInfo != null && spellInfo.HasAttribute(SpellAttr7.NoAttackMiss))
			return 0.0f;

		//calculate miss chance
		double missChance = victim.GetUnitMissChance();

		// melee attacks while dual wielding have +19% chance to miss
		if (spellInfo == null && HaveOffhandWeapon() && !IsInFeralForm && !HasAuraType(AuraType.IgnoreDualWieldHitPenalty))
			missChance += 19.0f;

		// Spellmod from SpellModOp.HitChance
		double resistMissChance = 100.0f;

		if (spellInfo != null)
		{
			var modOwner = GetSpellModOwner();

			if (modOwner != null)
				modOwner.ApplySpellMod(spellInfo, SpellModOp.HitChance, ref resistMissChance);
		}

		missChance += resistMissChance - 100.0f;

		if (attType == WeaponAttackType.RangedAttack)
			missChance -= ModRangedHitChance;
		else
			missChance -= ModMeleeHitChance;

		// miss chance from auras after calculating skill based miss
		missChance -= GetTotalAuraModifier(AuraType.ModHitChance);

		if (attType == WeaponAttackType.RangedAttack)
			missChance -= victim.GetTotalAuraModifier(AuraType.ModAttackerRangedHitChance);
		else
			missChance -= victim.GetTotalAuraModifier(AuraType.ModAttackerMeleeHitChance);

		return Math.Max(missChance, 0f);
	}

	public double GetUnitCriticalChanceAgainst(WeaponAttackType attackType, Unit victim)
	{
		var chance = GetUnitCriticalChanceDone(attackType);

		return victim.GetUnitCriticalChanceTaken(this, attackType, chance);
	}

	public int GetMechanicResistChance(SpellInfo spellInfo)
	{
		if (spellInfo == null)
			return 0;

		double resistMech = 0;

		foreach (var spellEffectInfo in spellInfo.Effects)
		{
			if (!spellEffectInfo.IsEffect())
				break;

			var effect_mech = (int)spellInfo.GetEffectMechanic(spellEffectInfo.EffectIndex);

			if (effect_mech != 0)
			{
				var temp = GetTotalAuraModifierByMiscValue(AuraType.ModMechanicResistance, effect_mech);

				if (resistMech < temp)
					resistMech = temp;
			}
		}

		return Math.Max((int)resistMech, 0);
	}

	public void ApplyModManaCostMultiplier(float manaCostMultiplier, bool apply)
	{
		ApplyModUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ManaCostMultiplier), manaCostMultiplier, apply);
	}

	public void ApplyModManaCostModifier(SpellSchools school, int mod, bool apply)
	{
		ApplyModUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.ManaCostModifier, (int)school), mod, apply);
	}

	void UpdateUnitMod(UnitMods unitMod)
	{
		if (!CanModifyStats())
			return;

		switch (unitMod)
		{
			case UnitMods.StatStrength:
			case UnitMods.StatAgility:
			case UnitMods.StatStamina:
			case UnitMods.StatIntellect:
				UpdateStats(GetStatByAuraGroup(unitMod));

				break;
			case UnitMods.Armor:
				UpdateArmor();

				break;
			case UnitMods.Health:
				UpdateMaxHealth();

				break;
			case UnitMods.Mana:
			case UnitMods.Rage:
			case UnitMods.Focus:
			case UnitMods.Energy:
			case UnitMods.ComboPoints:
			case UnitMods.Runes:
			case UnitMods.RunicPower:
			case UnitMods.SoulShards:
			case UnitMods.LunarPower:
			case UnitMods.HolyPower:
			case UnitMods.Alternate:
			case UnitMods.Maelstrom:
			case UnitMods.Chi:
			case UnitMods.Insanity:
			case UnitMods.BurningEmbers:
			case UnitMods.DemonicFury:
			case UnitMods.ArcaneCharges:
			case UnitMods.Fury:
			case UnitMods.Pain:
				UpdateMaxPower((PowerType)(unitMod - UnitMods.PowerStart));

				break;
			case UnitMods.ResistanceHoly:
			case UnitMods.ResistanceFire:
			case UnitMods.ResistanceNature:
			case UnitMods.ResistanceFrost:
			case UnitMods.ResistanceShadow:
			case UnitMods.ResistanceArcane:
				UpdateResistances(GetSpellSchoolByAuraGroup(unitMod));

				break;
			case UnitMods.AttackPower:
				UpdateAttackPowerAndDamage();

				break;
			case UnitMods.AttackPowerRanged:
				UpdateAttackPowerAndDamage(true);

				break;
			case UnitMods.DamageMainHand:
				UpdateDamagePhysical(WeaponAttackType.BaseAttack);

				break;
			case UnitMods.DamageOffHand:
				UpdateDamagePhysical(WeaponAttackType.OffAttack);

				break;
			case UnitMods.DamageRanged:
				UpdateDamagePhysical(WeaponAttackType.RangedAttack);

				break;
			default:
				break;
		}
	}

	int GetMinPower(PowerType power)
	{
		return power == PowerType.LunarPower ? -100 : 0;
	}

	Stats GetStatByAuraGroup(UnitMods unitMod)
	{
		var stat = Stats.Strength;

		switch (unitMod)
		{
			case UnitMods.StatStrength:
				stat = Stats.Strength;

				break;
			case UnitMods.StatAgility:
				stat = Stats.Agility;

				break;
			case UnitMods.StatStamina:
				stat = Stats.Stamina;

				break;
			case UnitMods.StatIntellect:
				stat = Stats.Intellect;

				break;
			default:
				break;
		}

		return stat;
	}

	void UpdateStatBuffModForClient(Stats stat)
	{
		SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.StatPosBuff, (int)stat), (int)_floatStatPosBuff[(int)stat]);
		SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.StatNegBuff, (int)stat), (int)_floatStatNegBuff[(int)stat]);
	}

	void TriggerOnPowerChangeAuras(PowerType power, int oldVal, int newVal)
	{
		var effects = GetAuraEffectsByType(AuraType.TriggerSpellOnPowerPct);
		var effectsAmount = GetAuraEffectsByType(AuraType.TriggerSpellOnPowerAmount);
		effects.AddRange(effectsAmount);

		foreach (var effect in effects)
			if (effect.MiscValue == (int)power)
			{
				var effectAmount = effect.Amount;
				var triggerSpell = effect.GetSpellEffectInfo().TriggerSpell;

				float oldValueCheck = oldVal;
				float newValueCheck = newVal;

				if (effect.AuraType == AuraType.TriggerSpellOnPowerPct)
				{
					var maxPower = GetMaxPower(power);
					oldValueCheck = MathFunctions.GetPctOf(oldVal, maxPower);
					newValueCheck = MathFunctions.GetPctOf(newVal, maxPower);
				}

				switch ((AuraTriggerOnPowerChangeDirection)effect.MiscValueB)
				{
					case AuraTriggerOnPowerChangeDirection.Gain:
						if (oldValueCheck >= effect.Amount || newValueCheck < effectAmount)
							continue;

						break;
					case AuraTriggerOnPowerChangeDirection.Loss:
						if (oldValueCheck <= effect.Amount || newValueCheck > effectAmount)
							continue;

						break;
					default:
						break;
				}

				CastSpell(this, triggerSpell, new CastSpellExtraArgs(effect));
			}
	}

	// player or player's pet resilience (-1%)
	double GetDamageReduction(double damage)
	{
		return GetCombatRatingDamageReduction(CombatRating.ResiliencePlayerDamage, 1.0f, 100.0f, damage);
	}

	double GetCombatRatingReduction(CombatRating cr)
	{
		var player = AsPlayer;

		if (player)
		{
			return player.GetRatingBonusValue(cr);
		}
		// Player's pet get resilience from owner
		else if (IsPet && GetOwner())
		{
			var owner = GetOwner().AsPlayer;

			if (owner)
				return owner.GetRatingBonusValue(cr);
		}

		return 0.0f;
	}

	double GetCombatRatingDamageReduction(CombatRating cr, float rate, float cap, double damage)
	{
		var percent = Math.Min(GetCombatRatingReduction(cr) * rate, cap);

		return MathFunctions.CalculatePct(damage, percent);
	}

	double GetUnitCriticalChanceDone(WeaponAttackType attackType)
	{
		double chance = 0.0f;
		var thisPlayer = AsPlayer;

		if (thisPlayer != null)
		{
			switch (attackType)
			{
				case WeaponAttackType.BaseAttack:
					chance = thisPlayer.ActivePlayerData.CritPercentage;

					break;
				case WeaponAttackType.OffAttack:
					chance = thisPlayer.ActivePlayerData.OffhandCritPercentage;

					break;
				case WeaponAttackType.RangedAttack:
					chance = thisPlayer.ActivePlayerData.RangedCritPercentage;

					break;
			}
		}
		else
		{
			if (!AsCreature.CreatureTemplate.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.NoCrit))
			{
				chance = 5.0f;
				chance += GetTotalAuraModifier(AuraType.ModWeaponCritPercent);
				chance += GetTotalAuraModifier(AuraType.ModCritPct);
			}
		}

		return chance;
	}

	double GetUnitCriticalChanceTaken(Unit attacker, WeaponAttackType attackType, double critDone)
	{
		var chance = critDone;

		// flat aura mods
		if (attackType == WeaponAttackType.RangedAttack)
			chance += GetTotalAuraModifier(AuraType.ModAttackerRangedCritChance);
		else
			chance += GetTotalAuraModifier(AuraType.ModAttackerMeleeCritChance);

		chance += GetTotalAuraModifier(AuraType.ModCritChanceVersusTargetHealth, aurEff => !HealthBelowPct(aurEff.MiscValueB));

		chance += GetTotalAuraModifier(AuraType.ModCritChanceForCaster, aurEff => aurEff.CasterGuid == attacker.GUID);

		var tempSummon = attacker.ToTempSummon();

		if (tempSummon != null)
			chance += GetTotalAuraModifier(AuraType.ModCritChanceForCasterPet, aurEff => aurEff.CasterGuid == tempSummon.GetSummonerGUID());

		chance += GetTotalAuraModifier(AuraType.ModAttackerSpellAndWeaponCritChance);

		return Math.Max(chance, 0.0f);
	}

	double GetUnitDodgeChance(WeaponAttackType attType, Unit victim)
	{
		var levelDiff = (int)(victim.GetLevelForTarget(this) - GetLevelForTarget(victim));

		double chance = 0.0f;
		double levelBonus = 0.0f;
		var playerVictim = victim.AsPlayer;

		if (playerVictim)
		{
			chance = playerVictim.ActivePlayerData.DodgePercentage;
		}
		else
		{
			if (!victim.IsTotem)
			{
				chance = 3.0f;
				chance += victim.GetTotalAuraModifier(AuraType.ModDodgePercent);

				if (levelDiff > 0)
					levelBonus = 1.5f * levelDiff;
			}
		}

		chance += levelBonus;

		// Reduce enemy dodge chance by SPELL_AURA_MOD_COMBAT_RESULT_CHANCE
		chance += GetTotalAuraModifierByMiscValue(AuraType.ModCombatResultChance, (int)VictimState.Dodge);

		// reduce dodge by SPELL_AURA_MOD_ENEMY_DODGE
		chance += GetTotalAuraModifier(AuraType.ModEnemyDodge);

		// Reduce dodge chance by attacker expertise rating
		if (IsTypeId(TypeId.Player))
			chance -= AsPlayer.GetExpertiseDodgeOrParryReduction(attType);
		else
			chance -= GetTotalAuraModifier(AuraType.ModExpertise) / 4.0f;

		return Math.Max(chance, 0.0f);
	}

	double GetUnitParryChance(WeaponAttackType attType, Unit victim)
	{
		var levelDiff = (int)(victim.GetLevelForTarget(this) - GetLevelForTarget(victim));

		double chance = 0.0f;
		double levelBonus = 0.0f;
		var playerVictim = victim.AsPlayer;

		if (playerVictim)
		{
			if (playerVictim.CanParry)
			{
				var tmpitem = playerVictim.GetWeaponForAttack(WeaponAttackType.BaseAttack, true);

				if (!tmpitem)
					tmpitem = playerVictim.GetWeaponForAttack(WeaponAttackType.OffAttack, true);

				if (tmpitem)
					chance = playerVictim.ActivePlayerData.ParryPercentage;
			}
		}
		else
		{
			if (!victim.IsTotem && !victim.AsCreature.CreatureTemplate.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.NoParry))
			{
				chance = 6.0f;
				chance += victim.GetTotalAuraModifier(AuraType.ModParryPercent);

				if (levelDiff > 0)
					levelBonus = 1.5f * levelDiff;
			}
		}

		chance += levelBonus;

		// Reduce parry chance by attacker expertise rating
		if (IsTypeId(TypeId.Player))
			chance -= AsPlayer.GetExpertiseDodgeOrParryReduction(attType);
		else
			chance -= GetTotalAuraModifier(AuraType.ModExpertise) / 4.0f;

		return Math.Max(chance, 0.0f);
	}

	float GetUnitMissChance()
	{
		var miss_chance = 5.0f;

		return miss_chance;
	}

	double GetUnitBlockChance(WeaponAttackType attType, Unit victim)
	{
		var levelDiff = (int)(victim.GetLevelForTarget(this) - GetLevelForTarget(victim));

		double chance = 0.0f;
		double levelBonus = 0.0f;
		var playerVictim = victim.AsPlayer;

		if (playerVictim)
		{
			if (playerVictim.CanBlock)
			{
				var tmpitem = playerVictim.GetUseableItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);

				if (tmpitem && !tmpitem.IsBroken() && tmpitem.GetTemplate().GetInventoryType() == InventoryType.Shield)
					chance = playerVictim.ActivePlayerData.BlockPercentage;
			}
		}
		else
		{
			if (!victim.IsTotem && !(victim.AsCreature.CreatureTemplate.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.NoBlock)))
			{
				chance = 3.0f;
				chance += victim.GetTotalAuraModifier(AuraType.ModBlockPercent);

				if (levelDiff > 0)
					levelBonus = 1.5f * levelDiff;
			}
		}

		chance += levelBonus;

		return Math.Max(chance, 0.0f);
	}
}
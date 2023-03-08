// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Dynamic;
using Game.DataStorage;
using Game.Networking.Packets;
using Game.Scripting.Interfaces.IItem;
using Game.Spells;

namespace Game.Entities;

public partial class Player
{
	public void UpdateSkillsForLevel()
	{
		var race = GetRace();
		var maxSkill = GetMaxSkillValueForLevel();
		SkillInfo skillInfoField = ActivePlayerData.Skill;

		foreach (var pair in _skillStatus)
		{
			if (pair.Value.State == SkillState.Deleted || skillInfoField.SkillRank[pair.Value.Pos] == 0)
				continue;

			var pskill = pair.Key;
			var rcEntry = Global.DB2Mgr.GetSkillRaceClassInfo(pskill, GetRace(), GetClass());

			if (rcEntry == null)
				continue;

			if (Global.SpellMgr.GetSkillRangeType(rcEntry) == SkillRangeType.Level)
			{
				if (rcEntry.Flags.HasAnyFlag(SkillRaceClassInfoFlags.AlwaysMaxValue))
					SetSkillRank(pair.Value.Pos, maxSkill);

				SetSkillMaxRank(pair.Value.Pos, maxSkill);

				if (pair.Value.State != SkillState.New)
					pair.Value.State = SkillState.Changed;
			}

			// Update level dependent skillline spells
			LearnSkillRewardedSpells(rcEntry.SkillID, skillInfoField.SkillRank[pair.Value.Pos], race);
		}
	}

	public ushort GetSkillValue(SkillType skill)
	{
		if (skill == 0)
			return 0;

		SkillInfo skillInfo = ActivePlayerData.Skill;

		var skillStatusData = _skillStatus.LookupByKey(skill);

		if (skillStatusData == null || skillStatusData.State == SkillState.Deleted || skillInfo.SkillRank[skillStatusData.Pos] == 0)
			return 0;

		int result = skillInfo.SkillRank[skillStatusData.Pos];
		result += skillInfo.SkillTempBonus[skillStatusData.Pos];
		result += skillInfo.SkillPermBonus[skillStatusData.Pos];

		return (ushort)(result < 0 ? 0 : result);
	}

	public ushort GetPureSkillValue(SkillType skill)
	{
		if (skill == 0)
			return 0;

		SkillInfo skillInfo = ActivePlayerData.Skill;

		var skillStatusData = _skillStatus.LookupByKey((uint)skill);

		if (skillStatusData == null || skillStatusData.State == SkillState.Deleted || skillInfo.SkillRank[skillStatusData.Pos] == 0)
			return 0;

		return skillInfo.SkillRank[skillStatusData.Pos];
	}

	public ushort GetSkillStep(SkillType skill)
	{
		if (skill == 0)
			return 0;

		SkillInfo skillInfo = ActivePlayerData.Skill;

		var skillStatusData = _skillStatus.LookupByKey(skill);

		if (skillStatusData == null || skillStatusData.State == SkillState.Deleted || skillInfo.SkillRank[skillStatusData.Pos] == 0)
			return 0;

		return skillInfo.SkillStep[skillStatusData.Pos];
	}

	public ushort GetPureMaxSkillValue(SkillType skill)
	{
		if (skill == 0)
			return 0;

		SkillInfo skillInfo = ActivePlayerData.Skill;

		var skillStatusData = _skillStatus.LookupByKey(skill);

		if (skillStatusData == null || skillStatusData.State == SkillState.Deleted || skillInfo.SkillRank[skillStatusData.Pos] == 0)
			return 0;

		return skillInfo.SkillMaxRank[skillStatusData.Pos];
	}

	public ushort GetBaseSkillValue(SkillType skill)
	{
		if (skill == 0)
			return 0;

		SkillInfo skillInfo = ActivePlayerData.Skill;

		var skillStatusData = _skillStatus.LookupByKey(skill);

		if (skillStatusData == null || skillStatusData.State == SkillState.Deleted || skillInfo.SkillRank[skillStatusData.Pos] == 0)
			return 0;

		int result = skillInfo.SkillRank[skillStatusData.Pos];
		result += skillInfo.SkillPermBonus[skillStatusData.Pos];

		return (ushort)(result < 0 ? 0 : result);
	}

	public ushort GetSkillPermBonusValue(uint skill)
	{
		if (skill == 0)
			return 0;

		SkillInfo skillInfo = ActivePlayerData.Skill;

		var skillStatusData = _skillStatus.LookupByKey(skill);

		if (skillStatusData == null || skillStatusData.State == SkillState.Deleted || skillInfo.SkillRank[skillStatusData.Pos] == 0)
			return 0;

		return skillInfo.SkillPermBonus[skillStatusData.Pos];
	}

	public ushort GetSkillTempBonusValue(uint skill)
	{
		if (skill == 0)
			return 0;

		SkillInfo skillInfo = ActivePlayerData.Skill;

		var skillStatusData = _skillStatus.LookupByKey(skill);

		if (skillStatusData == null || skillStatusData.State == SkillState.Deleted || skillInfo.SkillRank[skillStatusData.Pos] == 0)
			return 0;

		return skillInfo.SkillTempBonus[skillStatusData.Pos];
	}

	public void PetSpellInitialize()
	{
		var pet = GetPet();

		if (!pet)
			return;

		Log.outDebug(LogFilter.Pet, "Pet Spells Groups");

		var charmInfo = pet.GetCharmInfo();

		PetSpells petSpellsPacket = new();
		petSpellsPacket.PetGUID = pet.GetGUID();
		petSpellsPacket.CreatureFamily = (ushort)pet.GetCreatureTemplate().Family; // creature family (required for pet talents)
		petSpellsPacket.Specialization = pet.GetSpecialization();
		petSpellsPacket.TimeLimit = (uint)pet.GetDuration();
		petSpellsPacket.ReactState = pet.GetReactState();
		petSpellsPacket.CommandState = charmInfo.GetCommandState();

		// action bar loop
		for (byte i = 0; i < SharedConst.ActionBarIndexMax; ++i)
			petSpellsPacket.ActionButtons[i] = charmInfo.GetActionBarEntry(i).packedData;

		if (pet.IsPermanentPetFor(this))
			// spells loop
			foreach (var pair in pet.Spells)
			{
				if (pair.Value.State == PetSpellState.Removed)
					continue;

				petSpellsPacket.Actions.Add(UnitActionBarEntry.MAKE_UNIT_ACTION_BUTTON(pair.Key, (uint)pair.Value.Active));
			}

		// Cooldowns
		pet.GetSpellHistory().WritePacket(petSpellsPacket);

		SendPacket(petSpellsPacket);
	}

	public bool CanSeeSpellClickOn(Creature creature)
	{
		if (!creature.HasNpcFlag(NPCFlags.SpellClick))
			return false;

		var clickBounds = Global.ObjectMgr.GetSpellClickInfoMapBounds(creature.GetEntry());

		if (clickBounds.Empty())
			return true;

		foreach (var spellClickInfo in clickBounds)
		{
			if (!spellClickInfo.IsFitToRequirements(this, creature))
				return false;

			if (Global.ConditionMgr.IsObjectMeetingSpellClickConditions(creature.GetEntry(), spellClickInfo.spellId, this, creature))
				return true;
		}

		return false;
	}

	public override SpellInfo GetCastSpellInfo(SpellInfo spellInfo)
	{
		var overrides = _overrideSpells.LookupByKey(spellInfo.Id);

		if (!overrides.Empty())
			foreach (var spellId in overrides)
			{
				var newInfo = Global.SpellMgr.GetSpellInfo(spellId, GetMap().GetDifficultyID());

				if (newInfo != null)
					return GetCastSpellInfo(newInfo);
			}

		return base.GetCastSpellInfo(spellInfo);
	}

	public void SetOverrideSpellsId(uint overrideSpellsId)
	{
		SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.OverrideSpellsID), overrideSpellsId);
	}

	public void AddOverrideSpell(uint overridenSpellId, uint newSpellId)
	{
		_overrideSpells.Add(overridenSpellId, newSpellId);
	}

	public void RemoveOverrideSpell(uint overridenSpellId, uint newSpellId)
	{
		_overrideSpells.Remove(overridenSpellId, newSpellId);
	}

	public void LearnSpecializationSpells()
	{
		var specSpells = Global.DB2Mgr.GetSpecializationSpells(GetPrimarySpecialization());

		if (specSpells != null)
			for (var j = 0; j < specSpells.Count; ++j)
			{
				var specSpell = specSpells[j];
				var spellInfo = Global.SpellMgr.GetSpellInfo(specSpell.SpellID, Difficulty.None);

				if (spellInfo == null || spellInfo.SpellLevel > GetLevel())
					continue;

				LearnSpell(specSpell.SpellID, true);

				if (specSpell.OverridesSpellID != 0)
					AddOverrideSpell(specSpell.OverridesSpellID, specSpell.SpellID);
			}
	}

	public void SendSpellCategoryCooldowns()
	{
		SpellCategoryCooldown cooldowns = new();

		var categoryCooldownAuras = GetAuraEffectsByType(AuraType.ModSpellCategoryCooldown);

		foreach (var aurEff in categoryCooldownAuras)
		{
			var categoryId = (uint)aurEff.MiscValue;
			var cooldownInfo = cooldowns.CategoryCooldowns.Find(p => p.Category == categoryId);

			if (cooldownInfo == null)
				cooldowns.CategoryCooldowns.Add(new SpellCategoryCooldown.CategoryCooldownInfo(categoryId, -(int)aurEff.Amount));
			else
				cooldownInfo.ModCooldown -= (int)aurEff.Amount;
		}

		SendPacket(cooldowns);
	}

	public bool UpdateSkillPro(SkillType skillId, int chance, uint step)
	{
		return UpdateSkillPro((uint)skillId, chance, step);
	}

	public bool UpdateSkillPro(uint skillId, int chance, uint step)
	{
		// levels sync. with spell requirement for skill levels to learn
		// bonus abilities in sSkillLineAbilityStore
		// Used only to avoid scan DBC at each skill grow
		uint[] bonusSkillLevels =
		{
			75, 150, 225, 300, 375, 450, 525, 600, 700, 850
		};

		Log.outDebug(LogFilter.Player, "UpdateSkillPro(SkillId {0}, Chance {0:D3}%)", skillId, chance / 10.0f);

		if (skillId == 0)
			return false;

		if (chance <= 0) // speedup in 0 chance case
		{
			Log.outDebug(LogFilter.Player, "Player:UpdateSkillPro Chance={0:D3}% missed", chance / 10.0f);

			return false;
		}

		var skillStatusData = _skillStatus.LookupByKey(skillId);

		if (skillStatusData == null || skillStatusData.State == SkillState.Deleted)
			return false;

		SkillInfo skillInfoField = ActivePlayerData.Skill;

		var value = skillInfoField.SkillRank[skillStatusData.Pos];
		var max = skillInfoField.SkillMaxRank[skillStatusData.Pos];

		if (max == 0 || value == 0 || value >= max)
			return false;

		if (RandomHelper.IRand(1, 1000) > chance)
		{
			Log.outDebug(LogFilter.Player, "Player:UpdateSkillPro Chance={0:F3}% missed", chance / 10.0f);

			return false;
		}

		var new_value = (ushort)(value + step);

		if (new_value > max)
			new_value = max;

		SetSkillRank(skillStatusData.Pos, new_value);

		if (skillStatusData.State != SkillState.New)
			skillStatusData.State = SkillState.Changed;

		foreach (var bsl in bonusSkillLevels)
			if (value < bsl && new_value >= bsl)
			{
				LearnSkillRewardedSpells(skillId, new_value, GetRace());

				break;
			}

		UpdateSkillEnchantments(skillId, value, new_value);
		UpdateCriteria(CriteriaType.SkillRaised, skillId);
		Log.outDebug(LogFilter.Player, "Player:UpdateSkillPro Chance={0:F3}% taken", chance / 10.0f);

		return true;
	}

	public void ApplyEnchantment(Item item, EnchantmentSlot slot, bool apply, bool apply_dur = true, bool ignore_condition = false)
	{
		if (item == null || !item.IsEquipped())
			return;

		if (slot >= EnchantmentSlot.Max)
			return;

		var enchant_id = item.GetEnchantmentId(slot);

		if (enchant_id == 0)
			return;

		var pEnchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchant_id);

		if (pEnchant == null)
			return;

		if (!ignore_condition && pEnchant.ConditionID != 0 && !EnchantmentFitsRequirements(pEnchant.ConditionID, -1))
			return;

		if (pEnchant.MinLevel > GetLevel())
			return;

		if (pEnchant.RequiredSkillID > 0 && pEnchant.RequiredSkillRank > GetSkillValue((SkillType)pEnchant.RequiredSkillID))
			return;

		// If we're dealing with a gem inside a prismatic socket we need to check the prismatic socket requirements
		// rather than the gem requirements itself. If the socket has no color it is a prismatic socket.
		if ((slot == EnchantmentSlot.Sock1 || slot == EnchantmentSlot.Sock2 || slot == EnchantmentSlot.Sock3))
		{
			if (item.GetSocketColor((uint)(slot - EnchantmentSlot.Sock1)) == 0)
			{
				// Check if the requirements for the prismatic socket are met before applying the gem stats
				var pPrismaticEnchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(item.GetEnchantmentId(EnchantmentSlot.Prismatic));

				if (pPrismaticEnchant == null || (pPrismaticEnchant.RequiredSkillID > 0 && pPrismaticEnchant.RequiredSkillRank > GetSkillValue((SkillType)pPrismaticEnchant.RequiredSkillID)))
					return;
			}

			// Cogwheel gems dont have requirement data set in SpellItemEnchantment.dbc, but they do have it in Item-sparse.db2
			var gem = item.GetGem((ushort)(slot - EnchantmentSlot.Sock1));

			if (gem != null)
			{
				var gemTemplate = Global.ObjectMgr.GetItemTemplate(gem.ItemId);

				if (gemTemplate != null)
					if (gemTemplate.GetRequiredSkill() != 0 && GetSkillValue((SkillType)gemTemplate.GetRequiredSkill()) < gemTemplate.GetRequiredSkillRank())
						return;
			}
		}

		if (!item.IsBroken())
			for (var s = 0; s < ItemConst.MaxItemEnchantmentEffects; ++s)
			{
				var enchant_display_type = (ItemEnchantmentType)pEnchant.Effect[s];
				uint enchant_amount = pEnchant.EffectPointsMin[s];
				var enchant_spell_id = pEnchant.EffectArg[s];

				switch (enchant_display_type)
				{
					case ItemEnchantmentType.None:
						break;
					case ItemEnchantmentType.CombatSpell:
						// processed in Player.CastItemCombatSpell
						break;
					case ItemEnchantmentType.Damage:
					{
						var attackType = GetAttackBySlot(item.GetSlot(), item.GetTemplate().GetInventoryType());

						if (attackType != WeaponAttackType.Max)
							UpdateDamageDoneMods(attackType, apply ? -1 : (int)slot);
					}

						break;
					case ItemEnchantmentType.EquipSpell:
						if (enchant_spell_id != 0)
						{
							if (apply)
								CastSpell(this, enchant_spell_id, item);
							else
								RemoveAurasDueToItemSpell(enchant_spell_id, item.GetGUID());
						}

						break;
					case ItemEnchantmentType.Resistance:
						if (pEnchant.ScalingClass != 0)
						{
							int scalingClass = pEnchant.ScalingClass;

							if ((UnitData.MinItemLevel != 0 || UnitData.MaxItemLevel != 0) && pEnchant.ScalingClassRestricted != 0)
								scalingClass = pEnchant.ScalingClassRestricted;

							var minLevel = pEnchant.GetFlags().HasFlag(SpellItemEnchantmentFlags.ScaleAsAGem) ? 1 : 60u;
							var scalingLevel = GetLevel();
							var maxLevel = (byte)(pEnchant.MaxLevel != 0 ? pEnchant.MaxLevel : CliDB.SpellScalingGameTable.GetTableRowCount() - 1);

							if (minLevel > GetLevel())
								scalingLevel = minLevel;
							else if (maxLevel < GetLevel())
								scalingLevel = maxLevel;

							var spellScaling = CliDB.SpellScalingGameTable.GetRow(scalingLevel);

							if (spellScaling != null)
								enchant_amount = (uint)(pEnchant.EffectScalingPoints[s] * CliDB.GetSpellScalingColumnForClass(spellScaling, scalingClass));
						}

						enchant_amount = Math.Max(enchant_amount, 1u);
						HandleStatFlatModifier((UnitMods)((uint)UnitMods.ResistanceStart + enchant_spell_id), UnitModifierFlatType.Total, enchant_amount, apply);

						break;
					case ItemEnchantmentType.Stat:
					{
						if (pEnchant.ScalingClass != 0)
						{
							int scalingClass = pEnchant.ScalingClass;

							if ((UnitData.MinItemLevel != 0 || UnitData.MaxItemLevel != 0) && pEnchant.ScalingClassRestricted != 0)
								scalingClass = pEnchant.ScalingClassRestricted;

							var minLevel = pEnchant.GetFlags().HasFlag(SpellItemEnchantmentFlags.ScaleAsAGem) ? 1 : 60u;
							var scalingLevel = GetLevel();
							var maxLevel = (byte)(pEnchant.MaxLevel != 0 ? pEnchant.MaxLevel : CliDB.SpellScalingGameTable.GetTableRowCount() - 1);

							if (minLevel > GetLevel())
								scalingLevel = minLevel;
							else if (maxLevel < GetLevel())
								scalingLevel = maxLevel;

							var spellScaling = CliDB.SpellScalingGameTable.GetRow(scalingLevel);

							if (spellScaling != null)
								enchant_amount = (uint)(pEnchant.EffectScalingPoints[s] * CliDB.GetSpellScalingColumnForClass(spellScaling, scalingClass));
						}

						enchant_amount = Math.Max(enchant_amount, 1u);

						Log.outDebug(LogFilter.Player, "Adding {0} to stat nb {1}", enchant_amount, enchant_spell_id);

						switch ((ItemModType)enchant_spell_id)
						{
							case ItemModType.Mana:
								Log.outDebug(LogFilter.Player, "+ {0} MANA", enchant_amount);
								HandleStatFlatModifier(UnitMods.Mana, UnitModifierFlatType.Base, enchant_amount, apply);

								break;
							case ItemModType.Health:
								Log.outDebug(LogFilter.Player, "+ {0} HEALTH", enchant_amount);
								HandleStatFlatModifier(UnitMods.Health, UnitModifierFlatType.Base, enchant_amount, apply);

								break;
							case ItemModType.Agility:
								Log.outDebug(LogFilter.Player, "+ {0} AGILITY", enchant_amount);
								HandleStatFlatModifier(UnitMods.StatAgility, UnitModifierFlatType.Total, enchant_amount, apply);
								UpdateStatBuffMod(Stats.Agility);

								break;
							case ItemModType.Strength:
								Log.outDebug(LogFilter.Player, "+ {0} STRENGTH", enchant_amount);
								HandleStatFlatModifier(UnitMods.StatStrength, UnitModifierFlatType.Total, enchant_amount, apply);
								UpdateStatBuffMod(Stats.Strength);

								break;
							case ItemModType.Intellect:
								Log.outDebug(LogFilter.Player, "+ {0} INTELLECT", enchant_amount);
								HandleStatFlatModifier(UnitMods.StatIntellect, UnitModifierFlatType.Total, enchant_amount, apply);
								UpdateStatBuffMod(Stats.Intellect);

								break;
							//case ItemModType.Spirit:
							//Log.outDebug(LogFilter.Player, "+ {0} SPIRIT", enchant_amount);
							//HandleStatModifier(UnitMods.StatSpirit, UnitModifierType.TotalValue, enchant_amount, apply);
							//ApplyStatBuffMod(Stats.Spirit, enchant_amount, apply);
							//break;
							case ItemModType.Stamina:
								Log.outDebug(LogFilter.Player, "+ {0} STAMINA", enchant_amount);
								HandleStatFlatModifier(UnitMods.StatStamina, UnitModifierFlatType.Total, enchant_amount, apply);
								UpdateStatBuffMod(Stats.Stamina);

								break;
							case ItemModType.DefenseSkillRating:
								ApplyRatingMod(CombatRating.DefenseSkill, (int)enchant_amount, apply);
								Log.outDebug(LogFilter.Player, "+ {0} DEFENSE", enchant_amount);

								break;
							case ItemModType.DodgeRating:
								ApplyRatingMod(CombatRating.Dodge, (int)enchant_amount, apply);
								Log.outDebug(LogFilter.Player, "+ {0} DODGE", enchant_amount);

								break;
							case ItemModType.ParryRating:
								ApplyRatingMod(CombatRating.Parry, (int)enchant_amount, apply);
								Log.outDebug(LogFilter.Player, "+ {0} PARRY", enchant_amount);

								break;
							case ItemModType.BlockRating:
								ApplyRatingMod(CombatRating.Block, (int)enchant_amount, apply);
								Log.outDebug(LogFilter.Player, "+ {0} SHIELD_BLOCK", enchant_amount);

								break;
							case ItemModType.HitMeleeRating:
								ApplyRatingMod(CombatRating.HitMelee, (int)enchant_amount, apply);
								Log.outDebug(LogFilter.Player, "+ {0} MELEE_HIT", enchant_amount);

								break;
							case ItemModType.HitRangedRating:
								ApplyRatingMod(CombatRating.HitRanged, (int)enchant_amount, apply);
								Log.outDebug(LogFilter.Player, "+ {0} RANGED_HIT", enchant_amount);

								break;
							case ItemModType.HitSpellRating:
								ApplyRatingMod(CombatRating.HitSpell, (int)enchant_amount, apply);
								Log.outDebug(LogFilter.Player, "+ {0} SPELL_HIT", enchant_amount);

								break;
							case ItemModType.CritMeleeRating:
								ApplyRatingMod(CombatRating.CritMelee, (int)enchant_amount, apply);
								Log.outDebug(LogFilter.Player, "+ {0} MELEE_CRIT", enchant_amount);

								break;
							case ItemModType.CritRangedRating:
								ApplyRatingMod(CombatRating.CritRanged, (int)enchant_amount, apply);
								Log.outDebug(LogFilter.Player, "+ {0} RANGED_CRIT", enchant_amount);

								break;
							case ItemModType.CritSpellRating:
								ApplyRatingMod(CombatRating.CritSpell, (int)enchant_amount, apply);
								Log.outDebug(LogFilter.Player, "+ {0} SPELL_CRIT", enchant_amount);

								break;
							case ItemModType.HasteSpellRating:
								ApplyRatingMod(CombatRating.HasteSpell, (int)enchant_amount, apply);

								break;
							case ItemModType.HitRating:
								ApplyRatingMod(CombatRating.HitMelee, (int)enchant_amount, apply);
								ApplyRatingMod(CombatRating.HitRanged, (int)enchant_amount, apply);
								ApplyRatingMod(CombatRating.HitSpell, (int)enchant_amount, apply);
								Log.outDebug(LogFilter.Player, "+ {0} HIT", enchant_amount);

								break;
							case ItemModType.CritRating:
								ApplyRatingMod(CombatRating.CritMelee, (int)enchant_amount, apply);
								ApplyRatingMod(CombatRating.CritRanged, (int)enchant_amount, apply);
								ApplyRatingMod(CombatRating.CritSpell, (int)enchant_amount, apply);
								Log.outDebug(LogFilter.Player, "+ {0} CRITICAL", enchant_amount);

								break;
							case ItemModType.ResilienceRating:
								ApplyRatingMod(CombatRating.ResiliencePlayerDamage, (int)enchant_amount, apply);
								Log.outDebug(LogFilter.Player, "+ {0} RESILIENCE", enchant_amount);

								break;
							case ItemModType.HasteRating:
								ApplyRatingMod(CombatRating.HasteMelee, (int)enchant_amount, apply);
								ApplyRatingMod(CombatRating.HasteRanged, (int)enchant_amount, apply);
								ApplyRatingMod(CombatRating.HasteSpell, (int)enchant_amount, apply);
								Log.outDebug(LogFilter.Player, "+ {0} HASTE", enchant_amount);

								break;
							case ItemModType.ExpertiseRating:
								ApplyRatingMod(CombatRating.Expertise, (int)enchant_amount, apply);
								Log.outDebug(LogFilter.Player, "+ {0} EXPERTISE", enchant_amount);

								break;
							case ItemModType.AttackPower:
								HandleStatFlatModifier(UnitMods.AttackPower, UnitModifierFlatType.Total, enchant_amount, apply);
								HandleStatFlatModifier(UnitMods.AttackPowerRanged, UnitModifierFlatType.Total, enchant_amount, apply);
								Log.outDebug(LogFilter.Player, "+ {0} ATTACK_POWER", enchant_amount);

								break;
							case ItemModType.RangedAttackPower:
								HandleStatFlatModifier(UnitMods.AttackPowerRanged, UnitModifierFlatType.Total, enchant_amount, apply);
								Log.outDebug(LogFilter.Player, "+ {0} RANGED_ATTACK_POWER", enchant_amount);

								break;
							case ItemModType.ManaRegeneration:
								ApplyManaRegenBonus((int)enchant_amount, apply);
								Log.outDebug(LogFilter.Player, "+ {0} MANA_REGENERATION", enchant_amount);

								break;
							case ItemModType.ArmorPenetrationRating:
								ApplyRatingMod(CombatRating.ArmorPenetration, (int)enchant_amount, apply);
								Log.outDebug(LogFilter.Player, "+ {0} ARMOR PENETRATION", enchant_amount);

								break;
							case ItemModType.SpellPower:
								ApplySpellPowerBonus((int)enchant_amount, apply);
								Log.outDebug(LogFilter.Player, "+ {0} SPELL_POWER", enchant_amount);

								break;
							case ItemModType.HealthRegen:
								ApplyHealthRegenBonus((int)enchant_amount, apply);
								Log.outDebug(LogFilter.Player, "+ {0} HEALTH_REGENERATION", enchant_amount);

								break;
							case ItemModType.SpellPenetration:
								ApplySpellPenetrationBonus((int)enchant_amount, apply);
								Log.outDebug(LogFilter.Player, "+ {0} SPELL_PENETRATION", enchant_amount);

								break;
							case ItemModType.BlockValue:
								HandleBaseModFlatValue(BaseModGroup.ShieldBlockValue, enchant_amount, apply);
								Log.outDebug(LogFilter.Player, "+ {0} BLOCK_VALUE", enchant_amount);

								break;
							case ItemModType.MasteryRating:
								ApplyRatingMod(CombatRating.Mastery, (int)enchant_amount, apply);
								Log.outDebug(LogFilter.Player, "+ {0} MASTERY", enchant_amount);

								break;
							case ItemModType.Versatility:
								ApplyRatingMod(CombatRating.VersatilityDamageDone, (int)enchant_amount, apply);
								ApplyRatingMod(CombatRating.VersatilityHealingDone, (int)enchant_amount, apply);
								ApplyRatingMod(CombatRating.VersatilityDamageTaken, (int)enchant_amount, apply);
								Log.outDebug(LogFilter.Player, "+ {0} VERSATILITY", enchant_amount);

								break;
							default:
								break;
						}

						break;
					}
					case ItemEnchantmentType.Totem: // Shaman Rockbiter Weapon
					{
						var attackType = GetAttackBySlot(item.GetSlot(), item.GetTemplate().GetInventoryType());

						if (attackType != WeaponAttackType.Max)
							UpdateDamageDoneMods(attackType, apply ? -1 : (int)slot);

						break;
					}
					case ItemEnchantmentType.UseSpell:
						// processed in Player.CastItemUseSpell
						break;
					case ItemEnchantmentType.PrismaticSocket:
					case ItemEnchantmentType.ArtifactPowerBonusRankByType:
					case ItemEnchantmentType.ArtifactPowerBonusRankByID:
					case ItemEnchantmentType.BonusListID:
					case ItemEnchantmentType.BonusListCurve:
					case ItemEnchantmentType.ArtifactPowerBonusRankPicker:
						// nothing do..
						break;
					default:
						Log.outError(LogFilter.Player, "Unknown item enchantment (id = {0}) display type: {1}", enchant_id, enchant_display_type);

						break;
				}
			}

		// visualize enchantment at player and equipped items
		if (slot == EnchantmentSlot.Perm)
		{
			var visibleItem = Values.ModifyValue(PlayerData).ModifyValue(PlayerData.VisibleItems, item.GetSlot());
			SetUpdateFieldValue(visibleItem.ModifyValue(visibleItem.ItemVisual), item.GetVisibleItemVisual(this));
		}

		if (apply_dur)
		{
			if (apply)
			{
				// set duration
				var duration = item.GetEnchantmentDuration(slot);

				if (duration > 0)
					AddEnchantmentDuration(item, slot, duration);
			}
			else
			{
				// duration == 0 will remove EnchantDuration
				AddEnchantmentDuration(item, slot, 0);
			}
		}
	}

	public void ModifySkillBonus(SkillType skillid, int val, bool talent)
	{
		ModifySkillBonus((uint)skillid, val, talent);
	}

	public void ModifySkillBonus(uint skillid, int val, bool talent)
	{
		SkillInfo skillInfoField = ActivePlayerData.Skill;

		var skillStatusData = _skillStatus.LookupByKey(skillid);

		if (skillStatusData == null || skillStatusData.State == SkillState.Deleted || skillInfoField.SkillRank[skillStatusData.Pos] == 0)
			return;

		if (talent)
			SetSkillPermBonus(skillStatusData.Pos, (ushort)(skillInfoField.SkillPermBonus[skillStatusData.Pos] + val));
		else
			SetSkillTempBonus(skillStatusData.Pos, (ushort)(skillInfoField.SkillTempBonus[skillStatusData.Pos] + val));

		// Apply/Remove bonus to child skill lines
		var childSkillLines = Global.DB2Mgr.GetSkillLinesForParentSkill(skillid);

		if (childSkillLines != null)
			foreach (var childSkillLine in childSkillLines)
				ModifySkillBonus(childSkillLine.Id, val, talent);
	}

	public void StopCastingBindSight()
	{
		var target = GetViewpoint();

		if (target)
			if (target.IsTypeMask(TypeMask.Unit))
			{
				((Unit)target).RemoveAurasByType(AuraType.BindSight, GetGUID());
				((Unit)target).RemoveAurasByType(AuraType.ModPossess, GetGUID());
				((Unit)target).RemoveAurasByType(AuraType.ModPossessPet, GetGUID());
			}
	}

	public void RemoveArenaEnchantments(EnchantmentSlot slot)
	{
		// remove enchantments from equipped items first to clean up the m_enchantDuration list
		for (var i = 0; i < _enchantDurations.Count; ++i)
		{
			var enchantDuration = _enchantDurations[i];

			if (enchantDuration.Slot == slot)
			{
				if (enchantDuration.Item && enchantDuration.Item.GetEnchantmentId(slot) != 0)
				{
					// Poisons and DK runes are enchants which are allowed on arenas
					if (Global.SpellMgr.IsArenaAllowedEnchancment(enchantDuration.Item.GetEnchantmentId(slot)))
						continue;

					// remove from stats
					ApplyEnchantment(enchantDuration.Item, slot, false, false);
					// remove visual
					enchantDuration.Item.ClearEnchantment(slot);
				}

				// remove from update list
				_enchantDurations.Remove(enchantDuration);
			}
		}

		// remove enchants from inventory items
		// NOTE: no need to remove these from stats, since these aren't equipped
		// in inventory
		var inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();

		for (var i = InventorySlots.ItemStart; i < inventoryEnd; ++i)
		{
			var pItem = GetItemByPos(InventorySlots.Bag0, i);

			if (pItem && !Global.SpellMgr.IsArenaAllowedEnchancment(pItem.GetEnchantmentId(slot)))
				pItem.ClearEnchantment(slot);
		}

		// in inventory bags
		for (var i = InventorySlots.BagStart; i < InventorySlots.BagEnd; ++i)
		{
			var pBag = GetBagByPos(i);

			if (pBag)
				for (byte j = 0; j < pBag.GetBagSize(); j++)
				{
					var pItem = pBag.GetItemByPos(j);

					if (pItem && !Global.SpellMgr.IsArenaAllowedEnchancment(pItem.GetEnchantmentId(slot)))
						pItem.ClearEnchantment(slot);
				}
		}
	}

	public void UpdatePotionCooldown(Spell spell = null)
	{
		// no potion used i combat or still in combat
		if (_lastPotionId == 0 || IsInCombat())
			return;

		// Call not from spell cast, send cooldown event for item spells if no in combat
		if (!spell)
		{
			// spell/item pair let set proper cooldown (except not existed charged spell cooldown spellmods for potions)
			var proto = Global.ObjectMgr.GetItemTemplate(_lastPotionId);

			if (proto != null)
				for (byte idx = 0; idx < proto.Effects.Count; ++idx)
					if (proto.Effects[idx].SpellID != 0 && proto.Effects[idx].TriggerType == ItemSpelltriggerType.OnUse)
					{
						var spellInfo = Global.SpellMgr.GetSpellInfo((uint)proto.Effects[idx].SpellID, Difficulty.None);

						if (spellInfo != null)
							GetSpellHistory().SendCooldownEvent(spellInfo, _lastPotionId);
					}
		}
		// from spell cases (m_lastPotionId set in Spell.SendSpellCooldown)
		else
		{
			if (spell.IsIgnoringCooldowns())
				return;
			else
				GetSpellHistory().SendCooldownEvent(spell.SpellInfo, _lastPotionId, spell);
		}

		_lastPotionId = 0;
	}

	public bool CanUseMastery()
	{
		var chrSpec = CliDB.ChrSpecializationStorage.LookupByKey(GetPrimarySpecialization());

		if (chrSpec != null)
			return HasSpell(chrSpec.MasterySpellID[0]) || HasSpell(chrSpec.MasterySpellID[1]);

		return false;
	}

	public bool HasSkill(SkillType skill)
	{
		return HasSkill((uint)skill);
	}

	public bool HasSkill(uint skill)
	{
		if (skill == 0)
			return false;

		SkillInfo skillInfoField = ActivePlayerData.Skill;

		var skillStatusData = _skillStatus.LookupByKey(skill);

		return skillStatusData != null && skillStatusData.State != SkillState.Deleted && skillInfoField.SkillRank[skillStatusData.Pos] != 0;
	}

	public void SetSkill(SkillType skill, uint step, uint newVal, uint maxVal)
	{
		SetSkill((uint)skill, step, newVal, maxVal);
	}

	public void SetSkill(uint id, uint step, uint newVal, uint maxVal)
	{
		var skillEntry = CliDB.SkillLineStorage.LookupByKey(id);

		if (skillEntry == null)
		{
			Log.outError(LogFilter.Misc, $"Player.Spells.SetSkill: Skillid: {id} not found in SkillLineStorage for player {GetName()} ({GetGUID()})");

			return;
		}

		ushort currVal;
		var skillStatusData = _skillStatus.LookupByKey(id);
		SkillInfo skillInfoField = ActivePlayerData.Skill;

		void refreshSkillBonusAuras()
		{
			// Temporary bonuses
			foreach (var effect in GetAuraEffectsByType(AuraType.ModSkill))
				if (effect.MiscValue == id)
					effect.HandleEffect(this, AuraEffectHandleModes.Skill, true);

			foreach (var effect in GetAuraEffectsByType(AuraType.ModSkill2))
				if (effect.MiscValue == id)
					effect.HandleEffect(this, AuraEffectHandleModes.Skill, true);

			// Permanent bonuses
			foreach (var effect in GetAuraEffectsByType(AuraType.ModSkillTalent))
				if (effect.MiscValue == id)
					effect.HandleEffect(this, AuraEffectHandleModes.Skill, true);
		}

		// Handle already stored skills
		if (skillStatusData != null)
		{
			currVal = skillInfoField.SkillRank[skillStatusData.Pos];

			// Activate and update skill line
			if (newVal != 0)
			{
				// if skill value is going down, update enchantments before setting the new value
				if (newVal < currVal)
					UpdateSkillEnchantments(id, currVal, (ushort)newVal);

				// update step
				SetSkillStep(skillStatusData.Pos, (ushort)step);
				// update value
				SetSkillRank(skillStatusData.Pos, (ushort)newVal);
				SetSkillMaxRank(skillStatusData.Pos, (ushort)maxVal);

				LearnSkillRewardedSpells(id, newVal, GetRace());

				// if skill value is going up, update enchantments after setting the new value
				if (newVal > currVal)
				{
					UpdateSkillEnchantments(id, currVal, (ushort)newVal);

					if (id == (uint)SkillType.Riding)
						UpdateMountCapability();
				}

				UpdateCriteria(CriteriaType.SkillRaised, id);
				UpdateCriteria(CriteriaType.AchieveSkillStep, id);

				// update skill state
				if (skillStatusData.State == SkillState.Unchanged || skillStatusData.State == SkillState.Deleted)
				{
					if (currVal == 0) // activated skill, mark as new to save into database
					{
						skillStatusData.State = SkillState.New;

						// Set profession line
						var freeProfessionSlot = FindEmptyProfessionSlotFor(id);

						if (freeProfessionSlot != -1)
							SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ProfessionSkillLine, freeProfessionSlot), id);

						refreshSkillBonusAuras();
					}
					else // updated skill, mark as changed to save into database
					{
						skillStatusData.State = SkillState.Changed;
					}
				}
			}
			else if (currVal != 0 && newVal == 0) // Deactivate skill line
			{
				// Try to store profession tools and accessories into the bag
				// If we can't, we can't unlearn the profession
				var professionSlot = GetProfessionSlotFor(id);

				if (professionSlot != -1)
				{
					var professionSlotStart = (byte)(ProfessionSlots.Profession1Tool + professionSlot * ProfessionSlots.MaxCount);

					// Get all profession items equipped
					for (byte slotOffset = 0; slotOffset < ProfessionSlots.MaxCount; ++slotOffset)
					{
						var professionItem = GetItemByPos(InventorySlots.Bag0, (byte)(professionSlotStart + slotOffset));

						if (professionItem != null)
						{
							// Store item in bag
							List<ItemPosCount> professionItemDest = new();

							if (CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, professionItemDest, professionItem, false) != InventoryResult.Ok)
							{
								SendPacket(new DisplayGameError(GameError.InvFull));

								return;
							}

							RemoveItem(InventorySlots.Bag0, professionItem.GetSlot(), true);
							StoreItem(professionItemDest, professionItem, true);
						}
					}

					// Clear profession lines
					SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ProfessionSkillLine, professionSlot), 0u);
				}

				//remove enchantments needing this skill
				UpdateSkillEnchantments(id, currVal, 0);
				// clear skill fields
				SetSkillStep(skillStatusData.Pos, 0);
				SetSkillRank(skillStatusData.Pos, 0);
				SetSkillStartingRank(skillStatusData.Pos, 1);
				SetSkillMaxRank(skillStatusData.Pos, 0);
				SetSkillTempBonus(skillStatusData.Pos, 0);
				SetSkillPermBonus(skillStatusData.Pos, 0);

				// mark as deleted so the next save will delete the data from the database
				skillStatusData.State = SkillState.Deleted;


				// remove all spells that related to this skill
				var skillLineAbilities = Global.DB2Mgr.GetSkillLineAbilitiesBySkill(id);

				foreach (var skillLineAbility in skillLineAbilities)
					RemoveSpell(Global.SpellMgr.GetFirstSpellInChain(skillLineAbility.Spell));

				var childSkillLines = Global.DB2Mgr.GetSkillLinesForParentSkill(id);

				if (childSkillLines != null)
					foreach (var childSkillLine in childSkillLines)
						if (childSkillLine.ParentSkillLineID == id)
							SetSkill(childSkillLine.Id, 0, 0, 0);
			}
		}
		else
		{
			// We are about to learn a skill that has been added outside of normal circumstances (Game Master command, scripts etc.)
			byte skillSlot = 0;

			// Find a free skill slot
			for (var i = 0; i < SkillConst.MaxPlayerSkills; ++i)
				if (((SkillInfo)ActivePlayerData.Skill).SkillLineID[i] == 0)
				{
					skillSlot = (byte)i;

					break;
				}

			if (skillSlot == 0)
			{
				Log.outError(LogFilter.Misc, $"Tried to add skill {id} but player {GetName()} ({GetGUID()}) cannot have additional skills");

				return;
			}

			if (skillEntry.ParentSkillLineID != 0)
			{
				if (skillEntry.ParentTierIndex > 0)
				{
					var rcEntry = Global.DB2Mgr.GetSkillRaceClassInfo(skillEntry.ParentSkillLineID, GetRace(), GetClass());

					if (rcEntry != null)
					{
						var tier = Global.ObjectMgr.GetSkillTier(rcEntry.SkillTierID);

						if (tier != null)
						{
							var skillval = GetPureSkillValue((SkillType)skillEntry.ParentSkillLineID);
							SetSkill(skillEntry.ParentSkillLineID, (uint)skillEntry.ParentTierIndex, Math.Max(skillval, (ushort)1), tier.Value[skillEntry.ParentTierIndex - 1]);
						}
					}
				}
			}
			else
			{
				// also learn missing child skills at 0 value
				var childSkillLines = Global.DB2Mgr.GetSkillLinesForParentSkill(id);

				if (childSkillLines != null)
					foreach (var childSkillLine in childSkillLines)
						if (!HasSkill((SkillType)childSkillLine.Id))
							SetSkill(childSkillLine.Id, 0, 0, 0);

				var freeProfessionSlot = FindEmptyProfessionSlotFor(id);

				if (freeProfessionSlot != -1)
					SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ProfessionSkillLine, freeProfessionSlot), id);
			}

			if (skillStatusData == null)
				SetSkillLineId(skillSlot, (ushort)id);

			SetSkillStep(skillSlot, (ushort)step);
			SetSkillRank(skillSlot, (ushort)newVal);
			SetSkillStartingRank(skillSlot, 1);
			SetSkillMaxRank(skillSlot, (ushort)maxVal);

			// apply skill bonuses
			SetSkillTempBonus(skillSlot, 0);
			SetSkillPermBonus(skillSlot, 0);

			UpdateSkillEnchantments(id, 0, (ushort)newVal);

			_skillStatus.Add(id, new SkillStatusData(skillSlot, SkillState.New));

			if (newVal != 0)
			{
				refreshSkillBonusAuras();

				// Learn all spells for skill
				LearnSkillRewardedSpells(id, newVal, GetRace());
				UpdateCriteria(CriteriaType.SkillRaised, id);
				UpdateCriteria(CriteriaType.AchieveSkillStep, id);
			}
		}
	}

	public bool UpdateCraftSkill(SpellInfo spellInfo)
	{
		if (spellInfo.HasAttribute(SpellAttr1.NoSkillIncrease))
			return false;

		Log.outDebug(LogFilter.Player, "UpdateCraftSkill spellid {0}", spellInfo.Id);

		var bounds = Global.SpellMgr.GetSkillLineAbilityMapBounds(spellInfo.Id);

		foreach (var _spell_idx in bounds)
			if (_spell_idx.SkillupSkillLineID != 0)
			{
				uint SkillValue = GetPureSkillValue((SkillType)_spell_idx.SkillupSkillLineID);

				// Alchemy Discoveries here
				if (spellInfo.Mechanic == Mechanics.Discovery)
				{
					var discoveredSpell = SkillDiscovery.GetSkillDiscoverySpell(_spell_idx.SkillupSkillLineID, spellInfo.Id, this);

					if (discoveredSpell != 0)
						LearnSpell(discoveredSpell, false);
				}

				var craft_skill_gain = _spell_idx.NumSkillUps * WorldConfig.GetUIntValue(WorldCfg.SkillGainCrafting);

				return UpdateSkillPro(_spell_idx.SkillupSkillLineID,
									SkillGainChance(SkillValue,
													_spell_idx.TrivialSkillLineRankHigh,
													(uint)(_spell_idx.TrivialSkillLineRankHigh + _spell_idx.TrivialSkillLineRankLow) / 2,
													_spell_idx.TrivialSkillLineRankLow),
									craft_skill_gain);
			}

		return false;
	}

	public bool UpdateGatherSkill(uint SkillId, uint SkillValue, uint RedLevel, uint Multiplicator = 1, WorldObject obj = null)
	{
		return UpdateGatherSkill((SkillType)SkillId, SkillValue, RedLevel, Multiplicator, obj);
	}

	public bool UpdateGatherSkill(SkillType SkillId, uint SkillValue, uint RedLevel, uint Multiplicator = 1, WorldObject obj = null)
	{
		Log.outDebug(LogFilter.Player, "UpdateGatherSkill(SkillId {0} SkillLevel {1} RedLevel {2})", SkillId, SkillValue, RedLevel);

		var gathering_skill_gain = WorldConfig.GetUIntValue(WorldCfg.SkillGainGathering);

		var grayLevel = RedLevel + 100;
		var greenLevel = RedLevel + 50;
		var yellowLevel = RedLevel + 25;

		var go = obj?.ToGameObject();

		if (go != null)
		{
			if (go.GetGoInfo().GetTrivialSkillLow() != 0)
				yellowLevel = go.GetGoInfo().GetTrivialSkillLow();

			if (go.GetGoInfo().GetTrivialSkillHigh() != 0)
				grayLevel = go.GetGoInfo().GetTrivialSkillHigh();

			greenLevel = (yellowLevel + grayLevel) / 2;
		}

		// For skinning and Mining chance decrease with level. 1-74 - no decrease, 75-149 - 2 times, 225-299 - 8 times
		switch (SkillId)
		{
			case SkillType.Herbalism:
			case SkillType.ClassicHerbalism:
			case SkillType.OutlandHerbalism:
			case SkillType.NorthrendHerbalism:
			case SkillType.CataclysmHerbalism:
			case SkillType.PandariaHerbalism:
			case SkillType.DraenorHerbalism:
			case SkillType.LegionHerbalism:
			case SkillType.KulTiranHerbalism:
			case SkillType.Jewelcrafting:
			case SkillType.Inscription:
			case SkillType.DragonIslesHerbalism:
			case SkillType.DragonIslesInscription:
			case SkillType.DragonIslesJewelcrafting:
				return UpdateSkillPro(SkillId, SkillGainChance(SkillValue, grayLevel, greenLevel, yellowLevel) * (int)Multiplicator, gathering_skill_gain);
			case SkillType.Skinning:
			case SkillType.ClassicSkinning:
			case SkillType.OutlandSkinning:
			case SkillType.NorthrendSkinning:
			case SkillType.CataclysmSkinning:
			case SkillType.PandariaSkinning:
			case SkillType.DraenorSkinning:
			case SkillType.LegionSkinning:
			case SkillType.KulTiranSkinning:
			case SkillType.DragonIslesSkinning:
				if (WorldConfig.GetIntValue(WorldCfg.SkillChanceSkinningSteps) == 0)
					return UpdateSkillPro(SkillId, SkillGainChance(SkillValue, grayLevel, greenLevel, yellowLevel) * (int)Multiplicator, gathering_skill_gain);
				else
					return UpdateSkillPro(SkillId, (int)(SkillGainChance(SkillValue, grayLevel, greenLevel, yellowLevel) * Multiplicator) >> (int)(SkillValue / WorldConfig.GetIntValue(WorldCfg.SkillChanceSkinningSteps)), gathering_skill_gain);
			case SkillType.Mining:
			case SkillType.ClassicMining:
			case SkillType.OutlandMining:
			case SkillType.NorthrendMining:
			case SkillType.CataclysmMining:
			case SkillType.PandariaMining:
			case SkillType.DraenorMining:
			case SkillType.LegionMining:
			case SkillType.KulTiranMining:
			case SkillType.DragonIslesMining:
				if (WorldConfig.GetIntValue(WorldCfg.SkillChanceMiningSteps) == 0)
					return UpdateSkillPro(SkillId, SkillGainChance(SkillValue, grayLevel, greenLevel, yellowLevel) * (int)Multiplicator, gathering_skill_gain);
				else
					return UpdateSkillPro(SkillId, (int)(SkillGainChance(SkillValue, grayLevel, greenLevel, yellowLevel) * Multiplicator) >> (int)(SkillValue / WorldConfig.GetIntValue(WorldCfg.SkillChanceMiningSteps)), gathering_skill_gain);
		}

		return false;
	}

	public bool UpdateFishingSkill()
	{
		Log.outDebug(LogFilter.Player, "UpdateFishingSkill");

		uint SkillValue = GetPureSkillValue(SkillType.ClassicFishing);

		if (SkillValue >= GetMaxSkillValue(SkillType.ClassicFishing))
			return false;

		var stepsNeededToLevelUp = GetFishingStepsNeededToLevelUp(SkillValue);
		++_fishingSteps;

		if (_fishingSteps >= stepsNeededToLevelUp)
		{
			_fishingSteps = 0;

			var gathering_skill_gain = WorldConfig.GetUIntValue(WorldCfg.SkillGainGathering);

			return UpdateSkillPro(SkillType.ClassicFishing, 100 * 10, gathering_skill_gain);
		}

		return false;
	}

	public void CastItemUseSpell(Item item, SpellCastTargets targets, ObjectGuid castCount, uint[] misc)
	{
		if (!item.GetTemplate().HasFlag(ItemFlags.Legacy))
			// item spells casted at use
			foreach (var effectData in item.GetEffects())
			{
				// wrong triggering type
				if (effectData.TriggerType != ItemSpelltriggerType.OnUse)
					continue;

				var spellInfo = Global.SpellMgr.GetSpellInfo((uint)effectData.SpellID, Difficulty.None);

				if (spellInfo == null)
				{
					Log.outError(LogFilter.Player, "Player.CastItemUseSpell: Item (Entry: {0}) in have wrong spell id {1}, ignoring", item.GetEntry(), effectData.SpellID);

					continue;
				}

				Spell spell = new(this, spellInfo, TriggerCastFlags.None);

				SpellPrepare spellPrepare = new();
				spellPrepare.ClientCastID = castCount;
				spellPrepare.ServerCastID = spell.CastId;
				SendPacket(spellPrepare);

				spell.FromClient = true;
				spell.CastItem = item;
				spell.SpellMisc.Data0 = misc[0];
				spell.SpellMisc.Data1 = misc[1];
				spell.Prepare(targets);

				return;
			}

		// Item enchantments spells casted at use
		for (EnchantmentSlot e_slot = 0; e_slot < EnchantmentSlot.Max; ++e_slot)
		{
			var enchant_id = item.GetEnchantmentId(e_slot);
			var pEnchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchant_id);

			if (pEnchant == null)
				continue;

			for (byte s = 0; s < ItemConst.MaxItemEnchantmentEffects; ++s)
			{
				if (pEnchant.Effect[s] != ItemEnchantmentType.UseSpell)
					continue;

				var spellInfo = Global.SpellMgr.GetSpellInfo(pEnchant.EffectArg[s], Difficulty.None);

				if (spellInfo == null)
				{
					Log.outError(LogFilter.Player, "Player.CastItemUseSpell Enchant {0}, cast unknown spell {1}", enchant_id, pEnchant.EffectArg[s]);

					continue;
				}

				Spell spell = new(this, spellInfo, TriggerCastFlags.None);

				SpellPrepare spellPrepare = new();
				spellPrepare.ClientCastID = castCount;
				spellPrepare.ServerCastID = spell.CastId;
				SendPacket(spellPrepare);

				spell.FromClient = true;
				spell.CastItem = item;
				spell.SpellMisc.Data0 = misc[0];
				spell.SpellMisc.Data1 = misc[1];
				spell.Prepare(targets);

				return;
			}
		}
	}

	public uint GetLastPotionId()
	{
		return _lastPotionId;
	}

	public void SetLastPotionId(uint item_id)
	{
		_lastPotionId = item_id;
	}

	public void LearnSkillRewardedSpells(uint skillId, uint skillValue, Race race)
	{
		var raceMask = SharedConst.GetMaskForRace(race);
		var classMask = GetClassMask();

		var skillLineAbilities = Global.DB2Mgr.GetSkillLineAbilitiesBySkill(skillId);

		foreach (var ability in skillLineAbilities)
		{
			if (ability.SkillLine != skillId)
				continue;

			var spellInfo = Global.SpellMgr.GetSpellInfo(ability.Spell, Difficulty.None);

			if (spellInfo == null)
				continue;

			switch (ability.AcquireMethod)
			{
				case AbilityLearnType.OnSkillValue:
				case AbilityLearnType.OnSkillLearn:
					break;
				case AbilityLearnType.RewardedFromQuest:
					if (!ability.Flags.HasAnyFlag(SkillLineAbilityFlags.CanFallbackToLearnedOnSkillLearn) ||
						!spellInfo.MeetsFutureSpellPlayerCondition(this))
						continue;

					break;
				default:
					continue;
			}

			// AcquireMethod == 2 && NumSkillUps == 1 -. automatically learn riding skill spell, else we skip it (client shows riding in spellbook as trainable).
			if (skillId == (uint)SkillType.Riding && (ability.AcquireMethod != AbilityLearnType.OnSkillLearn || ability.NumSkillUps != 1))
				continue;

			// Check race if set
			if (ability.RaceMask != 0 && !Convert.ToBoolean(ability.RaceMask & raceMask))
				continue;

			// Check class if set
			if (ability.ClassMask != 0 && !Convert.ToBoolean(ability.ClassMask & classMask))
				continue;

			// check level, skip class spells if not high enough
			if (GetLevel() < spellInfo.SpellLevel)
				continue;

			// need unlearn spell
			if (skillValue < ability.MinSkillLineRank && ability.AcquireMethod == AbilityLearnType.OnSkillValue)
				RemoveSpell(ability.Spell);
			// need learn
			else if (!IsInWorld)
				AddSpell(ability.Spell, true, true, true, false, false, ability.SkillLine);
			else
				LearnSpell(ability.Spell, true, ability.SkillLine);
		}
	}

	public int GetProfessionSlotFor(uint skillId)
	{
		var skillEntry = CliDB.SkillLineStorage.LookupByKey(skillId);

		if (skillEntry == null)
			return -1;

		if (skillEntry.ParentSkillLineID == 0 || skillEntry.CategoryID != SkillCategory.Profession)
			return -1;

		var slot = 0;

		foreach (var bit in ActivePlayerData.ProfessionSkillLine)
		{
			if (bit == skillId)
				return slot;

			slot++;
		}

		return -1;
	}

	public bool HasItemFitToSpellRequirements(SpellInfo spellInfo, Item ignoreItem = null)
	{
		if (spellInfo.EquippedItemClass < 0)
			return true;

		// scan other equipped items for same requirements (mostly 2 daggers/etc)
		// for optimize check 2 used cases only
		switch (spellInfo.EquippedItemClass)
		{
			case ItemClass.Weapon:
			{
				var item = GetUseableItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);

				if (item)
					if (item != ignoreItem && item.IsFitToSpellRequirements(spellInfo))
						return true;

				item = GetUseableItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);

				if (item)
					if (item != ignoreItem && item.IsFitToSpellRequirements(spellInfo))
						return true;

				break;
			}
			case ItemClass.Armor:
			{
				if (!spellInfo.HasAttribute(SpellAttr8.ArmorSpecialization))
				{
					// most used check: shield only
					if ((spellInfo.EquippedItemSubClassMask & (1 << (int)ItemSubClassArmor.Shield)) != 0)
					{
						var item = GetUseableItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);

						if (item != null)
							if (item != ignoreItem && item.IsFitToSpellRequirements(spellInfo))
								return true;

						// special check to filter things like Shield Wall, the aura is not permanent and must stay even without required item
						if (!spellInfo.IsPassive)
							foreach (var spellEffectInfo in spellInfo.Effects)
								if (spellEffectInfo.IsAura())
									return true;
					}

					// tabard not have dependent spells
					for (var i = EquipmentSlot.Start; i < EquipmentSlot.MainHand; ++i)
					{
						var item = GetUseableItemByPos(InventorySlots.Bag0, i);

						if (item)
							if (item != ignoreItem && item.IsFitToSpellRequirements(spellInfo))
								return true;
					}
				}
				else
				{
					// requires item equipped in all armor slots
					foreach (var i in new[]
							{
								EquipmentSlot.Head, EquipmentSlot.Shoulders, EquipmentSlot.Chest, EquipmentSlot.Waist, EquipmentSlot.Legs, EquipmentSlot.Feet, EquipmentSlot.Wrist, EquipmentSlot.Hands
							})
					{
						var item = GetUseableItemByPos(InventorySlots.Bag0, i);

						if (!item || item == ignoreItem || !item.IsFitToSpellRequirements(spellInfo))
							return false;
					}

					return true;
				}

				break;
			}
			default:
				Log.outError(LogFilter.Player, "HasItemFitToSpellRequirements: Not handled spell requirement for item class {0}", spellInfo.EquippedItemClass);

				break;
		}

		return false;
	}

	public Dictionary<uint, PlayerSpell> GetSpellMap()
	{
		return _spells;
	}

	public override SpellSchoolMask GetMeleeDamageSchoolMask(WeaponAttackType attackType = WeaponAttackType.BaseAttack)
	{
		var weapon = GetWeaponForAttack(attackType, true);

		if (weapon != null)
			return (SpellSchoolMask)(1 << (int)weapon.GetTemplate().GetDamageType());

		return SpellSchoolMask.Normal;
	}

	public void UpdateAllWeaponDependentCritAuras()
	{
		for (var attackType = WeaponAttackType.BaseAttack; attackType < WeaponAttackType.Max; ++attackType)
			UpdateWeaponDependentCritAuras(attackType);
	}

	public void UpdateWeaponDependentAuras(WeaponAttackType attackType)
	{
		UpdateWeaponDependentCritAuras(attackType);
		UpdateDamageDoneMods(attackType);
		UpdateDamagePctDoneMods(attackType);
	}

	public void ApplyItemDependentAuras(Item item, bool apply)
	{
		if (apply)
		{
			var spells = GetSpellMap();

			foreach (var pair in spells)
			{
				if (pair.Value.State == PlayerSpellState.Removed || pair.Value.Disabled)
					continue;

				var spellInfo = Global.SpellMgr.GetSpellInfo(pair.Key, Difficulty.None);

				if (spellInfo == null || !spellInfo.IsPassive || spellInfo.EquippedItemClass < 0)
					continue;

				if (!HasAura(pair.Key) && HasItemFitToSpellRequirements(spellInfo))
					AddAura(pair.Key, this); // no SMSG_SPELL_GO in sniff found
			}
		}
		else
		{
			RemoveItemDependentAurasAndCasts(item);
		}
	}

	public override bool CheckAttackFitToAuraRequirement(WeaponAttackType attackType, AuraEffect aurEff)
	{
		var spellInfo = aurEff.SpellInfo;

		if (spellInfo.EquippedItemClass == ItemClass.None)
			return true;

		var item = GetWeaponForAttack(attackType, true);

		if (item == null || !item.IsFitToSpellRequirements(spellInfo))
			return false;

		return true;
	}

	public void AddTemporarySpell(uint spellId)
	{
		var spell = _spells.LookupByKey(spellId);

		// spell already added - do not do anything
		if (spell != null)
			return;

		PlayerSpell newspell = new();
		newspell.State = PlayerSpellState.Temporary;
		newspell.Active = true;
		newspell.Dependent = false;
		newspell.Disabled = false;

		_spells[spellId] = newspell;
	}

	public void RemoveTemporarySpell(uint spellId)
	{
		var spell = _spells.LookupByKey(spellId);

		// spell already not in list - do not do anything
		if (spell == null)
			return;

		// spell has other state than temporary - do not change it
		if (spell.State != PlayerSpellState.Temporary)
			return;

		_spells.Remove(spellId);
	}

	public void UpdateZoneDependentAuras(uint newZone)
	{
		// Some spells applied at enter into zone (with subzones), aura removed in UpdateAreaDependentAuras that called always at zone.area update
		var saBounds = Global.SpellMgr.GetSpellAreaForAreaMapBounds(newZone);

		foreach (var spell in saBounds)
			if (spell.Flags.HasAnyFlag(SpellAreaFlag.AutoCast) && spell.IsFitToRequirements(this, newZone, 0))
				if (!HasAura(spell.SpellId))
					CastSpell(this, spell.SpellId, true);
	}

	public void UpdateAreaDependentAuras(uint newArea)
	{
		// remove auras from spells with area limitations
		// use m_zoneUpdateId for speed: UpdateArea called from UpdateZone or instead UpdateZone in both cases m_zoneUpdateId up-to-date
		GetOwnedAurasList()
			.CallOnMatch((aura) => aura.SpellInfo.CheckLocation(Location.MapId, _zoneUpdateId, newArea, this) != SpellCastResult.SpellCastOk,
						(pair) => RemoveOwnedAura(pair.SpellInfo.Id, pair));

		// some auras applied at subzone enter
		var saBounds = Global.SpellMgr.GetSpellAreaForAreaMapBounds(newArea);

		foreach (var spell in saBounds)
			if (spell.Flags.HasAnyFlag(SpellAreaFlag.AutoCast) && spell.IsFitToRequirements(this, _zoneUpdateId, newArea))
				if (!HasAura(spell.SpellId))
					CastSpell(this, spell.SpellId, true);
	}

	public void ApplyModToSpell(SpellModifier mod, Spell spell)
	{
		if (spell == null)
			return;

		// don't do anything with no charges
		if (mod.OwnerAura.IsUsingCharges && mod.OwnerAura.Charges == 0)
			return;

		// register inside spell, proc system uses this to drop charges
		spell.AppliedMods.Add(mod.OwnerAura);
	}

	public void LearnCustomSpells()
	{
		//if (!WorldConfig.GetBoolValue(WorldCfg.StartAllSpells)) // this is not all spells, just custom ones.
		//    return;

		// learn default race/class spells
		var info = Global.ObjectMgr.GetPlayerInfo(GetRace(), GetClass());

		foreach (var tspell in info.CustomSpells)
		{
			Log.outDebug(LogFilter.Player, "PLAYER (Class: {0} Race: {1}): Adding initial spell, id = {2}", GetClass(), GetRace(), tspell);

			if (!IsInWorld) // will send in INITIAL_SPELLS in list anyway at map add
				AddSpell(tspell, true, true, true, false);
			else // but send in normal spell in game learn case
				LearnSpell(tspell, true);
		}
	}

	public void LearnDefaultSkills()
	{
		// learn default race/class skills
		var info = Global.ObjectMgr.GetPlayerInfo(GetRace(), GetClass());

		foreach (var rcInfo in info.Skills)
		{
			if (HasSkill((SkillType)rcInfo.SkillID))
				continue;

			if (rcInfo.MinLevel > GetLevel())
				continue;

			LearnDefaultSkill(rcInfo);
		}
	}

	public void LearnDefaultSkill(SkillRaceClassInfoRecord rcInfo)
	{
		var skillId = (SkillType)rcInfo.SkillID;

		switch (Global.SpellMgr.GetSkillRangeType(rcInfo))
		{
			case SkillRangeType.Language:
				SetSkill(skillId, 0, 300, 300);

				break;
			case SkillRangeType.Level:
			{
				ushort skillValue = 1;
				var maxValue = GetMaxSkillValueForLevel();

				if (rcInfo.Flags.HasAnyFlag(SkillRaceClassInfoFlags.AlwaysMaxValue))
					skillValue = maxValue;
				else if (GetClass() == Class.Deathknight)
					skillValue = (ushort)Math.Min(Math.Max(1, (GetLevel() - 1) * 5), maxValue);

				SetSkill(skillId, 0, skillValue, maxValue);

				break;
			}
			case SkillRangeType.Mono:
				SetSkill(skillId, 0, 1, 1);

				break;
			case SkillRangeType.Rank:
			{
				var tier = Global.ObjectMgr.GetSkillTier(rcInfo.SkillTierID);
				var maxValue = (ushort)tier.Value[0];
				ushort skillValue = 1;

				if (rcInfo.Flags.HasAnyFlag(SkillRaceClassInfoFlags.AlwaysMaxValue))
					skillValue = maxValue;
				else if (GetClass() == Class.Deathknight)
					skillValue = (ushort)Math.Min(Math.Max(1, (GetLevel() - 1) * 5), maxValue);

				SetSkill(skillId, 1, skillValue, maxValue);

				break;
			}
			default:
				break;
		}
	}

	public void LearnSpell<T>(T spellId, bool dependent, uint fromSkill = 0, bool suppressMessaging = false, int? traitDefinitionId = null) where T : struct, Enum
	{
		LearnSpell(Convert.ToUInt32(spellId), dependent, fromSkill, suppressMessaging, traitDefinitionId);
	}

	public void LearnSpell(uint spellId, bool dependent, uint fromSkill = 0, bool suppressMessaging = false, int? traitDefinitionId = null)
	{
		var spell = _spells.LookupByKey(spellId);

		var disabled = (spell != null) && spell.Disabled;
		var active = !disabled || spell.Active;
		var favorite = spell != null ? spell.Favorite : false;

		var learning = AddSpell(spellId, active, true, dependent, false, false, fromSkill, favorite, traitDefinitionId);

		// prevent duplicated entires in spell book, also not send if not in world (loading)
		if (learning && IsInWorld)
		{
			LearnedSpells learnedSpells = new();
			LearnedSpellInfo learnedSpellInfo = new();
			learnedSpellInfo.SpellID = spellId;
			learnedSpellInfo.IsFavorite = favorite;
			learnedSpellInfo.TraitDefinitionID = traitDefinitionId;
			learnedSpells.SuppressMessaging = suppressMessaging;
			learnedSpells.ClientLearnedSpellData.Add(learnedSpellInfo);
			SendPacket(learnedSpells);
		}

		// learn all disabled higher ranks and required spells (recursive)
		if (disabled)
		{
			var nextSpell = Global.SpellMgr.GetNextSpellInChain(spellId);

			if (nextSpell != 0)
			{
				var _spell = _spells.LookupByKey(nextSpell);

				if (spellId != 0 && _spell.Disabled)
					LearnSpell(nextSpell, false, fromSkill);
			}

			var spellsRequiringSpell = Global.SpellMgr.GetSpellsRequiringSpellBounds(spellId);

			foreach (var id in spellsRequiringSpell)
			{
				var spell1 = _spells.LookupByKey(id);

				if (spell1 != null && spell1.Disabled)
					LearnSpell(id, false, fromSkill);
			}
		}
		else
		{
			UpdateQuestObjectiveProgress(QuestObjectiveType.LearnSpell, (int)spellId, 1);
		}
	}

	public void RemoveSpell<T>(T spellId, bool disabled = false, bool learnLowRank = true, bool suppressMessaging = false) where T : struct, Enum
	{
		RemoveSpell(Convert.ToUInt32(spellId), disabled, learnLowRank, suppressMessaging);
	}

	public void RemoveSpell(uint spellId, bool disabled = false, bool learnLowRank = true, bool suppressMessaging = false)
	{
		var pSpell = _spells.LookupByKey(spellId);

		if (pSpell == null)
			return;

		if (pSpell.State == PlayerSpellState.Removed || (disabled && pSpell.Disabled) || pSpell.State == PlayerSpellState.Temporary)
			return;

		// unlearn non talent higher ranks (recursive)
		var nextSpell = Global.SpellMgr.GetNextSpellInChain(spellId);

		if (nextSpell != 0)
		{
			var spellInfo1 = Global.SpellMgr.GetSpellInfo(nextSpell, Difficulty.None);

			if (HasSpell(nextSpell) && !spellInfo1.HasAttribute(SpellCustomAttributes.IsTalent))
				RemoveSpell(nextSpell, disabled, false);
		}

		//unlearn spells dependent from recently removed spells
		var spellsRequiringSpell = Global.SpellMgr.GetSpellsRequiringSpellBounds(spellId);

		foreach (var id in spellsRequiringSpell)
			RemoveSpell(id, disabled);

		// re-search, it can be corrupted in prev loop
		pSpell = _spells.LookupByKey(spellId);

		if (pSpell == null)
			return; // already unleared

		var cur_active = pSpell.Active;
		var cur_dependent = pSpell.Dependent;

		if (disabled)
		{
			pSpell.Disabled = disabled;

			if (pSpell.State != PlayerSpellState.New)
				pSpell.State = PlayerSpellState.Changed;
		}
		else
		{
			if (pSpell.State == PlayerSpellState.New)
				_spells.Remove(spellId);
			else
				pSpell.State = PlayerSpellState.Removed;
		}

		RemoveOwnedAura(spellId, GetGUID());

		// remove pet auras
		foreach (var petAur in Global.SpellMgr.GetPetAuras(spellId)?.Values)
			RemovePetAura(petAur);

		// update free primary prof.points (if not overflow setting, can be in case GM use before .learn prof. learning)
		var spellInfo = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None);

		if (spellInfo != null && spellInfo.IsPrimaryProfessionFirstRank)
		{
			var freeProfs = GetFreePrimaryProfessionPoints() + 1;

			if (freeProfs <= WorldConfig.GetIntValue(WorldCfg.MaxPrimaryTradeSkill))
				SetFreePrimaryProfessions(freeProfs);
		}

		// remove dependent skill
		var spellLearnSkill = Global.SpellMgr.GetSpellLearnSkill(spellId);

		if (spellLearnSkill != null)
		{
			var prev_spell = Global.SpellMgr.GetPrevSpellInChain(spellId);

			if (prev_spell == 0) // first rank, remove skill
			{
				SetSkill(spellLearnSkill.Skill, 0, 0, 0);
			}
			else
			{
				// search prev. skill setting by spell ranks chain
				var prevSkill = Global.SpellMgr.GetSpellLearnSkill(prev_spell);

				while (prevSkill == null && prev_spell != 0)
				{
					prev_spell = Global.SpellMgr.GetPrevSpellInChain(prev_spell);
					prevSkill = Global.SpellMgr.GetSpellLearnSkill(Global.SpellMgr.GetFirstSpellInChain(prev_spell));
				}

				if (prevSkill == null) // not found prev skill setting, remove skill
				{
					SetSkill(spellLearnSkill.Skill, 0, 0, 0);
				}
				else // set to prev. skill setting values
				{
					uint skill_value = GetPureSkillValue(prevSkill.Skill);
					uint skill_max_value = GetPureMaxSkillValue(prevSkill.Skill);

					if (skill_value > prevSkill.Value)
						skill_value = prevSkill.Value;

					uint new_skill_max_value = prevSkill.Maxvalue == 0 ? GetMaxSkillValueForLevel() : prevSkill.Maxvalue;

					if (skill_max_value > new_skill_max_value)
						skill_max_value = new_skill_max_value;

					SetSkill(prevSkill.Skill, prevSkill.Step, skill_value, skill_max_value);
				}
			}
		}

		// remove dependent spells
		var spell_bounds = Global.SpellMgr.GetSpellLearnSpellMapBounds(spellId);

		foreach (var spellNode in spell_bounds)
		{
			RemoveSpell(spellNode.Spell, disabled);

			if (spellNode.OverridesSpell != 0)
				RemoveOverrideSpell(spellNode.OverridesSpell, spellNode.Spell);
		}

		// activate lesser rank in spellbook/action bar, and cast it if need
		var prev_activate = false;

		var prev_id = Global.SpellMgr.GetPrevSpellInChain(spellId);

		if (prev_id != 0)
			// if ranked non-stackable spell: need activate lesser rank and update dendence state
			// No need to check for spellInfo != NULL here because if cur_active is true, then that means that the spell was already in m_spells, and only valid spells can be pushed there.
			if (cur_active && spellInfo.IsRanked)
			{
				// need manually update dependence state (learn spell ignore like attempts)
				var prevSpell = _spells.LookupByKey(prev_id);

				if (prevSpell != null)
				{
					if (prevSpell.Dependent != cur_dependent)
					{
						prevSpell.Dependent = cur_dependent;

						if (prevSpell.State != PlayerSpellState.New)
							prevSpell.State = PlayerSpellState.Changed;
					}

					// now re-learn if need re-activate
					if (!prevSpell.Active && learnLowRank)
						if (AddSpell(prev_id, true, false, prevSpell.Dependent, prevSpell.Disabled))
						{
							// downgrade spell ranks in spellbook and action bar
							SendSupercededSpell(spellId, prev_id);
							prev_activate = true;
						}
				}
			}

		_overrideSpells.Remove(spellId);

		if (_canTitanGrip)
			if (spellInfo != null && spellInfo.IsPassive && spellInfo.HasEffect(SpellEffectName.TitanGrip))
			{
				RemoveAura(_titanGripPenaltySpellId);
				SetCanTitanGrip(false);
			}

		if (CanDualWield())
			if (spellInfo != null && spellInfo.IsPassive && spellInfo.HasEffect(SpellEffectName.DualWield))
				SetCanDualWield(false);

		if (WorldConfig.GetBoolValue(WorldCfg.OffhandCheckAtSpellUnlearn))
			AutoUnequipOffhandIfNeed();

		// remove from spell book if not replaced by lesser rank
		if (!prev_activate)
		{
			UnlearnedSpells unlearnedSpells = new();
			unlearnedSpells.SpellID.Add(spellId);
			unlearnedSpells.SuppressMessaging = suppressMessaging;
			SendPacket(unlearnedSpells);
		}
	}

	public void SetSpellFavorite(uint spellId, bool favorite)
	{
		var spell = _spells.LookupByKey(spellId);

		if (spell == null)
			return;

		spell.Favorite = favorite;

		if (spell.State == PlayerSpellState.Unchanged)
			spell.State = PlayerSpellState.Changed;
	}

	public void AddStoredAuraTeleportLocation(uint spellId)
	{
		StoredAuraTeleportLocation storedLocation = new();
		storedLocation.Loc = new WorldLocation(Location);
		storedLocation.CurrentState = StoredAuraTeleportLocation.State.Changed;

		_storedAuraTeleportLocations[spellId] = storedLocation;
	}

	public void RemoveStoredAuraTeleportLocation(uint spellId)
	{
		var storedLocation = _storedAuraTeleportLocations.LookupByKey(spellId);

		if (storedLocation != null)
			storedLocation.CurrentState = StoredAuraTeleportLocation.State.Deleted;
	}

	public WorldLocation GetStoredAuraTeleportLocation(uint spellId)
	{
		var auraLocation = _storedAuraTeleportLocations.LookupByKey(spellId);

		if (auraLocation != null)
			return auraLocation.Loc;

		return null;
	}

	public override bool HasSpell(uint spellId)
	{
		var spell = _spells.LookupByKey(spellId);

		if (spell != null)
			return spell.State != PlayerSpellState.Removed && !spell.Disabled;

		return false;
	}

	public bool HasActiveSpell(uint spellId)
	{
		var spell = _spells.LookupByKey(spellId);

		if (spell != null)
			return spell.State != PlayerSpellState.Removed && spell.Active && !spell.Disabled;

		return false;
	}

	public void AddSpellMod(SpellModifier mod, bool apply)
	{
		Log.outDebug(LogFilter.Spells, "Player.AddSpellMod {0}", mod.SpellId);

		// First, manipulate our spellmodifier container
		if (apply)
			_spellModifiers[(int)mod.Op][(int)mod.Type].Add(mod);
		else
			_spellModifiers[(int)mod.Op][(int)mod.Type].Remove(mod);

		// Now, send spellmodifier packet
		switch (mod.Type)
		{
			case SpellModType.Flat:
			case SpellModType.Pct:
				if (!IsLoading())
				{
					var opcode = (mod.Type == SpellModType.Flat ? ServerOpcodes.SetFlatSpellModifier : ServerOpcodes.SetPctSpellModifier);
					SetSpellModifier packet = new(opcode);

					// @todo Implement sending of bulk modifiers instead of single
					SpellModifierInfo spellMod = new();

					spellMod.ModIndex = (byte)mod.Op;

					for (var eff = 0; eff < 128; ++eff)
					{
						FlagArray128 mask = new();
						mask[eff / 32] = 1u << (eff % 32);

						if ((mod as SpellModifierByClassMask).Mask & mask)
						{
							SpellModifierData modData = new();

							if (mod.Type == SpellModType.Flat)
							{
								modData.ModifierValue = 0.0f;

								foreach (SpellModifierByClassMask spell in _spellModifiers[(int)mod.Op][(int)SpellModType.Flat])
									if (spell.Mask & mask)
										modData.ModifierValue += spell.Value;
							}
							else
							{
								modData.ModifierValue = 1.0f;

								foreach (SpellModifierByClassMask spell in _spellModifiers[(int)mod.Op][(int)SpellModType.Pct])
									if (spell.Mask & mask)
										modData.ModifierValue *= 1.0f + MathFunctions.CalculatePct(1.0f, spell.Value);
							}

							modData.ClassIndex = (byte)eff;

							spellMod.ModifierData.Add(modData);
						}
					}

					packet.Modifiers.Add(spellMod);

					SendPacket(packet);
				}

				break;
			case SpellModType.LabelFlat:
				if (apply)
				{
					AddDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.SpellFlatModByLabel), (mod as SpellFlatModifierByLabel).Value);
				}
				else
				{
					var firstIndex = ActivePlayerData.SpellFlatModByLabel.FindIndex((mod as SpellFlatModifierByLabel).Value);

					if (firstIndex >= 0)
						RemoveDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.SpellFlatModByLabel), firstIndex);
				}

				break;
			case SpellModType.LabelPct:
				if (apply)
				{
					AddDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.SpellPctModByLabel), (mod as SpellPctModifierByLabel).Value);
				}
				else
				{
					var firstIndex = ActivePlayerData.SpellPctModByLabel.FindIndex((mod as SpellPctModifierByLabel).Value);

					if (firstIndex >= 0)
						RemoveDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.SpellPctModByLabel), firstIndex);
				}

				break;
		}
	}

	public void ApplySpellMod(SpellInfo spellInfo, SpellModOp op, ref int basevalue, Spell spell = null)
	{
		double val = basevalue;
		ApplySpellMod(spellInfo, op, ref val, spell);
		basevalue = (int)val;
	}

	public void ApplySpellMod(SpellInfo spellInfo, SpellModOp op, ref uint basevalue, Spell spell = null)
	{
		double val = basevalue;
		ApplySpellMod(spellInfo, op, ref val, spell);
		basevalue = (uint)val;
	}

	public void ApplySpellMod(SpellInfo spellInfo, SpellModOp op, ref float basevalue, Spell spell = null)
	{
		double val = basevalue;
		ApplySpellMod(spellInfo, op, ref val, spell);
		basevalue = (float)val;
	}

	public void ApplySpellMod(SpellInfo spellInfo, SpellModOp op, ref double basevalue, Spell spell = null)
	{
		double totalmul = 1.0f;
		double totalflat = 0;

		GetSpellModValues(spellInfo, op, spell, basevalue, ref totalflat, ref totalmul);

		basevalue = (basevalue + totalflat) * totalmul;
	}

	public void GetSpellModValues<T>(SpellInfo spellInfo, SpellModOp op, Spell spell, T baseValue, ref double flat, ref double pct) where T : IComparable
	{
		flat = 0;
		pct = 1.0f;

		// Drop charges for triggering spells instead of triggered ones
		if (SpellModTakingSpell)
			spell = SpellModTakingSpell;

		switch (op)
		{
			// special case, if a mod makes spell instant, only consume that mod
			case SpellModOp.ChangeCastTime:
			{
				SpellModifier modInstantSpell = null;

				foreach (SpellModifierByClassMask mod in _spellModifiers[(int)op][(int)SpellModType.Pct])
				{
					if (!IsAffectedBySpellmod(spellInfo, mod, spell))
						continue;

					if (baseValue.CompareTo(10000d) < 0 && mod.Value <= -100)
					{
						modInstantSpell = mod;

						break;
					}
				}

				if (modInstantSpell == null)
					foreach (SpellPctModifierByLabel mod in _spellModifiers[(int)op][(int)SpellModType.LabelPct])
					{
						if (!IsAffectedBySpellmod(spellInfo, mod, spell))
							continue;

						if (baseValue.CompareTo(10000d) < 0 && mod.Value.ModifierValue <= -1.0f)
						{
							modInstantSpell = mod;

							break;
						}
					}

				if (modInstantSpell != null)
				{
					ApplyModToSpell(modInstantSpell, spell);
					pct = 0.0f;

					return;
				}

				break;
			}
			// special case if two mods apply 100% critical chance, only consume one
			case SpellModOp.CritChance:
			{
				SpellModifier modCritical = null;

				foreach (SpellModifierByClassMask mod in _spellModifiers[(int)op][(int)SpellModType.Flat])
				{
					if (!IsAffectedBySpellmod(spellInfo, mod, spell))
						continue;

					if (mod.Value >= 100)
					{
						modCritical = mod;

						break;
					}
				}

				if (modCritical == null)
					foreach (SpellFlatModifierByLabel mod in _spellModifiers[(int)op][(int)SpellModType.LabelFlat])
					{
						if (!IsAffectedBySpellmod(spellInfo, mod, spell))
							continue;

						if (mod.Value.ModifierValue >= 100)
						{
							modCritical = mod;

							break;
						}
					}

				if (modCritical != null)
				{
					ApplyModToSpell(modCritical, spell);
					flat = 100;

					return;
				}

				break;
			}
			default:
				break;
		}

		foreach (SpellModifierByClassMask mod in _spellModifiers[(int)op][(int)SpellModType.Flat])
		{
			if (!IsAffectedBySpellmod(spellInfo, mod, spell))
				continue;

			var value = mod.Value;

			if (value == 0)
				continue;

			flat += value;
			ApplyModToSpell(mod, spell);
		}

		foreach (SpellFlatModifierByLabel mod in _spellModifiers[(int)op][(int)SpellModType.LabelFlat])
		{
			if (!IsAffectedBySpellmod(spellInfo, mod, spell))
				continue;

			var value = mod.Value.ModifierValue;

			if (value == 0)
				continue;

			flat += value;
			ApplyModToSpell(mod, spell);
		}

		foreach (SpellModifierByClassMask mod in _spellModifiers[(int)op][(int)SpellModType.Pct])
		{
			if (!IsAffectedBySpellmod(spellInfo, mod, spell))
				continue;

			// skip percent mods for null basevalue (most important for spell mods with charges)
			if (baseValue + (dynamic)flat == 0)
				continue;

			var value = mod.Value;

			if (value == 0)
				continue;

			// special case (skip > 10sec spell casts for instant cast setting)
			if (op == SpellModOp.ChangeCastTime)
				if (baseValue.CompareTo(10000d) > 0 && value <= -100)
					continue;

			pct *= 1.0f + MathFunctions.CalculatePct(1.0f, value);
			ApplyModToSpell(mod, spell);
		}

		foreach (SpellPctModifierByLabel mod in _spellModifiers[(int)op][(int)SpellModType.LabelPct])
		{
			if (!IsAffectedBySpellmod(spellInfo, mod, spell))
				continue;

			// skip percent mods for null basevalue (most important for spell mods with charges)
			if (baseValue + (dynamic)flat == 0)
				continue;

			var value = mod.Value.ModifierValue;

			if (value == 1.0f)
				continue;

			// special case (skip > 10sec spell casts for instant cast setting)
			if (op == SpellModOp.ChangeCastTime)
				if (baseValue.CompareTo(10000d) > 0 && value <= -1.0f)
					continue;

			pct *= value;
			ApplyModToSpell(mod, spell);
		}
	}

	public void SetSpellModTakingSpell(Spell spell, bool apply)
	{
		if (apply && SpellModTakingSpell != null)
			return;

		if (!apply && (SpellModTakingSpell == null || SpellModTakingSpell != spell))
			return;

		SpellModTakingSpell = apply ? spell : null;
	}

	public void UpdateEquipSpellsAtFormChange()
	{
		for (byte i = 0; i < InventorySlots.BagEnd; ++i)
			if (_items[i] && !_items[i].IsBroken() && CanUseAttackType(GetAttackBySlot(i, _items[i].GetTemplate().GetInventoryType())))
			{
				ApplyItemEquipSpell(_items[i], false, true); // remove spells that not fit to form
				ApplyItemEquipSpell(_items[i], true, true);  // add spells that fit form but not active
			}

		UpdateItemSetAuras(true);
	}

	public int GetSpellPenetrationItemMod()
	{
		return _spellPenetrationItemMod;
	}

	public void RemoveArenaSpellCooldowns(bool removeActivePetCooldowns)
	{
		// remove cooldowns on spells that have < 10 min CD
		GetSpellHistory()
			.ResetCooldowns(p =>
							{
								var spellInfo = Global.SpellMgr.GetSpellInfo(p.Key, Difficulty.None);

								return spellInfo.RecoveryTime < 10 * Time.Minute * Time.InMilliseconds && spellInfo.CategoryRecoveryTime < 10 * Time.Minute * Time.InMilliseconds && !spellInfo.HasAttribute(SpellAttr6.DoNotResetCooldownInArena);
							},
							true);

		// pet cooldowns
		if (removeActivePetCooldowns)
		{
			var pet = GetPet();

			if (pet)
				pet.GetSpellHistory().ResetAllCooldowns();
		}
	}

	/**********************************/
	/*************Runes****************/
	/**********************************/
	public void SetRuneCooldown(byte index, uint cooldown)
	{
		_runes.Cooldown[index] = cooldown;
		_runes.SetRuneState(index, (cooldown == 0));
		var activeRunes = _runes.Cooldown.Count(p => p == 0);

		if (activeRunes != GetPower(PowerType.Runes))
			SetPower(PowerType.Runes, activeRunes);
	}

	public byte GetRunesState()
	{
		return (byte)(_runes.RuneState & ((1 << GetMaxPower(PowerType.Runes)) - 1));
	}

	public uint GetRuneBaseCooldown()
	{
		double cooldown = RuneCooldowns.Base;

		var regenAura = GetAuraEffectsByType(AuraType.ModPowerRegenPercent);

		foreach (var i in regenAura)
			if (i.MiscValue == (int)PowerType.Runes)
				cooldown *= 1.0f - i.Amount / 100.0f;

		// Runes cooldown are now affected by player's haste from equipment ...
		var hastePct = GetRatingBonusValue(CombatRating.HasteMelee);

		// ... and some auras.
		hastePct += GetTotalAuraModifier(AuraType.ModMeleeHaste);
		hastePct += GetTotalAuraModifier(AuraType.ModMeleeHaste2);
		hastePct += GetTotalAuraModifier(AuraType.ModMeleeHaste3);

		cooldown *= 1.0f - (hastePct / 100.0f);

		return (uint)cooldown;
	}

	public void ResyncRunes()
	{
		var maxRunes = GetMaxPower(PowerType.Runes);

		ResyncRunes data = new();
		data.Runes.Start = (byte)((1 << maxRunes) - 1);
		data.Runes.Count = GetRunesState();

		float baseCd = GetRuneBaseCooldown();

		for (byte i = 0; i < maxRunes; ++i)
			data.Runes.Cooldowns.Add((byte)((baseCd - GetRuneCooldown(i)) / baseCd * 255));

		SendPacket(data);
	}

	public void InitRunes()
	{
		if (GetClass() != Class.Deathknight)
			return;

		var runeIndex = GetPowerIndex(PowerType.Runes);

		if (runeIndex == (int)PowerType.Max)
			return;

		_runes = new Runes();
		_runes.RuneState = 0;

		for (byte i = 0; i < PlayerConst.MaxRunes; ++i)
			SetRuneCooldown(i, 0); // reset cooldowns

		// set a base regen timer equal to 10 sec
		SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.PowerRegenFlatModifier, (int)runeIndex), 0.0f);
		SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.PowerRegenInterruptedFlatModifier, (int)runeIndex), 0.0f);
	}

	public void UpdateAllRunesRegen()
	{
		if (GetClass() != Class.Deathknight)
			return;

		var runeIndex = GetPowerIndex(PowerType.Runes);

		if (runeIndex == (int)PowerType.Max)
			return;

		var runeEntry = Global.DB2Mgr.GetPowerTypeEntry(PowerType.Runes);

		var cooldown = GetRuneBaseCooldown();
		SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.PowerRegenFlatModifier, (int)runeIndex), (float)(1 * Time.InMilliseconds) / cooldown - runeEntry.RegenPeace);
		SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.PowerRegenInterruptedFlatModifier, (int)runeIndex), (float)(1 * Time.InMilliseconds) / cooldown - runeEntry.RegenCombat);
	}

	public uint GetRuneCooldown(byte index)
	{
		return _runes.Cooldown[index];
	}

	public bool CanNoReagentCast(SpellInfo spellInfo)
	{
		// don't take reagents for spells with SPELL_ATTR5_NO_REAGENT_WHILE_PREP
		if (spellInfo.HasAttribute(SpellAttr5.NoReagentCostWithAura) &&
			HasUnitFlag(UnitFlags.Preparation))
			return true;

		// Check no reagent use mask
		FlagArray128 noReagentMask = new();
		noReagentMask[0] = ActivePlayerData.NoReagentCostMask[0];
		noReagentMask[1] = ActivePlayerData.NoReagentCostMask[1];
		noReagentMask[2] = ActivePlayerData.NoReagentCostMask[2];
		noReagentMask[3] = ActivePlayerData.NoReagentCostMask[3];

		if (spellInfo.SpellFamilyFlags & noReagentMask)
			return true;

		return false;
	}

	public void SetNoRegentCostMask(FlagArray128 mask)
	{
		for (byte i = 0; i < 4; ++i)
			SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.NoReagentCostMask, i), mask[i]);
	}

	public void CastItemCombatSpell(DamageInfo damageInfo)
	{
		var target = damageInfo.GetVictim();

		if (target == null || !target.IsAlive() || target == this)
			return;

		for (var i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
		{
			// If usable, try to cast item spell
			var item = GetItemByPos(InventorySlots.Bag0, i);

			if (item != null)
				if (!item.IsBroken() && CanUseAttackType(damageInfo.GetAttackType()))
				{
					var proto = item.GetTemplate();

					if (proto != null)
					{
						// Additional check for weapons
						if (proto.GetClass() == ItemClass.Weapon)
						{
							// offhand item cannot proc from main hand hit etc
							byte slot;

							switch (damageInfo.GetAttackType())
							{
								case WeaponAttackType.BaseAttack:
								case WeaponAttackType.RangedAttack:
									slot = EquipmentSlot.MainHand;

									break;
								case WeaponAttackType.OffAttack:
									slot = EquipmentSlot.OffHand;

									break;
								default:
									slot = EquipmentSlot.End;

									break;
							}

							if (slot != i)
								continue;

							// Check if item is useable (forms or disarm)
							if (damageInfo.GetAttackType() == WeaponAttackType.BaseAttack)
								if (!IsUseEquipedWeapon(true) && !IsInFeralForm())
									continue;
						}

						CastItemCombatSpell(damageInfo, item, proto);
					}
				}
		}
	}

	public void CastItemCombatSpell(DamageInfo damageInfo, Item item, ItemTemplate proto)
	{
		// Can do effect if any damage done to target
		// for done procs allow normal + critical + absorbs by default
		var canTrigger = damageInfo.GetHitMask().HasAnyFlag(ProcFlagsHit.Normal | ProcFlagsHit.Critical | ProcFlagsHit.Absorb);

		if (canTrigger)
			if (!item.GetTemplate().HasFlag(ItemFlags.Legacy))
				foreach (var effectData in item.GetEffects())
				{
					// wrong triggering type
					if (effectData.TriggerType != ItemSpelltriggerType.OnProc)
						continue;

					var spellInfo = Global.SpellMgr.GetSpellInfo((uint)effectData.SpellID, Difficulty.None);

					if (spellInfo == null)
					{
						Log.outError(LogFilter.Player, "WORLD: unknown Item spellid {0}", effectData.SpellID);

						continue;
					}

					float chance = spellInfo.ProcChance;

					if (proto.SpellPPMRate != 0)
					{
						var WeaponSpeed = GetBaseAttackTime(damageInfo.GetAttackType());
						chance = GetPPMProcChance(WeaponSpeed, proto.SpellPPMRate, spellInfo);
					}
					else if (chance > 100.0f)
					{
						chance = GetWeaponProcChance();
					}

					if (RandomHelper.randChance(chance) && Global.ScriptMgr.RunScriptRet<IItemOnCastItemCombatSpell>(tmpscript => tmpscript.OnCastItemCombatSpell(this, damageInfo.GetVictim(), spellInfo, item), item.GetScriptId()))
						CastSpell(damageInfo.GetVictim(), spellInfo.Id, item);
				}

		// item combat enchantments
		for (byte e_slot = 0; e_slot < (byte)EnchantmentSlot.Max; ++e_slot)
		{
			var enchant_id = item.GetEnchantmentId((EnchantmentSlot)e_slot);
			var pEnchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchant_id);

			if (pEnchant == null)
				continue;

			for (byte s = 0; s < ItemConst.MaxItemEnchantmentEffects; ++s)
			{
				if (pEnchant.Effect[s] != ItemEnchantmentType.CombatSpell)
					continue;

				var entry = Global.SpellMgr.GetSpellEnchantProcEvent(enchant_id);

				if (entry != null && entry.HitMask != 0)
				{
					// Check hit/crit/dodge/parry requirement
					if ((entry.HitMask & (uint)damageInfo.GetHitMask()) == 0)
						continue;
				}
				else
				{
					// for done procs allow normal + critical + absorbs by default
					if (!canTrigger)
						continue;
				}

				// check if enchant procs only on white hits
				if (entry != null && entry.AttributesMask.HasAnyFlag(EnchantProcAttributes.WhiteHit) && damageInfo.GetSpellInfo() != null)
					continue;

				var spellInfo = Global.SpellMgr.GetSpellInfo(pEnchant.EffectArg[s], Difficulty.None);

				if (spellInfo == null)
				{
					Log.outError(LogFilter.Player,
								"Player.CastItemCombatSpell(GUID: {0}, name: {1}, enchant: {2}): unknown spell {3} is casted, ignoring...",
								GetGUID().ToString(),
								GetName(),
								enchant_id,
								pEnchant.EffectArg[s]);

					continue;
				}

				var chance = pEnchant.EffectPointsMin[s] != 0 ? pEnchant.EffectPointsMin[s] : GetWeaponProcChance();

				if (entry != null)
				{
					if (entry.ProcsPerMinute != 0)
						chance = GetPPMProcChance(proto.GetDelay(), entry.ProcsPerMinute, spellInfo);
					else if (entry.Chance != 0)
						chance = entry.Chance;
				}

				// Apply spell mods
				ApplySpellMod(spellInfo, SpellModOp.ProcChance, ref chance);

				// Shiv has 100% chance to apply the poison
				if (FindCurrentSpellBySpellId(5938) != null && e_slot == (byte)EnchantmentSlot.Temp)
					chance = 100.0f;

				if (RandomHelper.randChance(chance))
				{
					if (spellInfo.IsPositive)
						CastSpell(this, spellInfo.Id, item);
					else
						CastSpell(damageInfo.GetVictim(), spellInfo.Id, item);
				}

				if (RandomHelper.randChance(chance))
				{
					var target = spellInfo.IsPositive ? this : damageInfo.GetVictim();

					CastSpellExtraArgs args = new(item);

					// reduce effect values if enchant is limited
					if (entry != null && entry.AttributesMask.HasAnyFlag(EnchantProcAttributes.Limit60) && target.GetLevelForTarget(this) > 60)
					{
						var lvlDifference = (int)target.GetLevelForTarget(this) - 60;
						var lvlPenaltyFactor = 4; // 4% lost effectiveness per level

						var effectPct = Math.Max(0, 100 - (lvlDifference * lvlPenaltyFactor));

						foreach (var spellEffectInfo in spellInfo.Effects)
							if (spellEffectInfo.IsEffect())
								args.AddSpellMod(SpellValueMod.BasePoint0 + spellEffectInfo.EffectIndex, MathFunctions.CalculatePct(spellEffectInfo.CalcValue(this), effectPct));
					}

					CastSpell(target, spellInfo.Id, args);
				}
			}
		}
	}

	public void ResetSpells(bool myClassOnly = false)
	{
		// not need after this call
		if (HasAtLoginFlag(AtLoginFlags.ResetSpells))
			RemoveAtLoginFlag(AtLoginFlags.ResetSpells, true);

		// make full copy of map (spells removed and marked as deleted at another spell remove
		// and we can't use original map for safe iterative with visit each spell at loop end
		var smap = GetSpellMap();

		uint family;

		if (myClassOnly)
		{
			var clsEntry = CliDB.ChrClassesStorage.LookupByKey(GetClass());

			if (clsEntry == null)
				return;

			family = clsEntry.SpellClassSet;

			foreach (var spellId in smap.Keys)
			{
				var spellInfo = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None);

				if (spellInfo == null)
					continue;

				// skip server-side/triggered spells
				if (spellInfo.SpellLevel == 0)
					continue;

				// skip wrong class/race skills
				if (!IsSpellFitByClassAndRace(spellInfo.Id))
					continue;

				// skip other spell families
				if ((uint)spellInfo.SpellFamilyName != family)
					continue;

				// skip broken spells
				if (!Global.SpellMgr.IsSpellValid(spellInfo, this, false))
					continue;
			}
		}
		else
		{
			foreach (var spellId in smap.Keys)
				RemoveSpell(spellId, false, false); // only iter.first can be accessed, object by iter.second can be deleted already
		}

		LearnDefaultSkills();
		LearnCustomSpells();
		LearnQuestRewardedSpells();
	}

	public void SetPetSpellPower(uint spellPower)
	{
		SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.PetSpellPower), spellPower);
	}

	public void SetSkillLineId(uint pos, ushort skillLineId)
	{
		SkillInfo skillInfo = Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Skill);
		SetUpdateFieldValue(ref skillInfo.ModifyValue(skillInfo.SkillLineID, (int)pos), skillLineId);
	}

	public void SetSkillStep(uint pos, ushort step)
	{
		SkillInfo skillInfo = Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Skill);
		SetUpdateFieldValue(ref skillInfo.ModifyValue(skillInfo.SkillStep, (int)pos), step);
	}

	public void SetSkillRank(uint pos, ushort rank)
	{
		SkillInfo skillInfo = Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Skill);
		SetUpdateFieldValue(ref skillInfo.ModifyValue(skillInfo.SkillRank, (int)pos), rank);
	}

	public void SetSkillStartingRank(uint pos, ushort starting)
	{
		SkillInfo skillInfo = Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Skill);
		SetUpdateFieldValue(ref skillInfo.ModifyValue(skillInfo.SkillStartingRank, (int)pos), starting);
	}

	public void SetSkillMaxRank(uint pos, ushort max)
	{
		SkillInfo skillInfo = Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Skill);
		SetUpdateFieldValue(ref skillInfo.ModifyValue(skillInfo.SkillMaxRank, (int)pos), max);
	}

	public void SetSkillTempBonus(uint pos, ushort bonus)
	{
		SkillInfo skillInfo = Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Skill);
		SetUpdateFieldValue(ref skillInfo.ModifyValue(skillInfo.SkillTempBonus, (int)pos), bonus);
	}

	public void SetSkillPermBonus(uint pos, ushort bonus)
	{
		SkillInfo skillInfo = Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Skill);
		SetUpdateFieldValue(ref skillInfo.ModifyValue(skillInfo.SkillPermBonus, (int)pos), bonus);
	}

	ushort GetMaxSkillValue(SkillType skill)
	{
		if (skill == 0)
			return 0;

		SkillInfo skillInfo = ActivePlayerData.Skill;

		var skillStatusData = _skillStatus.LookupByKey(skill);

		if (skillStatusData == null || skillStatusData.State == SkillState.Deleted || skillInfo.SkillRank[skillStatusData.Pos] == 0)
			return 0;

		int result = skillInfo.SkillMaxRank[skillStatusData.Pos];
		result += skillInfo.SkillTempBonus[skillStatusData.Pos];
		result += skillInfo.SkillPermBonus[skillStatusData.Pos];

		return (ushort)(result < 0 ? 0 : result);
	}

	void InitializeSelfResurrectionSpells()
	{
		ClearSelfResSpell();

		var spells = new uint[3];

		var dummyAuras = GetAuraEffectsByType(AuraType.Dummy);

		foreach (var auraEffect in dummyAuras)
			// Soulstone Resurrection                           // prio: 3 (max, non death persistent)
			if (auraEffect.SpellInfo.SpellFamilyName == SpellFamilyNames.Warlock && auraEffect.SpellInfo.SpellFamilyFlags[1].HasAnyFlag(0x1000000u))
				spells[0] = 3026;
			// Twisting Nether                                  // prio: 2 (max)
			else if (auraEffect.Id == 23701 && RandomHelper.randChance(10))
				spells[1] = 23700;

		// Reincarnation (passive spell)  // prio: 1
		if (HasSpell(20608) && !GetSpellHistory().HasCooldown(21169))
			spells[2] = 21169;

		foreach (var selfResSpell in spells)
			if (selfResSpell != 0)
				AddSelfResSpell(selfResSpell);
	}

	void RemoveSpecializationSpells()
	{
		for (uint i = 0; i < PlayerConst.MaxSpecializations; ++i)
		{
			var specialization = Global.DB2Mgr.GetChrSpecializationByIndex(GetClass(), i);

			if (specialization != null)
			{
				var specSpells = Global.DB2Mgr.GetSpecializationSpells(specialization.Id);

				if (specSpells != null)
					for (var j = 0; j < specSpells.Count; ++j)
					{
						var specSpell = specSpells[j];
						RemoveSpell(specSpell.SpellID, true);

						if (specSpell.OverridesSpellID != 0)
							RemoveOverrideSpell(specSpell.OverridesSpellID, specSpell.SpellID);
					}

				for (uint j = 0; j < PlayerConst.MaxMasterySpells; ++j)
				{
					var mastery = specialization.MasterySpellID[j];

					if (mastery != 0)
						RemoveAura(mastery);
				}
			}
		}
	}

	void InitializeSkillFields()
	{
		uint i = 0;

		foreach (var skillLine in CliDB.SkillLineStorage.Values)
		{
			var rcEntry = Global.DB2Mgr.GetSkillRaceClassInfo(skillLine.Id, GetRace(), GetClass());

			if (rcEntry != null)
			{
				SetSkillLineId(i, (ushort)skillLine.Id);
				SetSkillStartingRank(i, 1);
				_skillStatus.Add(skillLine.Id, new SkillStatusData(i, SkillState.Unchanged));

				if (++i >= SkillConst.MaxPlayerSkills)
					break;
			}
		}
	}

	void UpdateSkillEnchantments(uint skill_id, ushort curr_value, ushort new_value)
	{
		for (byte i = 0; i < InventorySlots.BagEnd; ++i)
			if (_items[i] != null)
				for (EnchantmentSlot slot = 0; slot < EnchantmentSlot.Max; ++slot)
				{
					var ench_id = _items[i].GetEnchantmentId(slot);

					if (ench_id == 0)
						continue;

					var Enchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(ench_id);

					if (Enchant == null)
						return;

					if (Enchant.RequiredSkillID == skill_id)
					{
						// Checks if the enchantment needs to be applied or removed
						if (curr_value < Enchant.RequiredSkillRank && new_value >= Enchant.RequiredSkillRank)
							ApplyEnchantment(_items[i], slot, true);
						else if (new_value < Enchant.RequiredSkillRank && curr_value >= Enchant.RequiredSkillRank)
							ApplyEnchantment(_items[i], slot, false);
					}

					// If we're dealing with a gem inside a prismatic socket we need to check the prismatic socket requirements
					// rather than the gem requirements itself. If the socket has no color it is a prismatic socket.
					if ((slot == EnchantmentSlot.Sock1 || slot == EnchantmentSlot.Sock2 || slot == EnchantmentSlot.Sock3) && _items[i].GetSocketColor((uint)(slot - EnchantmentSlot.Sock1)) == 0)
					{
						var pPrismaticEnchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(_items[i].GetEnchantmentId(EnchantmentSlot.Prismatic));

						if (pPrismaticEnchant != null && pPrismaticEnchant.RequiredSkillID == skill_id)
						{
							if (curr_value < pPrismaticEnchant.RequiredSkillRank && new_value >= pPrismaticEnchant.RequiredSkillRank)
								ApplyEnchantment(_items[i], slot, true);
							else if (new_value < pPrismaticEnchant.RequiredSkillRank && curr_value >= pPrismaticEnchant.RequiredSkillRank)
								ApplyEnchantment(_items[i], slot, false);
						}
					}
				}
	}

	void UpdateEnchantTime(uint time)
	{
		for (var i = 0; i < _enchantDurations.Count; ++i)
		{
			var enchantDuration = _enchantDurations[i];

			if (enchantDuration.Item.GetEnchantmentId(enchantDuration.Slot) == 0)
			{
				_enchantDurations.Remove(enchantDuration);
			}
			else if (enchantDuration.Leftduration <= time)
			{
				ApplyEnchantment(enchantDuration.Item, enchantDuration.Slot, false, false);
				enchantDuration.Item.ClearEnchantment(enchantDuration.Slot);
				_enchantDurations.Remove(enchantDuration);
			}
			else if (enchantDuration.Leftduration > time)
			{
				enchantDuration.Leftduration -= time;
			}
		}
	}

	void ApplyEnchantment(Item item, bool apply)
	{
		for (EnchantmentSlot slot = 0; slot < EnchantmentSlot.Max; ++slot)
			ApplyEnchantment(item, slot, apply);
	}

	void AddEnchantmentDurations(Item item)
	{
		for (EnchantmentSlot x = 0; x < EnchantmentSlot.Max; ++x)
		{
			if (item.GetEnchantmentId(x) == 0)
				continue;

			var duration = item.GetEnchantmentDuration(x);

			if (duration > 0)
				AddEnchantmentDuration(item, x, duration);
		}
	}

	void AddEnchantmentDuration(Item item, EnchantmentSlot slot, uint duration)
	{
		if (item == null)
			return;

		if (slot >= EnchantmentSlot.Max)
			return;

		for (var i = 0; i < _enchantDurations.Count; ++i)
		{
			var enchantDuration = _enchantDurations[i];

			if (enchantDuration.Item == item && enchantDuration.Slot == slot)
			{
				enchantDuration.Item.SetEnchantmentDuration(enchantDuration.Slot, enchantDuration.Leftduration, this);
				_enchantDurations.Remove(enchantDuration);

				break;
			}
		}

		if (duration > 0)
		{
			GetSession().SendItemEnchantTimeUpdate(GetGUID(), item.GetGUID(), (uint)slot, duration / 1000);
			_enchantDurations.Add(new EnchantDuration(item, slot, duration));
		}
	}

	void RemoveEnchantmentDurations(Item item)
	{
		for (var i = 0; i < _enchantDurations.Count; ++i)
		{
			var enchantDuration = _enchantDurations[i];

			if (enchantDuration.Item == item)
			{
				// save duration in item
				item.SetEnchantmentDuration(enchantDuration.Slot, enchantDuration.Leftduration, this);
				_enchantDurations.Remove(enchantDuration);
			}
		}
	}

	void RemoveEnchantmentDurationsReferences(Item item)
	{
		for (var i = 0; i < _enchantDurations.Count; ++i)
		{
			var enchantDuration = _enchantDurations[i];

			if (enchantDuration.Item == item)
				_enchantDurations.Remove(enchantDuration);
		}
	}

	byte GetFishingStepsNeededToLevelUp(uint SkillValue)
	{
		// These formulas are guessed to be as close as possible to how the skill difficulty curve for fishing was on Retail.
		if (SkillValue < 75)
			return 1;

		if (SkillValue <= 300)
			return (byte)(SkillValue / 44);

		return (byte)(SkillValue / 31);
	}

	int SkillGainChance(uint SkillValue, uint GrayLevel, uint GreenLevel, uint YellowLevel)
	{
		if (SkillValue >= GrayLevel)
			return WorldConfig.GetIntValue(WorldCfg.SkillChanceGrey) * 10;

		if (SkillValue >= GreenLevel)
			return WorldConfig.GetIntValue(WorldCfg.SkillChanceGreen) * 10;

		if (SkillValue >= YellowLevel)
			return WorldConfig.GetIntValue(WorldCfg.SkillChanceYellow) * 10;

		return WorldConfig.GetIntValue(WorldCfg.SkillChanceOrange) * 10;
	}

	bool EnchantmentFitsRequirements(uint enchantmentcondition, sbyte slot)
	{
		if (enchantmentcondition == 0)
			return true;

		var Condition = CliDB.SpellItemEnchantmentConditionStorage.LookupByKey(enchantmentcondition);

		if (Condition == null)
			return true;

		byte[] curcount =
		{
			0, 0, 0, 0
		};

		//counting current equipped gem colors
		for (var i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
		{
			if (i == slot)
				continue;

			var pItem2 = GetItemByPos(InventorySlots.Bag0, i);

			if (pItem2 != null && !pItem2.IsBroken())
				foreach (var gemData in pItem2.ItemData.Gems)
				{
					var gemProto = Global.ObjectMgr.GetItemTemplate(gemData.ItemId);

					if (gemProto == null)
						continue;

					var gemProperty = CliDB.GemPropertiesStorage.LookupByKey(gemProto.GetGemProperties());

					if (gemProperty == null)
						continue;

					var GemColor = (uint)gemProperty.Type;

					for (byte b = 0, tmpcolormask = 1; b < 4; b++, tmpcolormask <<= 1)
						if (Convert.ToBoolean(tmpcolormask & GemColor))
							++curcount[b];
				}
		}

		var activate = true;

		for (byte i = 0; i < 5; i++)
		{
			if (Condition.LtOperandType[i] == 0)
				continue;

			uint _cur_gem = curcount[Condition.LtOperandType[i] - 1];

			// if have <CompareColor> use them as count, else use <value> from Condition
			uint _cmp_gem = Condition.RtOperandType[i] != 0 ? curcount[Condition.RtOperandType[i] - 1] : Condition.RtOperand[i];

			switch (Condition.Operator[i])
			{
				case 2: // requires less <color> than (<value> || <comparecolor>) gems
					activate &= (_cur_gem < _cmp_gem);

					break;
				case 3: // requires more <color> than (<value> || <comparecolor>) gems
					activate &= (_cur_gem > _cmp_gem);

					break;
				case 5: // requires at least <color> than (<value> || <comparecolor>) gems
					activate &= (_cur_gem >= _cmp_gem);

					break;
			}
		}

		Log.outDebug(LogFilter.Player, "Checking Condition {0}, there are {1} Meta Gems, {2} Red Gems, {3} Yellow Gems and {4} Blue Gems, Activate:{5}", enchantmentcondition, curcount[0], curcount[1], curcount[2], curcount[3], activate ? "yes" : "no");

		return activate;
	}

	void CorrectMetaGemEnchants(byte exceptslot, bool apply)
	{
		//cycle all equipped items
		for (var slot = EquipmentSlot.Start; slot < EquipmentSlot.End; ++slot)
		{
			//enchants for the slot being socketed are handled by Player.ApplyItemMods
			if (slot == exceptslot)
				continue;

			var pItem = GetItemByPos(InventorySlots.Bag0, slot);

			if (pItem == null || pItem.GetSocketColor(0) == 0)
				continue;

			for (var enchant_slot = EnchantmentSlot.Sock1; enchant_slot < EnchantmentSlot.Sock3; ++enchant_slot)
			{
				var enchant_id = pItem.GetEnchantmentId(enchant_slot);

				if (enchant_id == 0)
					continue;

				var enchantEntry = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchant_id);

				if (enchantEntry == null)
					continue;

				uint condition = enchantEntry.ConditionID;

				if (condition != 0)
				{
					//was enchant active with/without item?
					var wasactive = EnchantmentFitsRequirements(condition, (sbyte)(apply ? exceptslot : -1));

					//should it now be?
					if (wasactive ^ EnchantmentFitsRequirements(condition, (sbyte)(apply ? -1 : exceptslot)))
						// ignore item gem conditions
						//if state changed, (dis)apply enchant
						ApplyEnchantment(pItem, enchant_slot, !wasactive, true, true);
				}
			}
		}
	}

	int FindEmptyProfessionSlotFor(uint skillId)
	{
		var skillEntry = CliDB.SkillLineStorage.LookupByKey(skillId);

		if (skillEntry == null)
			return -1;

		if (skillEntry.ParentSkillLineID != 0 || skillEntry.CategoryID != SkillCategory.Profession)
			return -1;

		var index = 0;

		// if there is no same profession, find any free slot
		foreach (var b in ActivePlayerData.ProfessionSkillLine)
		{
			if (b == 0)
				return index;

			index++;
		}

		return -1;
	}

	void RemoveItemDependentAurasAndCasts(Item pItem)
	{
		GetOwnedAurasList()
			.CallOnMatch((aura) =>
						{
							// skip not self applied auras
							var spellInfo = aura.SpellInfo;

							if (aura.CasterGuid != GetGUID())
								return false;

							// skip if not item dependent or have alternative item
							if (HasItemFitToSpellRequirements(spellInfo, pItem))
								return false;

							// no alt item, remove aura, restart check
							return true;
						},
						(pair) => RemoveOwnedAura(pair));

		// currently casted spells can be dependent from item
		for (CurrentSpellTypes i = 0; i < CurrentSpellTypes.Max; ++i)
		{
			var spell = GetCurrentSpell(i);

			if (spell != null)
				if (spell.State != SpellState.Delayed && !HasItemFitToSpellRequirements(spell.SpellInfo, pItem))
					InterruptSpell(i);
		}
	}

	void CastAllObtainSpells()
	{
		var inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();

		for (var slot = InventorySlots.ItemStart; slot < inventoryEnd; ++slot)
		{
			var item = GetItemByPos(InventorySlots.Bag0, slot);

			if (item)
				ApplyItemObtainSpells(item, true);
		}

		for (var i = InventorySlots.BagStart; i < InventorySlots.BagEnd; ++i)
		{
			var bag = GetBagByPos(i);

			if (!bag)
				continue;

			for (byte slot = 0; slot < bag.GetBagSize(); ++slot)
			{
				var item = bag.GetItemByPos(slot);

				if (item)
					ApplyItemObtainSpells(item, true);
			}
		}
	}

	void ApplyItemObtainSpells(Item item, bool apply)
	{
		if (item.GetTemplate().HasFlag(ItemFlags.Legacy))
			return;

		foreach (var effect in item.GetEffects())
		{
			if (effect.TriggerType != ItemSpelltriggerType.OnPickup) // On obtain trigger
				continue;

			var spellId = effect.SpellID;

			if (spellId <= 0)
				continue;

			if (apply)
			{
				if (!HasAura((uint)spellId))
					CastSpell(this, (uint)spellId, new CastSpellExtraArgs().SetCastItem(item));
			}
			else
			{
				RemoveAura((uint)spellId);
			}
		}
	}

	// this one rechecks weapon auras and stores them in BaseModGroup container
	// needed for things like axe specialization applying only to axe weapons in case of dual-wield
	void UpdateWeaponDependentCritAuras(WeaponAttackType attackType)
	{
		BaseModGroup modGroup;

		switch (attackType)
		{
			case WeaponAttackType.BaseAttack:
				modGroup = BaseModGroup.CritPercentage;

				break;
			case WeaponAttackType.OffAttack:
				modGroup = BaseModGroup.OffhandCritPercentage;

				break;
			case WeaponAttackType.RangedAttack:
				modGroup = BaseModGroup.RangedCritPercentage;

				break;
			default:
				return;
		}

		double amount = 0.0f;
		amount += GetTotalAuraModifier(AuraType.ModWeaponCritPercent, auraEffect => CheckAttackFitToAuraRequirement(attackType, auraEffect));

		// these auras don't have item requirement (only Combat Expertise in 3.3.5a)
		amount += GetTotalAuraModifier(AuraType.ModCritPct);

		SetBaseModFlatValue(modGroup, amount);
	}

	void SendKnownSpells()
	{
		SendKnownSpells knownSpells = new();
		knownSpells.InitialLogin = IsLoading();

		foreach (var spell in _spells.ToList())
		{
			if (spell.Value.State == PlayerSpellState.Removed)
				continue;

			if (!spell.Value.Active || spell.Value.Disabled)
				continue;

			knownSpells.KnownSpells.Add(spell.Key);

			if (spell.Value.Favorite)
				knownSpells.FavoriteSpells.Add(spell.Key);
		}

		SendPacket(knownSpells);
	}

	void SendUnlearnSpells()
	{
		SendPacket(new SendUnlearnSpells());
	}

	bool HandlePassiveSpellLearn(SpellInfo spellInfo)
	{
		// note: form passives activated with shapeshift spells be implemented by HandleShapeshiftBoosts instead of spell_learn_spell
		// talent dependent passives activated at form apply have proper stance data
		var form = GetShapeshiftForm();

		var need_cast = (spellInfo.Stances == 0 ||
						(form != 0 && Convert.ToBoolean(spellInfo.Stances & (1ul << ((int)form - 1)))) ||
						(form == 0 && spellInfo.HasAttribute(SpellAttr2.AllowWhileNotShapeshiftedCasterForm)));

		// Check EquippedItemClass
		// passive spells which apply aura and have an item requirement are to be added manually, instead of casted
		if (spellInfo.EquippedItemClass >= 0)
			foreach (var spellEffectInfo in spellInfo.Effects)
				if (spellEffectInfo.IsAura())
				{
					if (!HasAura(spellInfo.Id) && HasItemFitToSpellRequirements(spellInfo))
						AddAura(spellInfo.Id, this);

					return false;
				}

		//Check CasterAuraStates
		return need_cast && (spellInfo.CasterAuraState == 0 || HasAuraState(spellInfo.CasterAuraState));
	}

	bool AddSpell(uint spellId, bool active, bool learning, bool dependent, bool disabled, bool loading = false, uint fromSkill = 0, bool favorite = false, int? traitDefinitionId = null)
	{
		var spellInfo = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None);

		if (spellInfo == null)
		{
			// do character spell book cleanup (all characters)
			if (!IsInWorld && !learning)
			{
				Log.outError(LogFilter.Spells, "Player.AddSpell: Spell (ID: {0}) does not exist. deleting for all characters in `character_spell`.", spellId);

				DeleteSpellFromAllPlayers(spellId);
			}
			else
			{
				Log.outError(LogFilter.Spells, "Player.AddSpell: Spell (ID: {0}) does not exist", spellId);
			}

			return false;
		}

		if (!Global.SpellMgr.IsSpellValid(spellInfo, this, false))
		{
			// do character spell book cleanup (all characters)
			if (!IsInWorld && !learning)
			{
				Log.outError(LogFilter.Spells, "Player.AddSpell: Spell (ID: {0}) is invalid. deleting for all characters in `character_spell`.", spellId);

				DeleteSpellFromAllPlayers(spellId);
			}
			else
			{
				Log.outError(LogFilter.Spells, "Player.AddSpell: Spell (ID: {0}) is invalid", spellId);
			}

			return false;
		}

		var state = learning ? PlayerSpellState.New : PlayerSpellState.Unchanged;

		var dependent_set = false;
		var disabled_case = false;
		var superceded_old = false;

		var spell = _spells.LookupByKey(spellId);

		if (spell != null && spell.State == PlayerSpellState.Temporary)
			RemoveTemporarySpell(spellId);

		if (spell != null)
		{
			uint next_active_spell_id = 0;

			// fix activate state for non-stackable low rank (and find next spell for !active case)
			if (spellInfo.IsRanked)
			{
				var next = Global.SpellMgr.GetNextSpellInChain(spellId);

				if (next != 0)
					if (HasSpell(next))
					{
						// high rank already known so this must !active
						active = false;
						next_active_spell_id = next;
					}
			}

			// not do anything if already known in expected state
			if (spell.State != PlayerSpellState.Removed &&
				spell.Active == active &&
				spell.Dependent == dependent &&
				spell.Disabled == disabled)
			{
				if (!IsInWorld && !learning)
					spell.State = PlayerSpellState.Unchanged;

				return false;
			}

			// dependent spell known as not dependent, overwrite state
			if (spell.State != PlayerSpellState.Removed && !spell.Dependent && dependent)
			{
				spell.Dependent = dependent;

				if (spell.State != PlayerSpellState.New)
					spell.State = PlayerSpellState.Changed;

				dependent_set = true;
			}

			if (spell.TraitDefinitionId != traitDefinitionId)
			{
				if (spell.TraitDefinitionId.HasValue)
				{
					var traitDefinition = CliDB.TraitDefinitionStorage.LookupByKey(spell.TraitDefinitionId.Value);

					if (traitDefinition != null)
						RemoveOverrideSpell((uint)traitDefinition.OverridesSpellID, spellId);
				}

				spell.TraitDefinitionId = traitDefinitionId;
			}

			spell.Favorite = favorite;

			// update active state for known spell
			if (spell.Active != active && spell.State != PlayerSpellState.Removed && !spell.Disabled)
			{
				spell.Active = active;

				if (!IsInWorld && !learning && !dependent_set) // explicitly load from DB and then exist in it already and set correctly
					spell.State = PlayerSpellState.Unchanged;
				else if (spell.State != PlayerSpellState.New)
					spell.State = PlayerSpellState.Changed;

				if (active)
				{
					if (spellInfo.IsPassive && HandlePassiveSpellLearn(spellInfo))
						CastSpell(this, spellId, true);
				}
				else if (IsInWorld)
				{
					if (next_active_spell_id != 0)
					{
						SendSupercededSpell(spellId, next_active_spell_id);
					}
					else
					{
						UnlearnedSpells removedSpells = new();
						removedSpells.SpellID.Add(spellId);
						SendPacket(removedSpells);
					}
				}

				return active;
			}

			if (spell.Disabled != disabled && spell.State != PlayerSpellState.Removed)
			{
				if (spell.State != PlayerSpellState.New)
					spell.State = PlayerSpellState.Changed;

				spell.Disabled = disabled;

				if (disabled)
					return false;

				disabled_case = true;
			}
			else
			{
				switch (spell.State)
				{
					case PlayerSpellState.Unchanged:
						return false;
					case PlayerSpellState.Removed:
					{
						_spells.Remove(spellId);
						state = PlayerSpellState.Changed;

						break;
					}
					default:
					{
						// can be in case spell loading but learned at some previous spell loading
						if (!IsInWorld && !learning && !dependent_set)
							spell.State = PlayerSpellState.Unchanged;

						return false;
					}
				}
			}
		}

		if (!disabled_case) // skip new spell adding if spell already known (disabled spells case)
		{
			// non talent spell: learn low ranks (recursive call)
			var prev_spell = Global.SpellMgr.GetPrevSpellInChain(spellId);

			if (prev_spell != 0)
			{
				if (!IsInWorld || disabled) // at spells loading, no output, but allow save
					AddSpell(prev_spell, active, true, true, disabled, false, fromSkill);
				else // at normal learning
					LearnSpell(prev_spell, true, fromSkill);
			}

			PlayerSpell newspell = new();
			newspell.State = state;
			newspell.Active = active;
			newspell.Dependent = dependent;
			newspell.Disabled = disabled;
			newspell.Favorite = favorite;

			if (traitDefinitionId.HasValue)
				newspell.TraitDefinitionId = traitDefinitionId.Value;

			// replace spells in action bars and spellbook to bigger rank if only one spell rank must be accessible
			if (newspell.Active && !newspell.Disabled && spellInfo.IsRanked)
				foreach (var _spell in _spells)
				{
					if (_spell.Value.State == PlayerSpellState.Removed)
						continue;

					var i_spellInfo = Global.SpellMgr.GetSpellInfo(_spell.Key, Difficulty.None);

					if (i_spellInfo == null)
						continue;

					if (spellInfo.IsDifferentRankOf(i_spellInfo))
						if (_spell.Value.Active)
						{
							if (spellInfo.IsHighRankOf(i_spellInfo))
							{
								if (IsInWorld) // not send spell (re-/over-)learn packets at loading
									SendSupercededSpell(_spell.Key, spellId);

								// mark old spell as disable (SMSG_SUPERCEDED_SPELL replace it in client by new)
								_spell.Value.Active = false;

								if (_spell.Value.State != PlayerSpellState.New)
									_spell.Value.State = PlayerSpellState.Changed;

								superceded_old = true; // new spell replace old in action bars and spell book.
							}
							else
							{
								if (IsInWorld) // not send spell (re-/over-)learn packets at loading
									SendSupercededSpell(spellId, _spell.Key);

								// mark new spell as disable (not learned yet for client and will not learned)
								newspell.Active = false;

								if (newspell.State != PlayerSpellState.New)
									newspell.State = PlayerSpellState.Changed;
							}
						}
				}

			_spells[spellId] = newspell;

			// return false if spell disabled
			if (newspell.Disabled)
				return false;
		}

		var castSpell = false;

		// cast talents with SPELL_EFFECT_LEARN_SPELL (other dependent spells will learned later as not auto-learned)
		// note: all spells with SPELL_EFFECT_LEARN_SPELL isn't passive
		if (!loading && spellInfo.HasAttribute(SpellCustomAttributes.IsTalent) && spellInfo.HasEffect(SpellEffectName.LearnSpell))
			// ignore stance requirement for talent learn spell (stance set for spell only for client spell description show)
			castSpell = true;
		// also cast passive spells (including all talents without SPELL_EFFECT_LEARN_SPELL) with additional checks
		else if (spellInfo.IsPassive)
			castSpell = HandlePassiveSpellLearn(spellInfo);
		else if (spellInfo.HasEffect(SpellEffectName.SkillStep))
			castSpell = true;
		else if (spellInfo.HasAttribute(SpellAttr1.CastWhenLearned))
			castSpell = true;

		if (castSpell)
		{
			CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);

			if (traitDefinitionId.HasValue)
			{
				var traitConfig = GetTraitConfig((int)(uint)ActivePlayerData.ActiveCombatTraitConfigID);

				if (traitConfig != null)
				{
					var traitEntryIndex = traitConfig.Entries.FindIndexIf(traitEntry => { return CliDB.TraitNodeEntryStorage.LookupByKey(traitEntry.TraitNodeEntryID)?.TraitDefinitionID == traitDefinitionId; });

					var rank = 0;

					if (traitEntryIndex >= 0)
						rank = traitConfig.Entries[traitEntryIndex].Rank + traitConfig.Entries[traitEntryIndex].GrantedRanks;

					if (rank > 0)
					{
						var traitDefinitionEffectPoints = TraitMgr.GetTraitDefinitionEffectPointModifiers(traitDefinitionId.Value);

						if (traitDefinitionEffectPoints != null)
							foreach (var traitDefinitionEffectPoint in traitDefinitionEffectPoints)
							{
								if (traitDefinitionEffectPoint.EffectIndex >= spellInfo.Effects.Count)
									continue;

								double basePoints = Global.DB2Mgr.GetCurveValueAt((uint)traitDefinitionEffectPoint.CurveID, rank);

								if (traitDefinitionEffectPoint.GetOperationType() == TraitPointsOperationType.Multiply)
									basePoints *= spellInfo.GetEffect(traitDefinitionEffectPoint.EffectIndex).CalcBaseValue(this, null, 0, -1);

								args.AddSpellMod(SpellValueMod.BasePoint0 + traitDefinitionEffectPoint.EffectIndex, basePoints);
							}
					}
				}
			}

			CastSpell(this, spellId, args);

			if (spellInfo.HasEffect(SpellEffectName.SkillStep))
				return false;
		}

		if (traitDefinitionId.HasValue)
		{
			var traitDefinition = CliDB.TraitDefinitionStorage.LookupByKey(traitDefinitionId.Value);

			if (traitDefinition != null)
				AddOverrideSpell(traitDefinition.OverridesSpellID, spellId);
		}

		// update free primary prof.points (if any, can be none in case GM .learn prof. learning)
		var freeProfs = GetFreePrimaryProfessionPoints();

		if (freeProfs != 0)
			if (spellInfo.IsPrimaryProfessionFirstRank)
				SetFreePrimaryProfessions(freeProfs - 1);

		var skill_bounds = Global.SpellMgr.GetSkillLineAbilityMapBounds(spellId);

		var spellLearnSkill = Global.SpellMgr.GetSpellLearnSkill(spellId);

		if (spellLearnSkill != null)
		{
			// add dependent skills if this spell is not learned from adding skill already
			if ((uint)spellLearnSkill.Skill != fromSkill)
			{
				var skill_value = GetPureSkillValue(spellLearnSkill.Skill);
				var skill_max_value = GetPureMaxSkillValue(spellLearnSkill.Skill);

				if (skill_value < spellLearnSkill.Value)
					skill_value = spellLearnSkill.Value;

				var new_skill_max_value = spellLearnSkill.Maxvalue == 0 ? GetMaxSkillValueForLevel() : spellLearnSkill.Maxvalue;

				if (skill_max_value < new_skill_max_value)
					skill_max_value = new_skill_max_value;

				SetSkill(spellLearnSkill.Skill, spellLearnSkill.Step, skill_value, skill_max_value);
			}
		}
		else
		{
			// not ranked skills
			foreach (var _spell_idx in skill_bounds)
			{
				var pSkill = CliDB.SkillLineStorage.LookupByKey(_spell_idx.SkillLine);

				if (pSkill == null)
					continue;

				if (_spell_idx.SkillLine == fromSkill)
					continue;

				// Runeforging special case
				if ((_spell_idx.AcquireMethod == AbilityLearnType.OnSkillLearn && !HasSkill((SkillType)_spell_idx.SkillLine)) || ((_spell_idx.SkillLine == (int)SkillType.Runeforging) && _spell_idx.TrivialSkillLineRankHigh == 0))
				{
					var rcInfo = Global.DB2Mgr.GetSkillRaceClassInfo(_spell_idx.SkillLine, GetRace(), GetClass());

					if (rcInfo != null)
						LearnDefaultSkill(rcInfo);
				}
			}
		}


		// learn dependent spells
		var spell_bounds = Global.SpellMgr.GetSpellLearnSpellMapBounds(spellId);

		foreach (var spellNode in spell_bounds)
		{
			if (!spellNode.AutoLearned)
			{
				if (!IsInWorld || !spellNode.Active) // at spells loading, no output, but allow save
					AddSpell(spellNode.Spell, spellNode.Active, true, true, false);
				else // at normal learning
					LearnSpell(spellNode.Spell, true);
			}

			if (spellNode.OverridesSpell != 0 && spellNode.Active)
				AddOverrideSpell(spellNode.OverridesSpell, spellNode.Spell);
		}

		if (!GetSession().PlayerLoading())
		{
			// not ranked skills
			foreach (var _spell_idx in skill_bounds)
			{
				UpdateCriteria(CriteriaType.LearnTradeskillSkillLine, _spell_idx.SkillLine);
				UpdateCriteria(CriteriaType.LearnSpellFromSkillLine, _spell_idx.SkillLine);
			}

			UpdateCriteria(CriteriaType.LearnOrKnowSpell, spellId);
		}

		// needs to be when spell is already learned, to prevent infinite recursion crashes
		if (Global.DB2Mgr.GetMount(spellId) != null)
			GetSession().GetCollectionMgr().AddMount(spellId, MountStatusFlags.None, false, !IsInWorld);

		// return true (for send learn packet) only if spell active (in case ranked spells) and not replace old spell
		return active && !disabled && !superceded_old;
	}

	bool IsAffectedBySpellmod(SpellInfo spellInfo, SpellModifier mod, Spell spell)
	{
		if (mod == null || spellInfo == null)
			return false;

		// First time this aura applies a mod to us and is out of charges
		if (spell && mod.OwnerAura.IsUsingCharges && mod.OwnerAura.Charges == 0 && !spell.AppliedMods.Contains(mod.OwnerAura))
			return false;

		switch (mod.Op)
		{
			case SpellModOp.Duration: // +duration to infinite duration spells making them limited
				if (spellInfo.Duration == -1)
					return false;

				break;
			case SpellModOp.CritChance: // mod crit to spells that can't crit
				if (!spellInfo.HasAttribute(SpellCustomAttributes.CanCrit))
					return false;

				break;
			case SpellModOp.PointsIndex0: // check if spell has any effect at that index
			case SpellModOp.Points:
				if (spellInfo.Effects.Count <= 0)
					return false;

				break;
			case SpellModOp.PointsIndex1: // check if spell has any effect at that index
				if (spellInfo.Effects.Count <= 1)
					return false;

				break;
			case SpellModOp.PointsIndex2: // check if spell has any effect at that index
				if (spellInfo.Effects.Count <= 2)
					return false;

				break;
			case SpellModOp.PointsIndex3: // check if spell has any effect at that index
				if (spellInfo.Effects.Count <= 3)
					return false;

				break;
			case SpellModOp.PointsIndex4: // check if spell has any effect at that index
				if (spellInfo.Effects.Count <= 4)
					return false;

				break;
			default:
				break;
		}

		return spellInfo.IsAffectedBySpellMod(mod);
	}

	void SendSpellModifiers()
	{
		SetSpellModifier flatMods = new(ServerOpcodes.SetFlatSpellModifier);
		SetSpellModifier pctMods = new(ServerOpcodes.SetPctSpellModifier);

		for (var i = 0; i < (int)SpellModOp.Max; ++i)
		{
			SpellModifierInfo flatMod = new();
			SpellModifierInfo pctMod = new();
			flatMod.ModIndex = pctMod.ModIndex = (byte)i;

			for (byte j = 0; j < 128; ++j)
			{
				FlagArray128 mask = new();
				mask[j / 32] = 1u << (j % 32);

				SpellModifierData flatData;
				SpellModifierData pctData;

				flatData.ClassIndex = j;
				flatData.ModifierValue = 0.0f;
				pctData.ClassIndex = j;
				pctData.ModifierValue = 1.0f;

				foreach (SpellModifierByClassMask mod in _spellModifiers[i][(int)SpellModType.Flat])
					if (mod.Mask & mask)
						flatData.ModifierValue += mod.Value;

				foreach (SpellModifierByClassMask mod in _spellModifiers[i][(int)SpellModType.Pct])
					if (mod.Mask & mask)
						pctData.ModifierValue *= 1.0f + MathFunctions.CalculatePct(1.0f, mod.Value);

				flatMod.ModifierData.Add(flatData);
				pctMod.ModifierData.Add(pctData);
			}

			flatMod.ModifierData.RemoveAll(mod => MathFunctions.fuzzyEq(mod.ModifierValue, 0.0f));

			pctMod.ModifierData.RemoveAll(mod => MathFunctions.fuzzyEq(mod.ModifierValue, 1.0f));

			flatMods.Modifiers.Add(flatMod);
			pctMods.Modifiers.Add(pctMod);
		}

		if (!flatMods.Modifiers.Empty())
			SendPacket(flatMods);

		if (!pctMods.Modifiers.Empty())
			SendPacket(pctMods);
	}

	void SendSupercededSpell(uint oldSpell, uint newSpell)
	{
		SupercededSpells supercededSpells = new();
		LearnedSpellInfo learnedSpellInfo = new();
		learnedSpellInfo.SpellID = newSpell;
		learnedSpellInfo.Superceded = (int)oldSpell;
		supercededSpells.ClientLearnedSpellData.Add(learnedSpellInfo);
		SendPacket(supercededSpells);
	}

	void UpdateItemSetAuras(bool formChange = false)
	{
		// item set bonuses not dependent from item broken state
		for (var setindex = 0; setindex < ItemSetEff.Count; ++setindex)
		{
			var eff = ItemSetEff[setindex];

			if (eff == null)
				continue;

			foreach (var itemSetSpell in eff.SetBonuses)
			{
				var spellInfo = Global.SpellMgr.GetSpellInfo(itemSetSpell.SpellID, Difficulty.None);

				if (itemSetSpell.ChrSpecID != 0 && itemSetSpell.ChrSpecID != GetPrimarySpecialization())
				{
					ApplyEquipSpell(spellInfo, null, false, false); // item set aura is not for current spec
				}
				else
				{
					ApplyEquipSpell(spellInfo, null, false, formChange); // remove spells that not fit to form - removal is skipped if shapeshift condition is satisfied
					ApplyEquipSpell(spellInfo, null, true, formChange);  // add spells that fit form but not active
				}
			}
		}
	}

	float GetWeaponProcChance()
	{
		// normalized proc chance for weapon attack speed
		// (odd formula...)
		if (IsAttackReady(WeaponAttackType.BaseAttack))
			return (GetBaseAttackTime(WeaponAttackType.BaseAttack) * 1.8f / 1000.0f);
		else if (HaveOffhandWeapon() && IsAttackReady(WeaponAttackType.OffAttack))
			return (GetBaseAttackTime(WeaponAttackType.OffAttack) * 1.6f / 1000.0f);

		return 0;
	}
}
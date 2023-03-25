// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Chrono;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.C;
using Forged.MapServer.DataStorage.Structs.P;
using Forged.MapServer.DataStorage.Structs.T;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Networking.Packets.Talent;
using Forged.MapServer.Networking.Packets.Trait;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Forged.MapServer.Server;
using Forged.MapServer.Spells;
using Framework.Constants;
using Framework.Database;

namespace Forged.MapServer.Entities.Players;

public partial class Player
{
	public void InitTalentForLevel()
	{
		var level = Level;

		// talents base at level diff (talents = level - 9 but some can be used already)
		if (level < PlayerConst.MinSpecializationLevel)
			ResetTalentSpecialization();

		var talentTiers = Global.DB2Mgr.GetNumTalentsAtLevel(level, Class);

		if (level < 10)
		{
			// Remove all talent points
			ResetTalents(true);
		}
		else
		{
			if (!Session.HasPermission(RBACPermissions.SkipCheckMoreTalentsThanAllowed))
				for (var t = talentTiers; t < PlayerConst.MaxTalentTiers; ++t)
					for (uint c = 0; c < PlayerConst.MaxTalentColumns; ++c)
						foreach (var talent in Global.DB2Mgr.GetTalentsByPosition(Class, t, c))
							RemoveTalent(talent);
		}

		SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.MaxTalentTiers), talentTiers);

		if (!Session.HasPermission(RBACPermissions.SkipCheckMoreTalentsThanAllowed))
			for (byte spec = 0; spec < PlayerConst.MaxSpecializations; ++spec)
			{
				for (var slot = Global.DB2Mgr.GetPvpTalentNumSlotsAtLevel(level, Class); slot < PlayerConst.MaxPvpTalentSlots; ++slot)
				{
					var pvpTalent = CliDB.PvpTalentStorage.LookupByKey(GetPvpTalentMap(spec)[slot]);

					if (pvpTalent != null)
						RemovePvpTalent(pvpTalent, spec);
				}
			}

		if (!Session.PlayerLoading)
			SendTalentsInfoData(); // update at client
	}

	public bool AddTalent(TalentRecord talent, byte spec, bool learning)
	{
		var spellInfo = Global.SpellMgr.GetSpellInfo(talent.SpellID, Difficulty.None);

		if (spellInfo == null)
		{
			Log.Logger.Error("Player.AddTalent: Spell (ID: {0}) does not exist.", talent.SpellID);

			return false;
		}

		if (!Global.SpellMgr.IsSpellValid(spellInfo, this, false))
		{
			Log.Logger.Error("Player.AddTalent: Spell (ID: {0}) is invalid", talent.SpellID);

			return false;
		}

		if (GetTalentMap(spec).ContainsKey(talent.Id))
			GetTalentMap(spec)[talent.Id] = PlayerSpellState.Unchanged;
		else
			GetTalentMap(spec)[talent.Id] = learning ? PlayerSpellState.New : PlayerSpellState.Unchanged;

		if (spec == GetActiveTalentGroup())
		{
			LearnSpell(talent.SpellID, true);

			if (talent.OverridesSpellID != 0)
				AddOverrideSpell(talent.OverridesSpellID, talent.SpellID);
		}

		if (learning)
			RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags2.ChangeTalent);

		return true;
	}

	public void RemoveTalent(TalentRecord talent)
	{
		var spellInfo = Global.SpellMgr.GetSpellInfo(talent.SpellID, Difficulty.None);

		if (spellInfo == null)
			return;

		RemoveSpell(talent.SpellID, true);

		// search for spells that the talent teaches and unlearn them
		foreach (var spellEffectInfo in spellInfo.Effects)
			if (spellEffectInfo.IsEffect(SpellEffectName.LearnSpell) && spellEffectInfo.TriggerSpell > 0)
				RemoveSpell(spellEffectInfo.TriggerSpell, true);

		if (talent.OverridesSpellID != 0)
			RemoveOverrideSpell(talent.OverridesSpellID, talent.SpellID);

		var talentMap = GetTalentMap(GetActiveTalentGroup());

		// if this talent rank can be found in the PlayerTalentMap, mark the talent as removed so it gets deleted
		if (talentMap.ContainsKey(talent.Id))
			talentMap[talent.Id] = PlayerSpellState.Removed;
	}

	public TalentLearnResult LearnTalent(uint talentId, ref int spellOnCooldown)
	{
		if (IsInCombat)
			return TalentLearnResult.FailedAffectingCombat;

		if (IsDead)
			return TalentLearnResult.FailedCantDoThatRightNow;

		if (GetPrimarySpecialization() == 0)
			return TalentLearnResult.FailedNoPrimaryTreeSelected;

		var talentInfo = CliDB.TalentStorage.LookupByKey(talentId);

		if (talentInfo == null)
			return TalentLearnResult.FailedUnknown;

		if (talentInfo.SpecID != 0 && talentInfo.SpecID != GetPrimarySpecialization())
			return TalentLearnResult.FailedUnknown;

		// prevent learn talent for different class (cheating)
		if (talentInfo.ClassID != (byte)Class)
			return TalentLearnResult.FailedUnknown;

		// check if we have enough talent points
		if (talentInfo.TierID >= ActivePlayerData.MaxTalentTiers)
			return TalentLearnResult.FailedUnknown;

		// TODO: prevent changing talents that are on cooldown

		// Check if there is a different talent for us to learn in selected slot
		// Example situation:
		// Warrior talent row 2 slot 0
		// Talent.dbc has an entry for each specialization
		// but only 2 out of 3 have SpecID != 0
		// We need to make sure that if player is in one of these defined specs he will not learn the other choice
		TalentRecord bestSlotMatch = null;

		foreach (var talent in Global.DB2Mgr.GetTalentsByPosition(Class, talentInfo.TierID, talentInfo.ColumnIndex))
			if (talent.SpecID == 0)
			{
				bestSlotMatch = talent;
			}

			else if (talent.SpecID == GetPrimarySpecialization())
			{
				bestSlotMatch = talent;

				break;
			}

		if (talentInfo != bestSlotMatch)
			return TalentLearnResult.FailedUnknown;

		// Check if player doesn't have any talent in current tier
		for (uint c = 0; c < PlayerConst.MaxTalentColumns; ++c)
			foreach (var talent in Global.DB2Mgr.GetTalentsByPosition(Class, talentInfo.TierID, c))
			{
				if (talent.SpecID != 0 && talent.SpecID != GetPrimarySpecialization())
					continue;

				if (!HasTalent(talent.Id, GetActiveTalentGroup()))
					continue;

				if (!HasPlayerFlag(PlayerFlags.Resting) && HasUnitFlag2(UnitFlags2.AllowChangingTalents))
					return TalentLearnResult.FailedRestArea;

				if (SpellHistory.HasCooldown(talent.SpellID))
				{
					spellOnCooldown = (int)talent.SpellID;

					return TalentLearnResult.FailedCantRemoveTalent;
				}

				RemoveTalent(talent);
			}

		// spell not set in talent.dbc
		var spellid = talentInfo.SpellID;

		if (spellid == 0)
		{
			Log.Logger.Error("Player.LearnTalent: Talent.dbc has no spellInfo for talent: {0} (spell id = 0)", talentId);

			return TalentLearnResult.FailedUnknown;
		}

		// already known
		if (HasTalent(talentId, GetActiveTalentGroup()) || HasSpell(spellid))
			return TalentLearnResult.FailedUnknown;

		if (!AddTalent(talentInfo, GetActiveTalentGroup(), true))
			return TalentLearnResult.FailedUnknown;

		Log.Logger.Debug("Player.LearnTalent: TalentID: {0} Spell: {1} Group: {2}", talentId, spellid, GetActiveTalentGroup());

		return TalentLearnResult.LearnOk;
	}

	public void ResetTalentSpecialization()
	{
		// Reset only talents that have different spells for each spec
		var class_ = Class;

		for (uint t = 0; t < PlayerConst.MaxTalentTiers; ++t)
		{
			for (uint c = 0; c < PlayerConst.MaxTalentColumns; ++c)
				if (Global.DB2Mgr.GetTalentsByPosition(class_, t, c).Count > 1)
					foreach (var talent in Global.DB2Mgr.GetTalentsByPosition(class_, t, c))
						RemoveTalent(talent);
		}

		ResetPvpTalents();
		RemoveSpecializationSpells();

		var defaultSpec = Global.DB2Mgr.GetDefaultChrSpecializationForClass(Class);
		SetPrimarySpecialization(defaultSpec.Id);
		SetActiveTalentGroup(defaultSpec.OrderIndex);

		LearnSpecializationSpells();

		SendTalentsInfoData();
		UpdateItemSetAuras(false);
	}

	public uint GetPrimarySpecialization()
	{
		return PlayerData.CurrentSpecID;
	}

	public byte GetActiveTalentGroup()
	{
		return _specializationInfo.ActiveGroup;
	}

	// Loot Spec
	public void SetLootSpecId(uint id)
	{
		SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.LootSpecID), (ushort)id);
	}

	public uint GetLootSpecId()
	{
		return ActivePlayerData.LootSpecID;
	}

	public uint GetDefaultSpecId()
	{
		return Global.DB2Mgr.GetDefaultChrSpecializationForClass(Class).Id;
	}

	public void ActivateTalentGroup(ChrSpecializationRecord spec)
	{
		if (GetActiveTalentGroup() == spec.OrderIndex)
			return;

		if (IsNonMeleeSpellCast(false))
			InterruptNonMeleeSpells(false);

		SQLTransaction trans = new();
		_SaveActions(trans);
		DB.Characters.CommitTransaction(trans);

		// TO-DO: We need more research to know what happens with warlock's reagent
		var pet = CurrentPet;

		if (pet)
			RemovePet(pet, PetSaveMode.NotInSlot);

		ClearAllReactives();
		UnsummonAllTotems();
		ExitVehicle();
		RemoveAllControlled();

		RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags2.ChangeSpec);

		// remove single target auras at other targets
		var scAuras = SingleCastAuras.ToList();

		foreach (var aura in scAuras)
			if (aura.OwnerAsUnit != this)
				aura.Remove();

		// Let client clear his current Actions
		SendActionButtons(2);

		foreach (var talentInfo in CliDB.TalentStorage.Values)
		{
			// unlearn only talents for character class
			// some spell learned by one class as normal spells or know at creation but another class learn it as talent,
			// to prevent unexpected lost normal learned spell skip another class talents
			if (talentInfo.ClassID != (int)Class)
				continue;

			if (talentInfo.SpellID == 0)
				continue;

			var spellInfo = Global.SpellMgr.GetSpellInfo(talentInfo.SpellID, Difficulty.None);

			if (spellInfo == null)
				continue;

			RemoveSpell(talentInfo.SpellID, true);

			// search for spells that the talent teaches and unlearn them
			foreach (var spellEffectInfo in spellInfo.Effects)
				if (spellEffectInfo.IsEffect(SpellEffectName.LearnSpell) && spellEffectInfo.TriggerSpell > 0)
					RemoveSpell(spellEffectInfo.TriggerSpell, true);

			if (talentInfo.OverridesSpellID != 0)
				RemoveOverrideSpell(talentInfo.OverridesSpellID, talentInfo.SpellID);
		}

		foreach (var talentInfo in CliDB.PvpTalentStorage.Values)
		{
			var spellInfo = Global.SpellMgr.GetSpellInfo(talentInfo.SpellID, Difficulty.None);

			if (spellInfo == null)
				continue;

			RemoveSpell(talentInfo.SpellID, true);

			// search for spells that the talent teaches and unlearn them
			foreach (var spellEffectInfo in spellInfo.Effects)
				if (spellEffectInfo.IsEffect(SpellEffectName.LearnSpell) && spellEffectInfo.TriggerSpell > 0)
					RemoveSpell(spellEffectInfo.TriggerSpell, true);

			if (talentInfo.OverridesSpellID != 0)
				RemoveOverrideSpell(talentInfo.OverridesSpellID, talentInfo.SpellID);
		}

		ApplyTraitConfig((int)(uint)ActivePlayerData.ActiveCombatTraitConfigID, false);

		// Remove spec specific spells
		RemoveSpecializationSpells();

		foreach (var glyphId in GetGlyphs(GetActiveTalentGroup()))
			RemoveAura(CliDB.GlyphPropertiesStorage.LookupByKey(glyphId).SpellID);

		SetActiveTalentGroup(spec.OrderIndex);
		SetPrimarySpecialization(spec.Id);
		var specTraitConfigIndex = ActivePlayerData.TraitConfigs.FindIndexIf(traitConfig => { return (TraitConfigType)(int)traitConfig.Type == TraitConfigType.Combat && traitConfig.ChrSpecializationID == spec.Id && ((TraitCombatConfigFlags)(int)traitConfig.CombatConfigFlags & TraitCombatConfigFlags.ActiveForSpec) != TraitCombatConfigFlags.None; });

		if (specTraitConfigIndex >= 0)
			SetActiveCombatTraitConfigID(ActivePlayerData.TraitConfigs[specTraitConfigIndex].ID);
		else
			SetActiveCombatTraitConfigID(0);

		foreach (var talentInfo in CliDB.TalentStorage.Values)
		{
			// learn only talents for character class
			if (talentInfo.ClassID != (int)Class)
				continue;

			if (talentInfo.SpellID == 0)
				continue;

			if (HasTalent(talentInfo.Id, GetActiveTalentGroup()))
			{
				LearnSpell(talentInfo.SpellID, true); // add the talent to the PlayerSpellMap

				if (talentInfo.OverridesSpellID != 0)
					AddOverrideSpell(talentInfo.OverridesSpellID, talentInfo.SpellID);
			}
		}

		for (byte slot = 0; slot < PlayerConst.MaxPvpTalentSlots; ++slot)
		{
			var talentInfo = CliDB.PvpTalentStorage.LookupByKey(GetPvpTalentMap(GetActiveTalentGroup())[slot]);

			if (talentInfo == null)
				continue;

			if (talentInfo.SpellID == 0)
				continue;

			AddPvpTalent(talentInfo, GetActiveTalentGroup(), slot);
		}

		LearnSpecializationSpells();

		if (CanUseMastery())
			for (uint i = 0; i < PlayerConst.MaxMasterySpells; ++i)
			{
				var mastery = spec.MasterySpellID[i];

				if (mastery != 0)
					LearnSpell(mastery, true);
			}

		ApplyTraitConfig((int)(uint)ActivePlayerData.ActiveCombatTraitConfigID, true);

		InitTalentForLevel();

		StartLoadingActionButtons();

		UpdateDisplayPower();
		var pw = DisplayPowerType;

		if (pw != PowerType.Mana)
			SetPower(PowerType.Mana, 0); // Mana must be 0 even if it isn't the active power type.

		SetPower(pw, 0);
		UpdateItemSetAuras(false);

		// update visible transmog
		for (var i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
		{
			var equippedItem = GetItemByPos(InventorySlots.Bag0, i);

			if (equippedItem)
				SetVisibleItemSlot(i, equippedItem);
		}

		foreach (var glyphId in GetGlyphs(spec.OrderIndex))
			CastSpell(this, CliDB.GlyphPropertiesStorage.LookupByKey(glyphId).SpellID, true);

		ActiveGlyphs activeGlyphs = new();

		foreach (var glyphId in GetGlyphs(spec.OrderIndex))
		{
			var bindableSpells = Global.DB2Mgr.GetGlyphBindableSpells(glyphId);

			foreach (var bindableSpell in bindableSpells)
				if (HasSpell(bindableSpell) && !_overrideSpells.ContainsKey(bindableSpell))
					activeGlyphs.Glyphs.Add(new GlyphBinding(bindableSpell, (ushort)glyphId));
		}

		activeGlyphs.IsFullUpdate = true;
		SendPacket(activeGlyphs);

		var item = GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);

		if (item != null)
		{
			var azeriteItem = item.AsAzeriteItem;

			if (azeriteItem != null)
			{
				if (azeriteItem.IsEquipped)
				{
					ApplyAllAzeriteEmpoweredItemMods(false);
					ApplyAzeritePowers(azeriteItem, false);
				}

				azeriteItem.SetSelectedAzeriteEssences(spec.Id);

				if (azeriteItem.IsEquipped)
				{
					ApplyAzeritePowers(azeriteItem, true);
					ApplyAllAzeriteEmpoweredItemMods(true);
				}

				azeriteItem.SetState(ItemUpdateState.Changed, this);
			}
		}

		var shapeshiftAuras = GetAuraEffectsByType(AuraType.ModShapeshift);

		foreach (var aurEff in shapeshiftAuras)
		{
			aurEff.HandleShapeshiftBoosts(this, false);
			aurEff.HandleShapeshiftBoosts(this, true);
		}
	}

	public Dictionary<uint, PlayerSpellState> GetTalentMap(uint spec)
	{
		return _specializationInfo.Talents[spec];
	}

	public List<uint> GetGlyphs(byte spec)
	{
		return _specializationInfo.Glyphs[spec];
	}

	public uint GetNextResetTalentsCost()
	{
		// The first time reset costs 1 gold
		if (GetTalentResetCost() < 1 * MoneyConstants.Gold)
		{
			return 1 * MoneyConstants.Gold;
		}
		// then 5 gold
		else if (GetTalentResetCost() < 5 * MoneyConstants.Gold)
		{
			return 5 * MoneyConstants.Gold;
		}
		// After that it increases in increments of 5 gold
		else if (GetTalentResetCost() < 10 * MoneyConstants.Gold)
		{
			return 10 * MoneyConstants.Gold;
		}
		else
		{
			var months = (ulong)(GameTime.GetGameTime() - GetTalentResetTime()) / Time.Month;

			if (months > 0)
			{
				// This cost will be reduced by a rate of 5 gold per month
				var new_cost = (uint)(GetTalentResetCost() - 5 * MoneyConstants.Gold * months);

				// to a minimum of 10 gold.
				return new_cost < 10 * MoneyConstants.Gold ? 10 * MoneyConstants.Gold : new_cost;
			}
			else
			{
				// After that it increases in increments of 5 gold
				var new_cost = GetTalentResetCost() + 5 * MoneyConstants.Gold;

				// until it hits a cap of 50 gold.
				if (new_cost > 50 * MoneyConstants.Gold)
					new_cost = 50 * MoneyConstants.Gold;

				return new_cost;
			}
		}
	}

	public bool ResetTalents(bool noCost = false)
	{
		Global.ScriptMgr.ForEach<IPlayerOnTalentsReset>(p => p.OnTalentsReset(this, noCost));

		// not need after this call
		if (HasAtLoginFlag(AtLoginFlags.ResetTalents))
			RemoveAtLoginFlag(AtLoginFlags.ResetTalents, true);

		uint cost = 0;

		if (!noCost && !GetDefaultValue("NoResetTalentsCost", false))
		{
			cost = GetNextResetTalentsCost();

			if (!HasEnoughMoney(cost))
			{
				SendBuyError(BuyResult.NotEnoughtMoney, null, 0);

				return false;
			}
		}

		RemovePet(null, PetSaveMode.NotInSlot, true);

		foreach (var talentInfo in CliDB.TalentStorage.Values)
		{
			// unlearn only talents for character class
			// some spell learned by one class as normal spells or know at creation but another class learn it as talent,
			// to prevent unexpected lost normal learned spell skip another class talents
			if (talentInfo.ClassID != (uint)Class)
				continue;

			// skip non-existant talent ranks
			if (talentInfo.SpellID == 0)
				continue;

			RemoveTalent(talentInfo);
		}

		SQLTransaction trans = new();
		_SaveTalents(trans);
		_SaveSpells(trans);
		DB.Characters.CommitTransaction(trans);

		if (!noCost)
		{
			ModifyMoney(-cost);
			UpdateCriteria(CriteriaType.MoneySpentOnRespecs, cost);
			UpdateCriteria(CriteriaType.TotalRespecs, 1);

			SetTalentResetCost(cost);
			SetTalentResetTime(GameTime.GetGameTime());
		}

		return true;
	}

	public void SendTalentsInfoData()
	{
		UpdateTalentData packet = new()
		{
			Info =
			{
				PrimarySpecialization = GetPrimarySpecialization()
			}
		};

		for (byte i = 0; i < PlayerConst.MaxSpecializations; ++i)
		{
			var spec = Global.DB2Mgr.GetChrSpecializationByIndex(Class, i);

			if (spec == null)
				continue;

			var talents = GetTalentMap(i);
			var pvpTalents = GetPvpTalentMap(i);

			UpdateTalentData.TalentGroupInfo groupInfoPkt = new()
			{
				SpecID = spec.Id
			};

			foreach (var pair in talents)
			{
				if (pair.Value == PlayerSpellState.Removed)
					continue;

				var talentInfo = CliDB.TalentStorage.LookupByKey(pair.Key);

				if (talentInfo == null)
				{
					Log.Logger.Error("Player {0} has unknown talent id: {1}", GetName(), pair.Key);

					continue;
				}

				var spellEntry = Global.SpellMgr.GetSpellInfo(talentInfo.SpellID, Difficulty.None);

				if (spellEntry == null)
				{
					Log.Logger.Error("Player {0} has unknown talent spell: {1}", GetName(), talentInfo.SpellID);

					continue;
				}

				groupInfoPkt.TalentIDs.Add((ushort)pair.Key);
			}

			for (byte slot = 0; slot < PlayerConst.MaxPvpTalentSlots; ++slot)
			{
				if (pvpTalents[slot] == 0)
					continue;

				var talentInfo = CliDB.PvpTalentStorage.LookupByKey(pvpTalents[slot]);

				if (talentInfo == null)
				{
					Log.Logger.Error($"Player.SendTalentsInfoData: Player '{GetName()}' ({GUID}) has unknown pvp talent id: {pvpTalents[slot]}");

					continue;
				}

				var spellEntry = Global.SpellMgr.GetSpellInfo(talentInfo.SpellID, Difficulty.None);

				if (spellEntry == null)
				{
					Log.Logger.Error($"Player.SendTalentsInfoData: Player '{GetName()}' ({GUID}) has unknown pvp talent spell: {talentInfo.SpellID}");

					continue;
				}

				PvPTalent pvpTalent = new()
				{
					PvPTalentID = (ushort)pvpTalents[slot],
					Slot = slot
				};

				groupInfoPkt.PvPTalents.Add(pvpTalent);
			}

			if (i == GetActiveTalentGroup())
				packet.Info.ActiveGroup = (byte)packet.Info.TalentGroups.Count;

			if (!groupInfoPkt.TalentIDs.Empty() || !groupInfoPkt.PvPTalents.Empty() || i == GetActiveTalentGroup())
				packet.Info.TalentGroups.Add(groupInfoPkt);
		}

		SendPacket(packet);
	}

	public void SendRespecWipeConfirm(ObjectGuid guid, uint cost, SpecResetType respecType)
	{
		RespecWipeConfirm respecWipeConfirm = new()
		{
			RespecMaster = guid,
			Cost = cost,
			RespecType = respecType
		};

		SendPacket(respecWipeConfirm);
	}

	public TalentLearnResult LearnPvpTalent(uint talentID, byte slot, ref uint spellOnCooldown)
	{
		if (slot >= PlayerConst.MaxPvpTalentSlots)
			return TalentLearnResult.FailedUnknown;

		if (IsInCombat)
			return TalentLearnResult.FailedAffectingCombat;

		if (IsDead)
			return TalentLearnResult.FailedCantDoThatRightNow;

		var talentInfo = CliDB.PvpTalentStorage.LookupByKey(talentID);

		if (talentInfo == null)
			return TalentLearnResult.FailedUnknown;

		if (talentInfo.SpecID != GetPrimarySpecialization())
			return TalentLearnResult.FailedUnknown;

		if (talentInfo.LevelRequired > Level)
			return TalentLearnResult.FailedUnknown;

		if (Global.DB2Mgr.GetRequiredLevelForPvpTalentSlot(slot, Class) > Level)
			return TalentLearnResult.FailedUnknown;

		var talentCategory = CliDB.PvpTalentCategoryStorage.LookupByKey(talentInfo.PvpTalentCategoryID);

		if (talentCategory != null)
			if (!Convert.ToBoolean(talentCategory.TalentSlotMask & (1 << slot)))
				return TalentLearnResult.FailedUnknown;

		// Check if player doesn't have this talent in other slot
		if (HasPvpTalent(talentID, GetActiveTalentGroup()))
			return TalentLearnResult.FailedUnknown;

		var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(talentInfo.PlayerConditionID);

		if (playerCondition != null)
			if (!ConditionManager.IsPlayerMeetingCondition(this, playerCondition))
				return TalentLearnResult.FailedCantDoThatRightNow;

		var talent = CliDB.PvpTalentStorage.LookupByKey(GetPvpTalentMap(GetActiveTalentGroup())[slot]);

		if (talent != null)
		{
			if (!HasPlayerFlag(PlayerFlags.Resting) && !HasUnitFlag2(UnitFlags2.AllowChangingTalents))
				return TalentLearnResult.FailedRestArea;

			if (SpellHistory.HasCooldown(talent.SpellID))
			{
				spellOnCooldown = talent.SpellID;

				return TalentLearnResult.FailedCantRemoveTalent;
			}

			RemovePvpTalent(talent, GetActiveTalentGroup());
		}

		if (!AddPvpTalent(talentInfo, GetActiveTalentGroup(), slot))
			return TalentLearnResult.FailedUnknown;

		return TalentLearnResult.LearnOk;
	}

	public void TogglePvpTalents(bool enable)
	{
		var pvpTalents = GetPvpTalentMap(GetActiveTalentGroup());

		foreach (var pvpTalentId in pvpTalents)
		{
			var pvpTalentInfo = CliDB.PvpTalentStorage.LookupByKey(pvpTalentId);

			if (pvpTalentInfo != null)
			{
				if (enable)
				{
					LearnSpell(pvpTalentInfo.SpellID, false);

					if (pvpTalentInfo.OverridesSpellID != 0)
						AddOverrideSpell(pvpTalentInfo.OverridesSpellID, pvpTalentInfo.SpellID);
				}
				else
				{
					if (pvpTalentInfo.OverridesSpellID != 0)
						RemoveOverrideSpell(pvpTalentInfo.OverridesSpellID, pvpTalentInfo.SpellID);

					RemoveSpell(pvpTalentInfo.SpellID, true);
				}
			}
		}
	}

	//Traits
	public void CreateTraitConfig(TraitConfigPacket traitConfig)
	{
		var configId = TraitMgr.GenerateNewTraitConfigId();

		bool hasConfigId(int id)
		{
			return ActivePlayerData.TraitConfigs.FindIndexIf(config => config.ID == id) >= 0;
		}

		while (hasConfigId(configId))
			configId = TraitMgr.GenerateNewTraitConfigId();

		traitConfig.ID = configId;

		var traitConfigIndex = ActivePlayerData.TraitConfigs.Size();
		AddTraitConfig(traitConfig);

		foreach (var grantedEntry in TraitMgr.GetGrantedTraitEntriesForConfig(traitConfig, this))
		{
			var entryIndex = traitConfig.Entries.LookupByKey(grantedEntry.TraitNodeID)?.LookupByKey(grantedEntry.TraitNodeEntryID);

			if (entryIndex == null)
			{
				TraitConfig value = Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.TraitConfigs, traitConfigIndex);
				AddDynamicUpdateFieldValue(value.ModifyValue(value.Entries), grantedEntry);
			}
		}

		_traitConfigStates[(int)configId] = PlayerSpellState.Changed;
	}

	public TraitConfig GetTraitConfig(int configId)
	{
		var index = ActivePlayerData.TraitConfigs.FindIndexIf(config => config.ID == configId);

		if (index < 0)
			return null;

		return ActivePlayerData.TraitConfigs[index];
	}

	public void UpdateTraitConfig(TraitConfigPacket newConfig, int savedConfigId, bool withCastTime)
	{
		var index = ActivePlayerData.TraitConfigs.FindIndexIf(config => config.ID == newConfig.ID);

		if (index < 0)
			return;

		if (withCastTime)
		{
			CastSpell(this, TraitMgr.COMMIT_COMBAT_TRAIT_CONFIG_CHANGES_SPELL_ID, new CastSpellExtraArgs(SpellValueMod.BasePoint0, savedConfigId).SetCustomArg(newConfig));

			return;
		}

		var isActiveConfig = true;
		var loadActionButtons = false;

		switch ((TraitConfigType)(int)ActivePlayerData.TraitConfigs[index].Type)
		{
			case TraitConfigType.Combat:
				isActiveConfig = newConfig.ID == ActivePlayerData.ActiveCombatTraitConfigID;
				loadActionButtons = ActivePlayerData.TraitConfigs[index].LocalIdentifier != newConfig.LocalIdentifier;

				break;
			case TraitConfigType.Profession:
				isActiveConfig = HasSkill((uint)(int)ActivePlayerData.TraitConfigs[index].SkillLineID);

				break;
			default:
				break;
		}

		var finalizeTraitConfigUpdate = () =>
		{
			TraitConfig newTraitConfig = Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.TraitConfigs, index);
			SetUpdateFieldValue(newTraitConfig.ModifyValue(newTraitConfig.LocalIdentifier), newConfig.LocalIdentifier);

			ApplyTraitEntryChanges(newConfig.ID, newConfig, isActiveConfig, true);

			if (savedConfigId != 0)
				ApplyTraitEntryChanges(savedConfigId, newConfig, false, false);

			if (((TraitCombatConfigFlags)(int)newConfig.CombatConfigFlags).HasFlag(TraitCombatConfigFlags.StarterBuild))
				SetTraitConfigUseStarterBuild(newConfig.ID, true);
		};

		if (loadActionButtons)
		{
			var trans = new SQLTransaction();
			_SaveActions(trans);
			DB.Characters.CommitTransaction(trans);

			StartLoadingActionButtons(finalizeTraitConfigUpdate);
		}
		else
		{
			finalizeTraitConfigUpdate();
		}
	}

	public void RenameTraitConfig(int editedConfigId, string newName)
	{
		var editedIndex = ActivePlayerData.TraitConfigs.FindIndexIf(traitConfig => { return traitConfig.ID == editedConfigId && (TraitConfigType)(int)traitConfig.Type == TraitConfigType.Combat && ((TraitCombatConfigFlags)(int)traitConfig.CombatConfigFlags & TraitCombatConfigFlags.ActiveForSpec) == TraitCombatConfigFlags.None; });

		if (editedIndex < 0)
			return;

		TraitConfig traitConfig = Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.TraitConfigs, editedIndex);
		SetUpdateFieldValue(traitConfig.ModifyValue(traitConfig.Name), newName);

		_traitConfigStates[editedConfigId] = PlayerSpellState.Changed;
	}

	public void DeleteTraitConfig(int deletedConfigId)
	{
		var deletedIndex = ActivePlayerData.TraitConfigs.FindIndexIf(traitConfig => { return traitConfig.ID == deletedConfigId && (TraitConfigType)(int)traitConfig.Type == TraitConfigType.Combat && ((TraitCombatConfigFlags)(int)traitConfig.CombatConfigFlags & TraitCombatConfigFlags.ActiveForSpec) == TraitCombatConfigFlags.None; });

		if (deletedIndex < 0)
			return;

		RemoveDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData)
											.ModifyValue(ActivePlayerData.TraitConfigs),
									deletedIndex);

		_traitConfigStates[deletedConfigId] = PlayerSpellState.Removed;
	}

	public void SetTraitConfigUseStarterBuild(int traitConfigId, bool useStarterBuild)
	{
		var configIndex = ActivePlayerData.TraitConfigs.FindIndexIf(traitConfig => { return traitConfig.ID == traitConfigId && (TraitConfigType)(int)traitConfig.Type == TraitConfigType.Combat && ((TraitCombatConfigFlags)(int)traitConfig.CombatConfigFlags & TraitCombatConfigFlags.ActiveForSpec) != TraitCombatConfigFlags.None; });

		if (configIndex < 0)
			return;

		var currentlyUsesStarterBuild = ((TraitCombatConfigFlags)(int)ActivePlayerData.TraitConfigs[configIndex].CombatConfigFlags).HasFlag(TraitCombatConfigFlags.StarterBuild);

		if (currentlyUsesStarterBuild == useStarterBuild)
			return;

		if (useStarterBuild)
		{
			TraitConfig traitConfig = Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.TraitConfigs, configIndex);
			SetUpdateFieldFlagValue(traitConfig.ModifyValue(traitConfig.CombatConfigFlags), (int)TraitCombatConfigFlags.StarterBuild);
		}
		else
		{
			TraitConfig traitConfig = Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.TraitConfigs, configIndex);
			RemoveUpdateFieldFlagValue(traitConfig.ModifyValue(traitConfig.CombatConfigFlags), (int)TraitCombatConfigFlags.StarterBuild);
		}

		_traitConfigStates[(int)traitConfigId] = PlayerSpellState.Changed;
	}

	public void SetTraitConfigUseSharedActionBars(int traitConfigId, bool usesSharedActionBars, bool isLastSelectedSavedConfig)
	{
		var configIndex = ActivePlayerData.TraitConfigs.FindIndexIf(traitConfig => { return traitConfig.ID == traitConfigId && (TraitConfigType)(int)traitConfig.Type == TraitConfigType.Combat && ((TraitCombatConfigFlags)(int)traitConfig.CombatConfigFlags & TraitCombatConfigFlags.ActiveForSpec) == TraitCombatConfigFlags.None; });

		if (configIndex < 0)
			return;

		var currentlyUsesSharedActionBars = ((TraitCombatConfigFlags)(int)ActivePlayerData.TraitConfigs[configIndex].CombatConfigFlags).HasFlag(TraitCombatConfigFlags.SharedActionBars);

		if (currentlyUsesSharedActionBars == usesSharedActionBars)
			return;

		TraitConfig traitConfig = Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.TraitConfigs, configIndex);

		if (usesSharedActionBars)
		{
			SetUpdateFieldFlagValue(traitConfig.ModifyValue(traitConfig.CombatConfigFlags), (int)TraitCombatConfigFlags.SharedActionBars);

			var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_ACTION_BY_TRAIT_CONFIG);
			stmt.AddValue(0, GUID.Counter);
			stmt.AddValue(1, traitConfigId);
			DB.Characters.Execute(stmt);

			if (isLastSelectedSavedConfig)
				StartLoadingActionButtons(); // load action buttons that were saved in shared mode
		}
		else
		{
			RemoveUpdateFieldFlagValue(traitConfig.ModifyValue(traitConfig.CombatConfigFlags), (int)TraitCombatConfigFlags.SharedActionBars);

			// trigger a save with traitConfigId
			foreach (var (_, button) in _actionButtons)
				if (button.UState != ActionButtonUpdateState.Deleted)
					button.UState = ActionButtonUpdateState.New;
		}

		_traitConfigStates[traitConfigId] = PlayerSpellState.Changed;
	}

	bool HasTalent(uint talentId, byte group)
	{
		return GetTalentMap(group).ContainsKey(talentId) && GetTalentMap(group)[talentId] != PlayerSpellState.Removed;
	}

	uint GetTalentResetCost()
	{
		return _specializationInfo.ResetTalentsCost;
	}

	void SetTalentResetCost(uint cost)
	{
		_specializationInfo.ResetTalentsCost = cost;
	}

	long GetTalentResetTime()
	{
		return _specializationInfo.ResetTalentsTime;
	}

	void SetTalentResetTime(long time_)
	{
		_specializationInfo.ResetTalentsTime = time_;
	}

	void SetPrimarySpecialization(uint spec)
	{
		SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.CurrentSpecID), spec);
	}

	void SetActiveTalentGroup(byte group)
	{
		_specializationInfo.ActiveGroup = group;
	}

	void StartLoadingActionButtons(Action callback = null)
	{
		uint traitConfigId = 0;

		var traitConfig = GetTraitConfig((int)(uint)ActivePlayerData.ActiveCombatTraitConfigID);

		if (traitConfig != null)
		{
			var usedSavedTraitConfigIndex = ActivePlayerData.TraitConfigs.FindIndexIf(savedConfig => { return (TraitConfigType)(int)savedConfig.Type == TraitConfigType.Combat && ((TraitCombatConfigFlags)(int)savedConfig.CombatConfigFlags & TraitCombatConfigFlags.ActiveForSpec) == TraitCombatConfigFlags.None && ((TraitCombatConfigFlags)(int)savedConfig.CombatConfigFlags & TraitCombatConfigFlags.SharedActionBars) == TraitCombatConfigFlags.None && savedConfig.LocalIdentifier == traitConfig.LocalIdentifier; });

			if (usedSavedTraitConfigIndex >= 0)
				traitConfigId = (uint)(int)ActivePlayerData.TraitConfigs[usedSavedTraitConfigIndex].ID;
		}

		// load them asynchronously
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_ACTIONS_SPEC);
		stmt.AddValue(0, GUID.Counter);
		stmt.AddValue(1, GetActiveTalentGroup());
		stmt.AddValue(2, traitConfigId);

		var myGuid = GUID;

		var mySess = Session;

		mySess.QueryProcessor
			.AddCallback(DB.Characters.AsyncQuery(stmt)
							.WithCallback(result =>
							{
								// safe callback, we can't pass this pointer directly
								// in case player logs out before db response (player would be deleted in that case)
								var thisPlayer = mySess.Player;

								if (thisPlayer != null && thisPlayer.GUID == myGuid)
									thisPlayer.LoadActions(result);

								if (callback != null)
									callback();
							}));
	}

	//Pvp
	void ResetPvpTalents()
	{
		for (byte spec = 0; spec < PlayerConst.MaxSpecializations; ++spec)
			foreach (var talentId in GetPvpTalentMap(spec))
			{
				var talentInfo = CliDB.PvpTalentStorage.LookupByKey(talentId);

				if (talentInfo != null)
					RemovePvpTalent(talentInfo, spec);
			}
	}

	bool AddPvpTalent(PvpTalentRecord talent, byte activeTalentGroup, byte slot)
	{
		//ASSERT(talent);
		var spellInfo = Global.SpellMgr.GetSpellInfo(talent.SpellID, Difficulty.None);

		if (spellInfo == null)
		{
			Log.Logger.Error($"Player.AddPvpTalent: Spell (ID: {talent.SpellID}) does not exist.");

			return false;
		}

		if (!Global.SpellMgr.IsSpellValid(spellInfo, this, false))
		{
			Log.Logger.Error($"Player.AddPvpTalent: Spell (ID: {talent.SpellID}) is invalid");

			return false;
		}

		if (activeTalentGroup == GetActiveTalentGroup() && HasAuraType(AuraType.PvpTalents))
		{
			LearnSpell(talent.SpellID, true);

			// Move this to toggle ?
			if (talent.OverridesSpellID != 0)
				AddOverrideSpell(talent.OverridesSpellID, talent.SpellID);
		}

		GetPvpTalentMap(activeTalentGroup)[slot] = talent.Id;

		return true;
	}

	void RemovePvpTalent(PvpTalentRecord talent, byte activeTalentGroup)
	{
		var spellInfo = Global.SpellMgr.GetSpellInfo(talent.SpellID, Difficulty.None);

		if (spellInfo == null)
			return;

		RemoveSpell(talent.SpellID, true);

		// Move this to toggle ?
		if (talent.OverridesSpellID != 0)
			RemoveOverrideSpell(talent.OverridesSpellID, talent.SpellID);

		// if this talent rank can be found in the PlayerTalentMap, mark the talent as removed so it gets deleted
		var talents = GetPvpTalentMap(activeTalentGroup);

		for (var i = 0; i < PlayerConst.MaxPvpTalentSlots; ++i)
			if (talents[i] == talent.Id)
				talents[i] = 0;
	}

	bool HasPvpTalent(uint talentID, byte activeTalentGroup)
	{
		return GetPvpTalentMap(activeTalentGroup).Contains(talentID);
	}

	void AddTraitConfig(TraitConfigPacket traitConfig)
	{
		var setter = new TraitConfig();
		setter.ModifyValue(setter.ID).SetValue(traitConfig.ID);
		setter.ModifyValue(setter.Name).SetValue(traitConfig.Name);
		setter.ModifyValue(setter.Type).SetValue((int)traitConfig.Type);
		setter.ModifyValue(setter.SkillLineID).SetValue((int)traitConfig.SkillLineID);
		setter.ModifyValue(setter.ChrSpecializationID).SetValue(traitConfig.ChrSpecializationID);
		setter.ModifyValue(setter.CombatConfigFlags).SetValue((int)traitConfig.CombatConfigFlags);
		setter.ModifyValue(setter.LocalIdentifier).SetValue(traitConfig.LocalIdentifier);
		setter.ModifyValue(setter.TraitSystemID).SetValue(traitConfig.TraitSystemID);

		AddDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.TraitConfigs), setter);

		foreach (var kvp in traitConfig.Entries.Values)
			foreach (var traitEntry in kvp.Values)
			{
				TraitEntry newEntry = new()
				{
					TraitNodeID = traitEntry.TraitNodeID,
					TraitNodeEntryID = traitEntry.TraitNodeEntryID,
					Rank = traitEntry.Rank,
					GrantedRanks = traitEntry.GrantedRanks
				};

				AddDynamicUpdateFieldValue(setter.ModifyValue(setter.Entries), newEntry);
			}
	}

	void ApplyTraitEntryChanges(int editedConfigId, TraitConfigPacket newConfig, bool applyTraits, bool consumeCurrencies)
	{
		var editedIndex = ActivePlayerData.TraitConfigs.FindIndexIf(config => config.ID == editedConfigId);

		if (editedIndex < 0)
			return;

		var editedConfig = ActivePlayerData.TraitConfigs[editedIndex];

		// remove traits not found in new config
		List<int> entryIndicesToRemove = new();

		for (var i = 0; i < editedConfig.Entries.Size(); ++i)
		{
			var oldEntry = editedConfig.Entries[i];
			var entryItr = newConfig.Entries.LookupByKey(oldEntry.TraitNodeID)?.LookupByKey(oldEntry.TraitNodeEntryID);

			if (entryItr != null)
				continue;

			if (applyTraits)
				ApplyTraitEntry(oldEntry.TraitNodeEntryID, 0, 0, false);

			entryIndicesToRemove.Add(i);
		}

		foreach (var indexToRemove in entryIndicesToRemove)
		{
			TraitConfig traitConfig = Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.TraitConfigs, editedIndex);
			RemoveDynamicUpdateFieldValue(traitConfig.ModifyValue(traitConfig.Entries), indexToRemove);
		}

		List<TraitEntryPacket> costEntries = new();

		// apply new traits
		foreach (var kvp in newConfig.Entries.Values)
			foreach (var newEntry in kvp.Values)
			{
				var oldEntryIndex = editedConfig.Entries.FindIndexIf(ufEntry => ufEntry.TraitNodeID == newEntry.TraitNodeID && ufEntry.TraitNodeEntryID == newEntry.TraitNodeEntryID);

				if (oldEntryIndex < 0)
				{
					if (consumeCurrencies)
						costEntries.Add(newEntry);

					TraitConfig newTraitConfig = Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.TraitConfigs, editedIndex);

					TraitEntry newUfEntry = new()
					{
						TraitNodeID = newEntry.TraitNodeID,
						TraitNodeEntryID = newEntry.TraitNodeEntryID,
						Rank = newEntry.Rank,
						GrantedRanks = newEntry.GrantedRanks
					};

					AddDynamicUpdateFieldValue(newTraitConfig.ModifyValue(newTraitConfig.Entries), newUfEntry);

					if (applyTraits)
						ApplyTraitEntry(newUfEntry.TraitNodeEntryID, newUfEntry.Rank, 0, true);
				}
				else if (newEntry.Rank != editedConfig.Entries[oldEntryIndex].Rank || newEntry.GrantedRanks != editedConfig.Entries[oldEntryIndex].GrantedRanks)
				{
					if (consumeCurrencies && newEntry.Rank > editedConfig.Entries[oldEntryIndex].Rank)
					{
						TraitEntryPacket costEntry = new();
						costEntry.Rank -= editedConfig.Entries[oldEntryIndex].Rank;
						costEntries.Add(newEntry);
					}

					TraitConfig traitConfig = Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.TraitConfigs, editedIndex);
					TraitEntry traitEntry = traitConfig.ModifyValue(traitConfig.Entries, oldEntryIndex);
					traitEntry.Rank = newEntry.Rank;
					traitEntry.GrantedRanks = newEntry.GrantedRanks;
					SetUpdateFieldValue(traitConfig.Entries, oldEntryIndex, traitEntry);

					if (applyTraits)
						ApplyTraitEntry(newEntry.TraitNodeEntryID, newEntry.Rank, newEntry.GrantedRanks, true);
				}
			}

		if (consumeCurrencies)
		{
			Dictionary<int, int> currencies = new();

			foreach (var costEntry in costEntries)
				TraitMgr.FillSpentCurrenciesMap(costEntry, currencies);

			foreach (var (traitCurrencyId, amount) in currencies)
			{
				var traitCurrency = CliDB.TraitCurrencyStorage.LookupByKey(traitCurrencyId);

				if (traitCurrency == null)
					continue;

				switch (traitCurrency.GetCurrencyType())
				{
					case TraitCurrencyType.Gold:
						ModifyMoney(-amount);

						break;
					case TraitCurrencyType.CurrencyTypesBased:
						RemoveCurrency((uint)traitCurrency.CurrencyTypesID, amount /* TODO: CurrencyDestroyReason */);

						break;
					default:
						break;
				}
			}
		}

		_traitConfigStates[(int)editedConfigId] = PlayerSpellState.Changed;
	}

	void ApplyTraitConfig(int configId, bool apply)
	{
		var traitConfig = GetTraitConfig(configId);

		if (traitConfig == null)
			return;

		foreach (var traitEntry in traitConfig.Entries)
			ApplyTraitEntry(traitEntry.TraitNodeEntryID, traitEntry.Rank, traitEntry.GrantedRanks, apply);
	}

	void ApplyTraitEntry(int traitNodeEntryId, int rank, int grantedRanks, bool apply)
	{
		var traitNodeEntry = CliDB.TraitNodeEntryStorage.LookupByKey(traitNodeEntryId);

		if (traitNodeEntry == null)
			return;

		var traitDefinition = CliDB.TraitDefinitionStorage.LookupByKey(traitNodeEntry.TraitDefinitionID);

		if (traitDefinition == null)
			return;

		if (traitDefinition.SpellID != 0)
		{
			if (apply)
				LearnSpell(traitDefinition.SpellID, true, 0, false, traitNodeEntry.TraitDefinitionID);
			else
				RemoveSpell(traitDefinition.SpellID);
		}
	}
}
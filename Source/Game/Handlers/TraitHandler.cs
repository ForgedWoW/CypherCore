﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.DataStorage;
using Game.Networking;
using Game.Networking.Packets;

namespace Game;

public partial class WorldSession
{
	[WorldPacketHandler(ClientOpcodes.TraitsCommitConfig)]
	void HandleTraitsCommitConfig(TraitsCommitConfig traitsCommitConfig)
	{
		var configId = traitsCommitConfig.Config.ID;
		var existingConfig = _player.GetTraitConfig(configId);

		if (existingConfig == null)
		{
			SendPacket(new TraitConfigCommitFailed(configId, 0, (int)TalentLearnResult.FailedUnknown));

			return;
		}

		if (_player.IsInCombat)
		{
			SendPacket(new TraitConfigCommitFailed(configId, 0, (int)TalentLearnResult.FailedAffectingCombat));

			return;
		}

		if (_player.Battleground && _player.Battleground.GetStatus() == BattlegroundStatus.InProgress)
		{
			SendPacket(new TraitConfigCommitFailed(configId, 0, (int)TalentLearnResult.InPvpMatch));

			return;
		}

		var hasRemovedEntries = false;
		TraitConfigPacket newConfigState = new(existingConfig);

		foreach (var kvp in traitsCommitConfig.Config.Entries.Values)
			foreach (var newEntry in kvp.Values)
			{
				var traitEntry = newConfigState.Entries.LookupByKey(newEntry.TraitNodeID)?.LookupByKey(newEntry.TraitNodeEntryID);

				if (traitEntry == null)
				{
					newConfigState.AddEntry(newEntry);

					continue;
				}

				if (traitEntry.Rank > newEntry.Rank)
				{
					var traitNode = CliDB.TraitNodeStorage.LookupByKey(newEntry.TraitNodeID);

					if (traitNode == null)
					{
						SendPacket(new TraitConfigCommitFailed(configId, 0, (int)TalentLearnResult.FailedUnknown));

						return;
					}

					var traitTree = CliDB.TraitTreeStorage.LookupByKey(traitNode.TraitTreeID);

					if (traitTree == null)
					{
						SendPacket(new TraitConfigCommitFailed(configId, 0, (int)TalentLearnResult.FailedUnknown));

						return;
					}

					if (traitTree.GetFlags().HasFlag(TraitTreeFlag.CannotRefund))
					{
						SendPacket(new TraitConfigCommitFailed(configId, 0, (int)TalentLearnResult.FailedCantRemoveTalent));

						return;
					}

					var traitNodeEntry = CliDB.TraitNodeEntryStorage.LookupByKey(newEntry.TraitNodeEntryID);

					if (traitNodeEntry == null)
					{
						SendPacket(new TraitConfigCommitFailed(configId, 0, (int)TalentLearnResult.FailedUnknown));

						return;
					}

					var traitDefinition = CliDB.TraitDefinitionStorage.LookupByKey(traitNodeEntry.TraitDefinitionID);

					if (traitDefinition == null)
					{
						SendPacket(new TraitConfigCommitFailed(configId, 0, (int)TalentLearnResult.FailedUnknown));

						return;
					}

					if (traitDefinition.SpellID != 0 && _player.SpellHistory.HasCooldown(traitDefinition.SpellID))
					{
						SendPacket(new TraitConfigCommitFailed(configId, traitDefinition.SpellID, (int)TalentLearnResult.FailedCantRemoveTalent));

						return;
					}

					if (traitDefinition.VisibleSpellID != 0 && _player.SpellHistory.HasCooldown((uint)traitDefinition.VisibleSpellID))
					{
						SendPacket(new TraitConfigCommitFailed(configId, traitDefinition.VisibleSpellID, (int)TalentLearnResult.FailedCantRemoveTalent));

						return;
					}

					hasRemovedEntries = true;
				}

				if (newEntry.Rank != 0)
					traitEntry.Rank = newEntry.Rank;
				else
					newConfigState.Entries.Remove(traitEntry.TraitNodeID);
			}

		var validationResult = TraitMgr.ValidateConfig(newConfigState, _player, true);

		if (validationResult != TalentLearnResult.LearnOk)
		{
			SendPacket(new TraitConfigCommitFailed(configId, 0, (int)validationResult));

			return;
		}

		var needsCastTime = newConfigState.Type == TraitConfigType.Combat && hasRemovedEntries;

		if (traitsCommitConfig.SavedLocalIdentifier != 0)
		{
			newConfigState.LocalIdentifier = traitsCommitConfig.SavedLocalIdentifier;
		}
		else
		{
			var savedConfig = _player.GetTraitConfig(traitsCommitConfig.SavedLocalIdentifier);

			if (savedConfig != null)
				newConfigState.LocalIdentifier = savedConfig.LocalIdentifier;
		}

		_player.UpdateTraitConfig(newConfigState, traitsCommitConfig.SavedConfigID, needsCastTime);
	}

	[WorldPacketHandler(ClientOpcodes.ClassTalentsRequestNewConfig)]
	void HandleClassTalentsRequestNewConfig(ClassTalentsRequestNewConfig classTalentsRequestNewConfig)
	{
		if (classTalentsRequestNewConfig.Config.Type != TraitConfigType.Combat)
			return;

		if ((classTalentsRequestNewConfig.Config.CombatConfigFlags & TraitCombatConfigFlags.ActiveForSpec) != (int)TraitCombatConfigFlags.None)
			return;

		long configCount = _player.ActivePlayerData.TraitConfigs.Values.Count(traitConfig => { return (TraitConfigType)(int)traitConfig.Type == TraitConfigType.Combat && ((TraitCombatConfigFlags)(int)traitConfig.CombatConfigFlags & TraitCombatConfigFlags.ActiveForSpec) == TraitCombatConfigFlags.None; });

		if (configCount >= TraitMgr.MAX_COMBAT_TRAIT_CONFIGS)
			return;

		int findFreeLocalIdentifier()
		{
			var index = 1;

			while (_player.ActivePlayerData.TraitConfigs.FindIndexIf(traitConfig => { return (TraitConfigType)(int)traitConfig.Type == TraitConfigType.Combat && traitConfig.ChrSpecializationID == _player.GetPrimarySpecialization() && traitConfig.LocalIdentifier == index; }) >= 0)
				++index;

			return index;
		}

		classTalentsRequestNewConfig.Config.ChrSpecializationID = (int)_player.GetPrimarySpecialization();
		classTalentsRequestNewConfig.Config.LocalIdentifier = findFreeLocalIdentifier();

		foreach (var grantedEntry in TraitMgr.GetGrantedTraitEntriesForConfig(classTalentsRequestNewConfig.Config, _player))
		{
			var newEntry = classTalentsRequestNewConfig.Config.Entries.LookupByKey(grantedEntry.TraitNodeID)?.LookupByKey(grantedEntry.TraitNodeEntryID);

			if (newEntry == null)
			{
				newEntry = new TraitEntryPacket();
				classTalentsRequestNewConfig.Config.AddEntry(newEntry);
			}

			newEntry.TraitNodeID = grantedEntry.TraitNodeID;
			newEntry.TraitNodeEntryID = grantedEntry.TraitNodeEntryID;
			newEntry.Rank = grantedEntry.Rank;
			newEntry.GrantedRanks = grantedEntry.GrantedRanks;

			var traitNodeEntry = CliDB.TraitNodeEntryStorage.LookupByKey(grantedEntry.TraitNodeEntryID);

			if (traitNodeEntry != null)
				if (newEntry.Rank + newEntry.GrantedRanks > traitNodeEntry.MaxRanks)
					newEntry.Rank = Math.Max(0, traitNodeEntry.MaxRanks - newEntry.GrantedRanks);
		}

		var validationResult = TraitMgr.ValidateConfig(classTalentsRequestNewConfig.Config, _player);

		if (validationResult != TalentLearnResult.LearnOk)
			return;

		_player.CreateTraitConfig(classTalentsRequestNewConfig.Config);
	}

	[WorldPacketHandler(ClientOpcodes.ClassTalentsRenameConfig)]
	void HandleClassTalentsRenameConfig(ClassTalentsRenameConfig classTalentsRenameConfig)
	{
		_player.RenameTraitConfig(classTalentsRenameConfig.ConfigID, classTalentsRenameConfig.Name);
	}

	[WorldPacketHandler(ClientOpcodes.ClassTalentsDeleteConfig)]
	void HandleClassTalentsDeleteConfig(ClassTalentsDeleteConfig classTalentsDeleteConfig)
	{
		_player.DeleteTraitConfig(classTalentsDeleteConfig.ConfigID);
	}

	[WorldPacketHandler(ClientOpcodes.ClassTalentsSetStarterBuildActive)]
	void HandleClassTalentsSetStarterBuildActive(ClassTalentsSetStarterBuildActive classTalentsSetStarterBuildActive)
	{
		var traitConfig = _player.GetTraitConfig(classTalentsSetStarterBuildActive.ConfigID);

		if (traitConfig == null)
			return;

		if ((TraitConfigType)(int)traitConfig.Type != TraitConfigType.Combat)
			return;

		if (!((TraitCombatConfigFlags)(int)traitConfig.CombatConfigFlags).HasFlag(TraitCombatConfigFlags.ActiveForSpec))
			return;

		if (classTalentsSetStarterBuildActive.Active)
		{
			TraitConfigPacket newConfigState = new(traitConfig);

			var freeLocalIdentifier = 1;

			while (_player.ActivePlayerData.TraitConfigs.FindIndexIf(traitConfig => { return (TraitConfigType)(int)traitConfig.Type == TraitConfigType.Combat && traitConfig.ChrSpecializationID == _player.GetPrimarySpecialization() && traitConfig.LocalIdentifier == freeLocalIdentifier; }) >= 0)
				++freeLocalIdentifier;

			TraitMgr.InitializeStarterBuildTraitConfig(newConfigState, _player);
			newConfigState.CombatConfigFlags |= TraitCombatConfigFlags.StarterBuild;
			newConfigState.LocalIdentifier = freeLocalIdentifier;

			_player.UpdateTraitConfig(newConfigState, 0, true);
		}
		else
		{
			_player.SetTraitConfigUseStarterBuild(classTalentsSetStarterBuildActive.ConfigID, false);
		}
	}

	[WorldPacketHandler(ClientOpcodes.ClassTalentsSetUsesSharedActionBars)]
	void HandleClassTalentsSetUsesSharedActionBars(ClassTalentsSetUsesSharedActionBars classTalentsSetUsesSharedActionBars)
	{
		_player.SetTraitConfigUseSharedActionBars(classTalentsSetUsesSharedActionBars.ConfigID,
												classTalentsSetUsesSharedActionBars.UsesShared,
												classTalentsSetUsesSharedActionBars.IsLastSelectedSavedConfig);
	}
}
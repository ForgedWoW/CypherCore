// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Forged.RealmServer.DataStorage;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets;

namespace Forged.RealmServer;

public partial class WorldSession
{
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

	[WorldPacketHandler(ClientOpcodes.ClassTalentsSetUsesSharedActionBars)]
	void HandleClassTalentsSetUsesSharedActionBars(ClassTalentsSetUsesSharedActionBars classTalentsSetUsesSharedActionBars)
	{
		_player.SetTraitConfigUseSharedActionBars(classTalentsSetUsesSharedActionBars.ConfigID,
												classTalentsSetUsesSharedActionBars.UsesShared,
												classTalentsSetUsesSharedActionBars.IsLastSelectedSavedConfig);
	}
}
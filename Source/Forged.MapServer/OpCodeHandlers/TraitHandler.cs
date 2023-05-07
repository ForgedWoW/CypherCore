﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Trait;
using Forged.MapServer.Spells;
using Framework.Constants;
using Game.Common.Handlers;

namespace Forged.MapServer.OpCodeHandlers;

public class TraitHandler : IWorldSessionHandler
{
    [WorldPacketHandler(ClientOpcodes.ClassTalentsDeleteConfig)]
    private void HandleClassTalentsDeleteConfig(ClassTalentsDeleteConfig classTalentsDeleteConfig)
    {
        _session.Player.DeleteTraitConfig(classTalentsDeleteConfig.ConfigID);
    }

    [WorldPacketHandler(ClientOpcodes.ClassTalentsRenameConfig)]
    private void HandleClassTalentsRenameConfig(ClassTalentsRenameConfig classTalentsRenameConfig)
    {
        _session.Player.RenameTraitConfig(classTalentsRenameConfig.ConfigID, classTalentsRenameConfig.Name);
    }

    [WorldPacketHandler(ClientOpcodes.ClassTalentsRequestNewConfig)]
    private void HandleClassTalentsRequestNewConfig(ClassTalentsRequestNewConfig classTalentsRequestNewConfig)
    {
        if (classTalentsRequestNewConfig.Config.Type != TraitConfigType.Combat)
            return;

        if ((classTalentsRequestNewConfig.Config.CombatConfigFlags & TraitCombatConfigFlags.ActiveForSpec) != (int)TraitCombatConfigFlags.None)
            return;

        long configCount = Enumerable.Count<TraitConfig>(_session.Player.ActivePlayerData.TraitConfigs.Values, traitConfig => { return (TraitConfigType)(int)traitConfig.Type == TraitConfigType.Combat && ((TraitCombatConfigFlags)(int)traitConfig.CombatConfigFlags & TraitCombatConfigFlags.ActiveForSpec) == TraitCombatConfigFlags.None; });

        if (configCount >= TraitMgr.MAX_COMBAT_TRAIT_CONFIGS)
            return;

        int findFreeLocalIdentifier()
        {
            var index = 1;

            while (_session.Player.ActivePlayerData.TraitConfigs.FindIndexIf(traitConfig => { return (TraitConfigType)(int)traitConfig.Type == TraitConfigType.Combat && traitConfig.ChrSpecializationID == _session.Player.GetPrimarySpecialization() && traitConfig.LocalIdentifier == index; }) >= 0)
                ++index;

            return index;
        }

        classTalentsRequestNewConfig.Config.ChrSpecializationID = (int)_session.Player.GetPrimarySpecialization();
        classTalentsRequestNewConfig.Config.LocalIdentifier = findFreeLocalIdentifier();

        foreach (var grantedEntry in TraitMgr.GetGrantedTraitEntriesForConfig(classTalentsRequestNewConfig.Config, _session.Player))
        {
            if (!classTalentsRequestNewConfig.Config.Entries.LookupByKey(grantedEntry.TraitNodeID)?.TryGetValue(grantedEntry.TraitNodeEntryID, out var newEntry))
            {
                newEntry = new TraitEntryPacket();
                classTalentsRequestNewConfig.Config.AddEntry(newEntry);
            }

            newEntry.TraitNodeID = grantedEntry.TraitNodeID;
            newEntry.TraitNodeEntryID = grantedEntry.TraitNodeEntryID;
            newEntry.Rank = grantedEntry.Rank;
            newEntry.GrantedRanks = grantedEntry.GrantedRanks;

            if (CliDB.TraitNodeEntryStorage.TryGetValue(grantedEntry.TraitNodeEntryID, out var traitNodeEntry))
                if (newEntry.Rank + newEntry.GrantedRanks > traitNodeEntry.MaxRanks)
                    newEntry.Rank = Math.Max(0, traitNodeEntry.MaxRanks - newEntry.GrantedRanks);
        }

        var validationResult = TraitMgr.ValidateConfig(classTalentsRequestNewConfig.Config, _session.Player);

        if (validationResult != TalentLearnResult.LearnOk)
            return;

        _session.Player.CreateTraitConfig(classTalentsRequestNewConfig.Config);
    }

    [WorldPacketHandler(ClientOpcodes.ClassTalentsSetStarterBuildActive)]
    private void HandleClassTalentsSetStarterBuildActive(ClassTalentsSetStarterBuildActive classTalentsSetStarterBuildActive)
    {
        var traitConfig = _session.Player.GetTraitConfig(classTalentsSetStarterBuildActive.ConfigID);

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

            while (_session.Player.ActivePlayerData.TraitConfigs.FindIndexIf(traitConfig => { return (TraitConfigType)(int)traitConfig.Type == TraitConfigType.Combat && traitConfig.ChrSpecializationID == _session.Player.GetPrimarySpecialization() && traitConfig.LocalIdentifier == freeLocalIdentifier; }) >= 0)
                ++freeLocalIdentifier;

            TraitMgr.InitializeStarterBuildTraitConfig(newConfigState, _session.Player);
            newConfigState.CombatConfigFlags |= TraitCombatConfigFlags.StarterBuild;
            newConfigState.LocalIdentifier = freeLocalIdentifier;

            _session.Player.UpdateTraitConfig(newConfigState, 0, true);
        }
        else
            _session.Player.SetTraitConfigUseStarterBuild(classTalentsSetStarterBuildActive.ConfigID, false);
    }

    [WorldPacketHandler(ClientOpcodes.ClassTalentsSetUsesSharedActionBars)]
    private void HandleClassTalentsSetUsesSharedActionBars(ClassTalentsSetUsesSharedActionBars classTalentsSetUsesSharedActionBars)
    {
        _session.Player.SetTraitConfigUseSharedActionBars(classTalentsSetUsesSharedActionBars.ConfigID,
                                                  classTalentsSetUsesSharedActionBars.UsesShared,
                                                  classTalentsSetUsesSharedActionBars.IsLastSelectedSavedConfig);
    }

    [WorldPacketHandler(ClientOpcodes.TraitsCommitConfig)]
    private void HandleTraitsCommitConfig(TraitsCommitConfig traitsCommitConfig)
    {
        var configId = traitsCommitConfig.Config.ID;
        var existingConfig = _session.Player.GetTraitConfig(configId);

        if (existingConfig == null)
        {
            SendPacket(new TraitConfigCommitFailed(configId, 0, (int)TalentLearnResult.FailedUnknown));

            return;
        }

        if (_session.Player.IsInCombat)
        {
            SendPacket(new TraitConfigCommitFailed(configId, 0, (int)TalentLearnResult.FailedAffectingCombat));

            return;
        }

        if (_session.Player.Battleground && _session.Player.Battleground.GetStatus() == BattlegroundStatus.InProgress)
        {
            SendPacket(new TraitConfigCommitFailed(configId, 0, (int)TalentLearnResult.InPvpMatch));

            return;
        }

        var hasRemovedEntries = false;
        TraitConfigPacket newConfigState = new(existingConfig);

        foreach (var kvp in traitsCommitConfig.Config.Entries.Values)
            foreach (var newEntry in kvp.Values)
            {
                if (!newConfigState.Entries.LookupByKey(newEntry.TraitNodeID)?.TryGetValue(newEntry.TraitNodeEntryID, out var traitEntry))
                {
                    newConfigState.AddEntry(newEntry);

                    continue;
                }

                if (traitEntry.Rank > newEntry.Rank)
                {
                    if (!CliDB.TraitNodeStorage.TryGetValue(newEntry.TraitNodeID, out var traitNode))
                    {
                        SendPacket(new TraitConfigCommitFailed(configId, 0, (int)TalentLearnResult.FailedUnknown));

                        return;
                    }

                    if (!CliDB.TraitTreeStorage.TryGetValue(traitNode.TraitTreeID, out var traitTree))
                    {
                        SendPacket(new TraitConfigCommitFailed(configId, 0, (int)TalentLearnResult.FailedUnknown));

                        return;
                    }

                    if (traitTree.GetFlags().HasFlag(TraitTreeFlag.CannotRefund))
                    {
                        SendPacket(new TraitConfigCommitFailed(configId, 0, (int)TalentLearnResult.FailedCantRemoveTalent));

                        return;
                    }

                    if (!CliDB.TraitNodeEntryStorage.TryGetValue(newEntry.TraitNodeEntryID, out var traitNodeEntry))
                    {
                        SendPacket(new TraitConfigCommitFailed(configId, 0, (int)TalentLearnResult.FailedUnknown));

                        return;
                    }

                    if (!CliDB.TraitDefinitionStorage.TryGetValue(traitNodeEntry.TraitDefinitionID, out var traitDefinition))
                    {
                        SendPacket(new TraitConfigCommitFailed(configId, 0, (int)TalentLearnResult.FailedUnknown));

                        return;
                    }

                    if (traitDefinition.SpellID != 0 && _session.Player.SpellHistory.HasCooldown(traitDefinition.SpellID))
                    {
                        SendPacket(new TraitConfigCommitFailed(configId, traitDefinition.SpellID, (int)TalentLearnResult.FailedCantRemoveTalent));

                        return;
                    }

                    if (traitDefinition.VisibleSpellID != 0 && _session.Player.SpellHistory.HasCooldown(traitDefinition.VisibleSpellID))
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

        var validationResult = TraitMgr.ValidateConfig(newConfigState, _session.Player, true);

        if (validationResult != TalentLearnResult.LearnOk)
        {
            SendPacket(new TraitConfigCommitFailed(configId, 0, (int)validationResult));

            return;
        }

        var needsCastTime = newConfigState.Type == TraitConfigType.Combat && hasRemovedEntries;

        if (traitsCommitConfig.SavedLocalIdentifier != 0)
            newConfigState.LocalIdentifier = traitsCommitConfig.SavedLocalIdentifier;
        else
        {
            var savedConfig = _session.Player.GetTraitConfig(traitsCommitConfig.SavedLocalIdentifier);

            if (savedConfig != null)
                newConfigState.LocalIdentifier = savedConfig.LocalIdentifier;
        }

        _session.Player.UpdateTraitConfig(newConfigState, traitsCommitConfig.SavedConfigID, needsCastTime);
    }
}
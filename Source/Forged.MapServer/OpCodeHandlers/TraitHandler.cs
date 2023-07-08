// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Linq;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.T;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Trait;
using Forged.MapServer.Server;
using Forged.MapServer.Spells;
using Framework.Constants;
using Game.Common.Handlers;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class TraitHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;
    private readonly DB6Storage<TraitDefinitionRecord> _traitDefinitionRecords;
    private readonly TraitMgr _traitMgr;
    private readonly DB6Storage<TraitNodeEntryRecord> _traitNodeEntryRecords;
    private readonly DB6Storage<TraitNodeRecord> _traitNodeRecords;
    private readonly DB6Storage<TraitTreeRecord> _traitTreeRecords;

    public TraitHandler(WorldSession session, TraitMgr traitMgr, DB6Storage<TraitNodeEntryRecord> traitNodeEntryRecords, DB6Storage<TraitNodeRecord> traitNodeRecords,
                        DB6Storage<TraitTreeRecord> traitTreeRecords, DB6Storage<TraitDefinitionRecord> traitDefinitionRecords)
    {
        _session = session;
        _traitMgr = traitMgr;
        _traitNodeEntryRecords = traitNodeEntryRecords;
        _traitNodeRecords = traitNodeRecords;
        _traitTreeRecords = traitTreeRecords;
        _traitDefinitionRecords = traitDefinitionRecords;
    }

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

        long configCount = _session.Player.ActivePlayerData.TraitConfigs.Values.Count(traitConfig => (TraitConfigType)(int)traitConfig.Type == TraitConfigType.Combat &&
                                                                                                     ((TraitCombatConfigFlags)(int)traitConfig.CombatConfigFlags & TraitCombatConfigFlags.ActiveForSpec) == TraitCombatConfigFlags.None);

        if (configCount >= TraitMgr.MAX_COMBAT_TRAIT_CONFIGS)
            return;

        int FindFreeLocalIdentifier()
        {
            var index = 1;

            while (_session.Player.ActivePlayerData.TraitConfigs.FindIndexIf(traitConfig => (TraitConfigType)(int)traitConfig.Type == TraitConfigType.Combat &&
                                                                                            traitConfig.ChrSpecializationID == _session.Player.GetPrimarySpecialization() &&
                                                                                            traitConfig.LocalIdentifier == index) >= 0)
                ++index;

            return index;
        }

        classTalentsRequestNewConfig.Config.ChrSpecializationID = (int)_session.Player.GetPrimarySpecialization();
        classTalentsRequestNewConfig.Config.LocalIdentifier = FindFreeLocalIdentifier();

        foreach (var grantedEntry in _traitMgr.GetGrantedTraitEntriesForConfig(classTalentsRequestNewConfig.Config, _session.Player))
        {
            if (!classTalentsRequestNewConfig.Config.Entries.TryGetValue(grantedEntry.TraitNodeID, out var traitEntryPackets) ||
                !traitEntryPackets.TryGetValue(grantedEntry.TraitNodeEntryID, out var newEntry))
            {
                newEntry = new TraitEntryPacket();
                classTalentsRequestNewConfig.Config.AddEntry(newEntry);
            }

            newEntry.TraitNodeID = grantedEntry.TraitNodeID;
            newEntry.TraitNodeEntryID = grantedEntry.TraitNodeEntryID;
            newEntry.Rank = grantedEntry.Rank;
            newEntry.GrantedRanks = grantedEntry.GrantedRanks;

            if (_traitNodeEntryRecords.TryGetValue((uint)grantedEntry.TraitNodeEntryID, out var traitNodeEntry) &&
                newEntry.Rank + newEntry.GrantedRanks > traitNodeEntry.MaxRanks)
                newEntry.Rank = Math.Max(0, traitNodeEntry.MaxRanks - newEntry.GrantedRanks);
        }

        var validationResult = _traitMgr.ValidateConfig(classTalentsRequestNewConfig.Config, _session.Player);

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

            while (_session.Player.ActivePlayerData.TraitConfigs.FindIndexIf(tCfg => (TraitConfigType)(int)tCfg.Type == TraitConfigType.Combat && tCfg.ChrSpecializationID == _session.Player.GetPrimarySpecialization() && tCfg.LocalIdentifier == freeLocalIdentifier) >= 0)
                ++freeLocalIdentifier;

            _traitMgr.InitializeStarterBuildTraitConfig(newConfigState, _session.Player);
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
            _session.SendPacket(new TraitConfigCommitFailed(configId, 0, (int)TalentLearnResult.FailedUnknown));

            return;
        }

        if (_session.Player.IsInCombat)
        {
            _session.SendPacket(new TraitConfigCommitFailed(configId, 0, (int)TalentLearnResult.FailedAffectingCombat));

            return;
        }

        if (_session.Player.Battleground is { Status: BattlegroundStatus.InProgress })
        {
            _session.SendPacket(new TraitConfigCommitFailed(configId, 0, (int)TalentLearnResult.InPvpMatch));

            return;
        }

        var hasRemovedEntries = false;
        TraitConfigPacket newConfigState = new(existingConfig);

        foreach (var kvp in traitsCommitConfig.Config.Entries.Values)
            foreach (var newEntry in kvp.Values)
            {
                if (!newConfigState.Entries.TryGetValue(newEntry.TraitNodeID, out var traitEntryPackets) ||
                    !traitEntryPackets.TryGetValue(newEntry.TraitNodeEntryID, out var traitEntry))
                {
                    newConfigState.AddEntry(newEntry);

                    continue;
                }

                if (traitEntry.Rank > newEntry.Rank)
                {
                    if (!_traitNodeRecords.TryGetValue((uint)newEntry.TraitNodeID, out var traitNode))
                    {
                        _session.SendPacket(new TraitConfigCommitFailed(configId, 0, (int)TalentLearnResult.FailedUnknown));

                        return;
                    }

                    if (!_traitTreeRecords.TryGetValue((uint)traitNode.TraitTreeID, out var traitTree))
                    {
                        _session.SendPacket(new TraitConfigCommitFailed(configId, 0, (int)TalentLearnResult.FailedUnknown));

                        return;
                    }

                    if (traitTree.GetFlags().HasFlag(TraitTreeFlag.CannotRefund))
                    {
                        _session.SendPacket(new TraitConfigCommitFailed(configId, 0, (int)TalentLearnResult.FailedCantRemoveTalent));

                        return;
                    }

                    if (!_traitNodeEntryRecords.TryGetValue((uint)newEntry.TraitNodeEntryID, out var traitNodeEntry))
                    {
                        _session.SendPacket(new TraitConfigCommitFailed(configId, 0, (int)TalentLearnResult.FailedUnknown));

                        return;
                    }

                    if (!_traitDefinitionRecords.TryGetValue((uint)traitNodeEntry.TraitDefinitionID, out var traitDefinition))
                    {
                        _session.SendPacket(new TraitConfigCommitFailed(configId, 0, (int)TalentLearnResult.FailedUnknown));

                        return;
                    }

                    if (traitDefinition.SpellID != 0 && _session.Player.SpellHistory.HasCooldown(traitDefinition.SpellID))
                    {
                        _session.SendPacket(new TraitConfigCommitFailed(configId, traitDefinition.SpellID, (int)TalentLearnResult.FailedCantRemoveTalent));

                        return;
                    }

                    if (traitDefinition.VisibleSpellID != 0 && _session.Player.SpellHistory.HasCooldown(traitDefinition.VisibleSpellID))
                    {
                        _session.SendPacket(new TraitConfigCommitFailed(configId, traitDefinition.VisibleSpellID, (int)TalentLearnResult.FailedCantRemoveTalent));

                        return;
                    }

                    hasRemovedEntries = true;
                }

                if (newEntry.Rank != 0)
                    traitEntry.Rank = newEntry.Rank;
                else
                    newConfigState.Entries.Remove(traitEntry.TraitNodeID);
            }

        var validationResult = _traitMgr.ValidateConfig(newConfigState, _session.Player, true);

        if (validationResult != TalentLearnResult.LearnOk)
        {
            _session.SendPacket(new TraitConfigCommitFailed(configId, 0, (int)validationResult));

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
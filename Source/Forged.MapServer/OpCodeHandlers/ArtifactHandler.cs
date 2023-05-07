// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.DataStorage.Structs.GameTable;
using Forged.MapServer.DataStorage.Structs.P;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Artifact;
using Forged.MapServer.Server;
using Framework.Constants;
using Game.Common.Handlers;

namespace Forged.MapServer.OpCodeHandlers;

public class ArtifactHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;
    private readonly GameTable<GtArtifactLevelXPRecord> _artifactLevelXpTable;
    private readonly DB6Storage<ArtifactPowerRecord> _artifactPowerRecords;
    private readonly DB2Manager _db2Manager;
    private readonly DB6Storage<ArtifactAppearanceRecord> _appearanceRecords;
    private readonly DB6Storage<ArtifactTierRecord> _artifactTierRecords;
    private readonly DB6Storage<ArtifactAppearanceSetRecord> _appearanceSetRecords;
    private readonly DB6Storage<PlayerConditionRecord> _playerConditionRecords;
    private readonly ConditionManager _conditionManager;

    public ArtifactHandler(WorldSession session, GameTable<GtArtifactLevelXPRecord> artifactLevelXpTable, DB6Storage<ArtifactPowerRecord> artifactPowerRecords, DB2Manager db2Manager,
                           DB6Storage<ArtifactAppearanceRecord> appearanceRecords, DB6Storage<ArtifactTierRecord> artifactTierRecords, DB6Storage<ArtifactAppearanceSetRecord> appearanceSetRecords,
                           DB6Storage<PlayerConditionRecord> playerConditionRecords, ConditionManager conditionManager)
    {
        _session = session;
        _artifactLevelXpTable = artifactLevelXpTable;
        _artifactPowerRecords = artifactPowerRecords;
        _db2Manager = db2Manager;
        _appearanceRecords = appearanceRecords;
        _artifactTierRecords = artifactTierRecords;
        _appearanceSetRecords = appearanceSetRecords;
        _playerConditionRecords = playerConditionRecords;
        _conditionManager = conditionManager;
    }

    [WorldPacketHandler(ClientOpcodes.ArtifactAddPower, Processing = PacketProcessing.Inplace)]
    private void HandleArtifactAddPower(ArtifactAddPower artifactAddPower)
    {
        if (_session.Player.GetGameObjectIfCanInteractWith(artifactAddPower.ForgeGUID, GameObjectTypes.ItemForge) == null)
            return;

        var artifact = _session.Player.GetItemByGuid(artifactAddPower.ArtifactGUID);

        if (artifact == null || artifact.IsArtifactDisabled())
            return;

        var currentArtifactTier = artifact.GetModifier(ItemModifier.ArtifactTier);

        ulong xpCost = 0;
        var cost = _artifactLevelXpTable.GetRow(artifact.GetTotalPurchasedArtifactPowers() + 1);

        if (cost != null)
            xpCost = (ulong)(currentArtifactTier == PlayerConst.MaxArtifactTier ? cost.XP2 : cost.XP);

        if (xpCost > artifact.ItemData.ArtifactXP)
            return;

        if (artifactAddPower.PowerChoices.Empty())
            return;

        var artifactPower = artifact.GetArtifactPower(artifactAddPower.PowerChoices[0].ArtifactPowerID);

        if (artifactPower == null)
            return;

        if (!_artifactPowerRecords.TryGetValue(artifactPower.ArtifactPowerId, out var artifactPowerEntry))
            return;

        if (artifactPowerEntry.Tier > currentArtifactTier)
            return;

        uint maxRank = artifactPowerEntry.MaxPurchasableRank;

        if (artifactPowerEntry.Tier < currentArtifactTier)
        {
            if (artifactPowerEntry.Flags.HasAnyFlag(ArtifactPowerFlag.Final))
                maxRank = 1;
            else if (artifactPowerEntry.Flags.HasAnyFlag(ArtifactPowerFlag.MaxRankWithTier))
                maxRank += currentArtifactTier - artifactPowerEntry.Tier;
        }

        if (artifactAddPower.PowerChoices[0].Rank != artifactPower.PurchasedRank + 1 ||
            artifactAddPower.PowerChoices[0].Rank > maxRank)
            return;

        if (!artifactPowerEntry.Flags.HasAnyFlag(ArtifactPowerFlag.NoLinkRequired))
        {
            var artifactPowerLinks = _db2Manager.GetArtifactPowerLinks(artifactPower.ArtifactPowerId);

            if (artifactPowerLinks != null)
            {
                var hasAnyLink = false;

                foreach (var artifactPowerLinkId in artifactPowerLinks)
                {
                    if (!_artifactPowerRecords.TryGetValue(artifactPowerLinkId, out var artifactPowerLink))
                        continue;

                    var artifactPowerLinkLearned = artifact.GetArtifactPower(artifactPowerLinkId);

                    if (artifactPowerLinkLearned == null)
                        continue;

                    if (artifactPowerLinkLearned.PurchasedRank >= artifactPowerLink.MaxPurchasableRank)
                    {
                        hasAnyLink = true;

                        break;
                    }
                }

                if (!hasAnyLink)
                    return;
            }
        }

        var artifactPowerRank = _db2Manager.GetArtifactPowerRank(artifactPower.ArtifactPowerId, (byte)(artifactPower.CurrentRankWithBonus + 1 - 1)); // need data for next rank, but -1 because of how db2 data is structured

        if (artifactPowerRank == null)
            return;

        artifact.SetArtifactPower(artifactPower.ArtifactPowerId, (byte)(artifactPower.PurchasedRank + 1), (byte)(artifactPower.CurrentRankWithBonus + 1));

        if (artifact.IsEquipped)
        {
            _session.Player.ApplyArtifactPowerRank(artifact, artifactPowerRank, true);

            foreach (var power in artifact.ItemData.ArtifactPowers)
            {
                var scaledArtifactPowerEntry = _artifactPowerRecords.LookupByKey(power.ArtifactPowerId);

                if (!scaledArtifactPowerEntry.Flags.HasAnyFlag(ArtifactPowerFlag.ScalesWithNumPowers))
                    continue;

                var scaledArtifactPowerRank = _db2Manager.GetArtifactPowerRank(scaledArtifactPowerEntry.Id, 0);

                if (scaledArtifactPowerRank == null)
                    continue;

                artifact.SetArtifactPower(power.ArtifactPowerId, power.PurchasedRank, (byte)(power.CurrentRankWithBonus + 1));

                _session.Player.ApplyArtifactPowerRank(artifact, scaledArtifactPowerRank, false);
                _session.Player.ApplyArtifactPowerRank(artifact, scaledArtifactPowerRank, true);
            }
        }

        artifact.SetArtifactXP(artifact.ItemData.ArtifactXP - xpCost);
        artifact.SetState(ItemUpdateState.Changed, _session.Player);

        var totalPurchasedArtifactPower = artifact.GetTotalPurchasedArtifactPowers();
        uint artifactTier = 0;

        foreach (var tier in _artifactTierRecords.Values)
        {
            if (artifactPowerEntry.Flags.HasAnyFlag(ArtifactPowerFlag.Final) && artifactPowerEntry.Tier < PlayerConst.MaxArtifactTier)
            {
                artifactTier = artifactPowerEntry.Tier + 1u;

                break;
            }

            if (totalPurchasedArtifactPower < tier.MaxNumTraits)
            {
                artifactTier = tier.ArtifactTier;

                break;
            }
        }

        artifactTier = Math.Max(artifactTier, (uint)currentArtifactTier);

        for (var i = currentArtifactTier; i <= artifactTier; ++i)
            artifact.InitArtifactPowers(artifact.Template.ArtifactID, (byte)i);

        artifact.SetModifier(ItemModifier.ArtifactTier, artifactTier);
    }

    [WorldPacketHandler(ClientOpcodes.ArtifactSetAppearance, Processing = PacketProcessing.Inplace)]
    private void HandleArtifactSetAppearance(ArtifactSetAppearance artifactSetAppearance)
    {
        if (_session.Player.GetGameObjectIfCanInteractWith(artifactSetAppearance.ForgeGUID, GameObjectTypes.ItemForge) == null)
            return;

        if (!_appearanceRecords.TryGetValue((uint)artifactSetAppearance.ArtifactAppearanceID, out var artifactAppearance))
            return;

        var artifact = _session.Player.GetItemByGuid(artifactSetAppearance.ArtifactGUID);

        if (artifact == null)
            return;

        var artifactAppearanceSet = _appearanceSetRecords.LookupByKey(artifactAppearance.ArtifactAppearanceSetID);

        if (artifactAppearanceSet == null || artifactAppearanceSet.ArtifactID != artifact.Template.ArtifactID)
            return;

        if (_playerConditionRecords.TryGetValue(artifactAppearance.UnlockPlayerConditionID, out var playerCondition))
            if (!_conditionManager.IsPlayerMeetingCondition(_session.Player, playerCondition))
                return;

        artifact.SetAppearanceModId(artifactAppearance.ItemAppearanceModifierID);
        artifact.SetModifier(ItemModifier.ArtifactAppearanceId, artifactAppearance.Id);
        artifact.SetState(ItemUpdateState.Changed, _session.Player);
        var childItem = _session.Player.GetChildItemByGuid(artifact.ChildItem);

        if (childItem != null)
        {
            childItem.SetAppearanceModId(artifactAppearance.ItemAppearanceModifierID);
            childItem.SetState(ItemUpdateState.Changed, _session.Player);
        }

        if (artifact.IsEquipped)
        {
            // change weapon appearance
            _session.Player.SetVisibleItemSlot(artifact.Slot, artifact);

            if (childItem != null)
                _session.Player.SetVisibleItemSlot(childItem.Slot, childItem);

            // change druid form appearance
            if (artifactAppearance.OverrideShapeshiftDisplayID != 0 && artifactAppearance.OverrideShapeshiftFormID != 0 && _session.Player.ShapeshiftForm == (ShapeShiftForm)artifactAppearance.OverrideShapeshiftFormID)
                _session.Player.RestoreDisplayId(_session.Player.IsMounted);
        }
    }

    [WorldPacketHandler(ClientOpcodes.ConfirmArtifactRespec)]
    private void HandleConfirmArtifactRespec(ConfirmArtifactRespec confirmArtifactRespec)
    {
        if (_session.Player.GetNPCIfCanInteractWith(confirmArtifactRespec.NpcGUID, NPCFlags.ArtifactPowerRespec, NPCFlags2.None) == null)
            return;

        var artifact = _session.Player.GetItemByGuid(confirmArtifactRespec.ArtifactGUID);

        if (artifact == null || artifact.IsArtifactDisabled())
            return;

        ulong xpCost = 0;
        var cost = _artifactLevelXpTable.GetRow(artifact.GetTotalPurchasedArtifactPowers() + 1);

        if (cost != null)
            xpCost = (ulong)(artifact.GetModifier(ItemModifier.ArtifactTier) == 1 ? cost.XP2 : cost.XP);

        if (xpCost > artifact.ItemData.ArtifactXP)
            return;

        var newAmount = artifact.ItemData.ArtifactXP - xpCost;

        for (uint i = 0; i <= artifact.GetTotalPurchasedArtifactPowers(); ++i)
        {
            var cost1 = _artifactLevelXpTable.GetRow(i);

            if (cost1 != null)
                newAmount += (ulong)(artifact.GetModifier(ItemModifier.ArtifactTier) == 1 ? cost1.XP2 : cost1.XP);
        }

        foreach (var artifactPower in artifact.ItemData.ArtifactPowers)
        {
            var oldPurchasedRank = artifactPower.PurchasedRank;

            if (oldPurchasedRank == 0)
                continue;

            artifact.SetArtifactPower(artifactPower.ArtifactPowerId, (byte)(artifactPower.PurchasedRank - oldPurchasedRank), (byte)(artifactPower.CurrentRankWithBonus - oldPurchasedRank));

            if (artifact.IsEquipped)
            {
                var artifactPowerRank = _db2Manager.GetArtifactPowerRank(artifactPower.ArtifactPowerId, 0);

                if (artifactPowerRank != null)
                    _session.Player.ApplyArtifactPowerRank(artifact, artifactPowerRank, false);
            }
        }

        foreach (var power in artifact.ItemData.ArtifactPowers)
        {
            var scaledArtifactPowerEntry = _artifactPowerRecords.LookupByKey(power.ArtifactPowerId);

            if (!scaledArtifactPowerEntry.Flags.HasAnyFlag(ArtifactPowerFlag.ScalesWithNumPowers))
                continue;

            var scaledArtifactPowerRank = _db2Manager.GetArtifactPowerRank(scaledArtifactPowerEntry.Id, 0);

            if (scaledArtifactPowerRank == null)
                continue;

            artifact.SetArtifactPower(power.ArtifactPowerId, power.PurchasedRank, 0);

            _session.Player.ApplyArtifactPowerRank(artifact, scaledArtifactPowerRank, false);
        }

        artifact.SetArtifactXP(newAmount);
        artifact.SetState(ItemUpdateState.Changed, _session.Player);
    }
}
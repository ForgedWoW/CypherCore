﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Networking;
using Game.Networking.Packets;

namespace Game;

public partial class WorldSession
{
	[WorldPacketHandler(ClientOpcodes.ArtifactAddPower, Processing = PacketProcessing.Inplace)]
	void HandleArtifactAddPower(ArtifactAddPower artifactAddPower)
	{
		if (!_player.GetGameObjectIfCanInteractWith(artifactAddPower.ForgeGUID, GameObjectTypes.ItemForge))
			return;

		var artifact = _player.GetItemByGuid(artifactAddPower.ArtifactGUID);

		if (!artifact || artifact.IsArtifactDisabled())
			return;

		var currentArtifactTier = artifact.GetModifier(ItemModifier.ArtifactTier);

		ulong xpCost = 0;
		var cost = CliDB.ArtifactLevelXPGameTable.GetRow(artifact.GetTotalPurchasedArtifactPowers() + 1);

		if (cost != null)
			xpCost = (ulong)(currentArtifactTier == PlayerConst.MaxArtifactTier ? cost.XP2 : cost.XP);

		if (xpCost > artifact.ItemData.ArtifactXP)
			return;

		if (artifactAddPower.PowerChoices.Empty())
			return;

		var artifactPower = artifact.GetArtifactPower(artifactAddPower.PowerChoices[0].ArtifactPowerID);

		if (artifactPower == null)
			return;

		var artifactPowerEntry = CliDB.ArtifactPowerStorage.LookupByKey(artifactPower.ArtifactPowerId);

		if (artifactPowerEntry == null)
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
			var artifactPowerLinks = Global.DB2Mgr.GetArtifactPowerLinks(artifactPower.ArtifactPowerId);

			if (artifactPowerLinks != null)
			{
				var hasAnyLink = false;

				foreach (var artifactPowerLinkId in artifactPowerLinks)
				{
					var artifactPowerLink = CliDB.ArtifactPowerStorage.LookupByKey(artifactPowerLinkId);

					if (artifactPowerLink == null)
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

		var artifactPowerRank = Global.DB2Mgr.GetArtifactPowerRank(artifactPower.ArtifactPowerId, (byte)(artifactPower.CurrentRankWithBonus + 1 - 1)); // need data for next rank, but -1 because of how db2 data is structured

		if (artifactPowerRank == null)
			return;

		artifact.SetArtifactPower(artifactPower.ArtifactPowerId, (byte)(artifactPower.PurchasedRank + 1), (byte)(artifactPower.CurrentRankWithBonus + 1));

		if (artifact.IsEquipped)
		{
			_player.ApplyArtifactPowerRank(artifact, artifactPowerRank, true);

			foreach (var power in artifact.ItemData.ArtifactPowers)
			{
				var scaledArtifactPowerEntry = CliDB.ArtifactPowerStorage.LookupByKey(power.ArtifactPowerId);

				if (!scaledArtifactPowerEntry.Flags.HasAnyFlag(ArtifactPowerFlag.ScalesWithNumPowers))
					continue;

				var scaledArtifactPowerRank = Global.DB2Mgr.GetArtifactPowerRank(scaledArtifactPowerEntry.Id, 0);

				if (scaledArtifactPowerRank == null)
					continue;

				artifact.SetArtifactPower(power.ArtifactPowerId, power.PurchasedRank, (byte)(power.CurrentRankWithBonus + 1));

				_player.ApplyArtifactPowerRank(artifact, scaledArtifactPowerRank, false);
				_player.ApplyArtifactPowerRank(artifact, scaledArtifactPowerRank, true);
			}
		}

		artifact.SetArtifactXP(artifact.ItemData.ArtifactXP - xpCost);
		artifact.SetState(ItemUpdateState.Changed, _player);

		var totalPurchasedArtifactPower = artifact.GetTotalPurchasedArtifactPowers();
		uint artifactTier = 0;

		foreach (var tier in CliDB.ArtifactTierStorage.Values)
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

		artifactTier = Math.Max(artifactTier, currentArtifactTier);

		for (var i = currentArtifactTier; i <= artifactTier; ++i)
			artifact.InitArtifactPowers(artifact.Template.ArtifactID, (byte)i);

		artifact.SetModifier(ItemModifier.ArtifactTier, artifactTier);
	}

	[WorldPacketHandler(ClientOpcodes.ArtifactSetAppearance, Processing = PacketProcessing.Inplace)]
	void HandleArtifactSetAppearance(ArtifactSetAppearance artifactSetAppearance)
	{
		if (!_player.GetGameObjectIfCanInteractWith(artifactSetAppearance.ForgeGUID, GameObjectTypes.ItemForge))
			return;

		var artifactAppearance = CliDB.ArtifactAppearanceStorage.LookupByKey(artifactSetAppearance.ArtifactAppearanceID);

		if (artifactAppearance == null)
			return;

		var artifact = _player.GetItemByGuid(artifactSetAppearance.ArtifactGUID);

		if (!artifact)
			return;

		var artifactAppearanceSet = CliDB.ArtifactAppearanceSetStorage.LookupByKey(artifactAppearance.ArtifactAppearanceSetID);

		if (artifactAppearanceSet == null || artifactAppearanceSet.ArtifactID != artifact.Template.ArtifactID)
			return;

		var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(artifactAppearance.UnlockPlayerConditionID);

		if (playerCondition != null)
			if (!ConditionManager.IsPlayerMeetingCondition(_player, playerCondition))
				return;

		artifact.SetAppearanceModId(artifactAppearance.ItemAppearanceModifierID);
		artifact.SetModifier(ItemModifier.ArtifactAppearanceId, artifactAppearance.Id);
		artifact.SetState(ItemUpdateState.Changed, _player);
		var childItem = _player.GetChildItemByGuid(artifact.ChildItem);

		if (childItem)
		{
			childItem.SetAppearanceModId(artifactAppearance.ItemAppearanceModifierID);
			childItem.SetState(ItemUpdateState.Changed, _player);
		}

		if (artifact.IsEquipped)
		{
			// change weapon appearance
			_player.SetVisibleItemSlot(artifact.Slot, artifact);

			if (childItem)
				_player.SetVisibleItemSlot(childItem.Slot, childItem);

			// change druid form appearance
			if (artifactAppearance.OverrideShapeshiftDisplayID != 0 && artifactAppearance.OverrideShapeshiftFormID != 0 && _player.ShapeshiftForm == (ShapeShiftForm)artifactAppearance.OverrideShapeshiftFormID)
				_player.RestoreDisplayId(_player.IsMounted);
		}
	}

	[WorldPacketHandler(ClientOpcodes.ConfirmArtifactRespec)]
	void HandleConfirmArtifactRespec(ConfirmArtifactRespec confirmArtifactRespec)
	{
		if (!_player.GetNPCIfCanInteractWith(confirmArtifactRespec.NpcGUID, NPCFlags.ArtifactPowerRespec, NPCFlags2.None))
			return;

		var artifact = _player.GetItemByGuid(confirmArtifactRespec.ArtifactGUID);

		if (!artifact || artifact.IsArtifactDisabled())
			return;

		ulong xpCost = 0;
		var cost = CliDB.ArtifactLevelXPGameTable.GetRow(artifact.GetTotalPurchasedArtifactPowers() + 1);

		if (cost != null)
			xpCost = (ulong)(artifact.GetModifier(ItemModifier.ArtifactTier) == 1 ? cost.XP2 : cost.XP);

		if (xpCost > artifact.ItemData.ArtifactXP)
			return;

		var newAmount = artifact.ItemData.ArtifactXP - xpCost;

		for (uint i = 0; i <= artifact.GetTotalPurchasedArtifactPowers(); ++i)
		{
			var cost1 = CliDB.ArtifactLevelXPGameTable.GetRow(i);

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
				var artifactPowerRank = Global.DB2Mgr.GetArtifactPowerRank(artifactPower.ArtifactPowerId, 0);

				if (artifactPowerRank != null)
					_player.ApplyArtifactPowerRank(artifact, artifactPowerRank, false);
			}
		}

		foreach (var power in artifact.ItemData.ArtifactPowers)
		{
			var scaledArtifactPowerEntry = CliDB.ArtifactPowerStorage.LookupByKey(power.ArtifactPowerId);

			if (!scaledArtifactPowerEntry.Flags.HasAnyFlag(ArtifactPowerFlag.ScalesWithNumPowers))
				continue;

			var scaledArtifactPowerRank = Global.DB2Mgr.GetArtifactPowerRank(scaledArtifactPowerEntry.Id, 0);

			if (scaledArtifactPowerRank == null)
				continue;

			artifact.SetArtifactPower(power.ArtifactPowerId, power.PurchasedRank, 0);

			_player.ApplyArtifactPowerRank(artifact, scaledArtifactPowerRank, false);
		}

		artifact.SetArtifactXP(newAmount);
		artifact.SetState(ItemUpdateState.Changed, _player);
	}
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Guilds;
using Forged.RealmServer.Entities.Players;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Achievements;
using Game.Common.Networking.Packets.Guild;
using Game.Common.Handlers;

namespace Forged.RealmServer;

public class GuildHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;

    public GuildHandler(WorldSession session)
    {
        _session = session;
    }

    [WorldPacketHandler(ClientOpcodes.GuildInviteByName)]
	void HandleGuildInviteByName(GuildInviteByName packet)
	{
		if (!ObjectManager.NormalizePlayerName(ref packet.Name))
			return;

		var guild = Player.Guild;

		if (guild)
			guild.HandleInviteMember(this, packet.Name);
	}

	[WorldPacketHandler(ClientOpcodes.GuildOfficerRemoveMember)]
	void HandleGuildOfficerRemoveMember(GuildOfficerRemoveMember packet)
	{
		var guild = Player.Guild;

		if (guild)
			guild.HandleRemoveMember(this, packet.Removee);
	}

	[WorldPacketHandler(ClientOpcodes.AcceptGuildInvite)]
	void HandleGuildAcceptInvite(AcceptGuildInvite packet)
	{
		if (Player.GuildId == 0)
		{
			var guild = Global.GuildMgr.GetGuildById(Player.GuildIdInvited);

			if (guild)
				guild.HandleAcceptMember(this);
		}
	}

	[WorldPacketHandler(ClientOpcodes.GuildDeclineInvitation)]
	void HandleGuildDeclineInvitation(GuildDeclineInvitation packet)
	{
		if (Player.GuildId != 0)
			return;

		Player.GuildIdInvited = 0;
		Player.SetInGuild(0);
	}

	[WorldPacketHandler(ClientOpcodes.GuildPromoteMember)]
	void HandleGuildPromoteMember(GuildPromoteMember packet)
	{
		var guild = Player.Guild;

		if (guild)
			guild.HandleUpdateMemberRank(this, packet.Promotee, false);
	}

	[WorldPacketHandler(ClientOpcodes.GuildDemoteMember)]
	void HandleGuildDemoteMember(GuildDemoteMember packet)
	{
		var guild = Player.Guild;

		if (guild)
			guild.HandleUpdateMemberRank(this, packet.Demotee, true);
	}

	[WorldPacketHandler(ClientOpcodes.GuildAssignMemberRank)]
	void HandleGuildAssignRank(GuildAssignMemberRank packet)
	{
		var setterGuid = Player.GUID;

		var guild = Player.Guild;

		if (guild)
			guild.HandleSetMemberRank(this, packet.Member, setterGuid, (GuildRankOrder)packet.RankOrder);
	}

	[WorldPacketHandler(ClientOpcodes.GuildLeave)]
	void HandleGuildLeave(GuildLeave packet)
	{
		var guild = Player.Guild;

		if (guild)
			guild.HandleLeaveMember(this);
	}

	[WorldPacketHandler(ClientOpcodes.GuildDelete)]
	void HandleGuildDisband(GuildDelete packet)
	{
		var guild = Player.Guild;

		if (guild)
			guild.HandleDelete(this);
	}

	[WorldPacketHandler(ClientOpcodes.GuildUpdateMotdText)]
	void HandleGuildUpdateMotdText(GuildUpdateMotdText packet)
	{
		if (!DisallowHyperlinksAndMaybeKick(packet.MotdText))
			return;

		if (packet.MotdText.Length > 255)
			return;

		var guild = Player.Guild;

		if (guild)
			guild.HandleSetMOTD(this, packet.MotdText);
	}

	[WorldPacketHandler(ClientOpcodes.GuildSetMemberNote)]
	void HandleGuildSetMemberNote(GuildSetMemberNote packet)
	{
		if (!DisallowHyperlinksAndMaybeKick(packet.Note))
			return;

		if (packet.Note.Length > 31)
			return;

		var guild = Player.Guild;

		if (guild)
			guild.HandleSetMemberNote(this, packet.Note, packet.NoteeGUID, packet.IsPublic);
	}

	[WorldPacketHandler(ClientOpcodes.GuildAddRank)]
	void HandleGuildAddRank(GuildAddRank packet)
	{
		if (!DisallowHyperlinksAndMaybeKick(packet.Name))
			return;

		if (packet.Name.Length > 15)
			return;

		var guild = Player.Guild;

		if (guild)
			guild.HandleAddNewRank(this, packet.Name);
	}

	[WorldPacketHandler(ClientOpcodes.GuildDeleteRank)]
	void HandleGuildDeleteRank(GuildDeleteRank packet)
	{
		var guild = Player.Guild;

		if (guild)
			guild.HandleRemoveRank(this, (GuildRankOrder)packet.RankOrder);
	}

	[WorldPacketHandler(ClientOpcodes.GuildShiftRank)]
	void HandleGuildShiftRank(GuildShiftRank shiftRank)
	{
		var guild = Player.Guild;

		if (guild)
			guild.HandleShiftRank(this, (GuildRankOrder)shiftRank.RankOrder, shiftRank.ShiftUp);
	}

	[WorldPacketHandler(ClientOpcodes.GuildUpdateInfoText)]
	void HandleGuildUpdateInfoText(GuildUpdateInfoText packet)
	{
		if (!DisallowHyperlinksAndMaybeKick(packet.InfoText))
			return;

		if (packet.InfoText.Length > 500)
			return;

		var guild = Player.Guild;

		if (guild)
			guild.HandleSetInfo(this, packet.InfoText);
	}

	[WorldPacketHandler(ClientOpcodes.SaveGuildEmblem)]
	void HandleSaveGuildEmblem(SaveGuildEmblem packet)
	{
		Guild.EmblemInfo emblemInfo = new();
		emblemInfo.ReadPacket(packet);

		if (Player.GetNPCIfCanInteractWith(packet.Vendor, NPCFlags.TabardDesigner, NPCFlags2.None))
		{
			// Remove fake death
			if (Player.HasUnitState(UnitState.Died))
				Player.RemoveAurasByType(AuraType.FeignDeath);

			if (!emblemInfo.ValidateEmblemColors())
			{
				Guild.SendSaveEmblemResult(this, GuildEmblemError.InvalidTabardColors);

				return;
			}

			var guild = Player.Guild;

			if (guild)
				guild.HandleSetEmblem(this, emblemInfo);
			else
				Guild.SendSaveEmblemResult(this, GuildEmblemError.NoGuild); // "You are not part of a guild!";
		}
		else
		{
			Guild.SendSaveEmblemResult(this, GuildEmblemError.InvalidVendor); // "That's not an emblem vendor!"
		}
	}

	[WorldPacketHandler(ClientOpcodes.GuildEventLogQuery)]
	void HandleGuildEventLogQuery(GuildEventLogQuery packet)
	{
		var guild = Player.Guild;

		if (guild)
			guild.SendEventLog(this);
	}

	[WorldPacketHandler(ClientOpcodes.GuildBankDepositMoney)]
	void HandleGuildBankDepositMoney(GuildBankDepositMoney packet)
	{
		if (Player.GetGameObjectIfCanInteractWith(packet.Banker, GameObjectTypes.GuildBank))
			if (packet.Money != 0 && Player.HasEnoughMoney(packet.Money))
			{
				var guild = Player.Guild;

				if (guild)
					guild.HandleMemberDepositMoney(this, packet.Money);
			}
	}

	[WorldPacketHandler(ClientOpcodes.GuildBankWithdrawMoney)]
	void HandleGuildBankWithdrawMoney(GuildBankWithdrawMoney packet)
	{
		if (packet.Money != 0 && Player.GetGameObjectIfCanInteractWith(packet.Banker, GameObjectTypes.GuildBank))
		{
			var guild = Player.Guild;

			if (guild)
				guild.HandleMemberWithdrawMoney(this, packet.Money);
		}
	}

	[WorldPacketHandler(ClientOpcodes.AutoGuildBankItem)]
	void HandleAutoGuildBankItem(AutoGuildBankItem depositGuildBankItem)
	{
		if (!Player.GetGameObjectIfCanInteractWith(depositGuildBankItem.Banker, GameObjectTypes.GuildBank))
			return;

		var guild = Player.Guild;

		if (guild == null)
			return;

		if (!Player.IsInventoryPos(depositGuildBankItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0), depositGuildBankItem.ContainerItemSlot))
			Player.SendEquipError(InventoryResult.InternalBagError, null);
		else
			guild.SwapItemsWithInventory(Player,
										false,
										depositGuildBankItem.BankTab,
										depositGuildBankItem.BankSlot,
										depositGuildBankItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0),
										depositGuildBankItem.ContainerItemSlot,
										0);
	}

	[WorldPacketHandler(ClientOpcodes.StoreGuildBankItem)]
	void HandleStoreGuildBankItem(StoreGuildBankItem storeGuildBankItem)
	{
		if (!Player.GetGameObjectIfCanInteractWith(storeGuildBankItem.Banker, GameObjectTypes.GuildBank))
			return;

		var guild = Player.Guild;

		if (guild == null)
			return;

		if (!Player.IsInventoryPos(storeGuildBankItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0), storeGuildBankItem.ContainerItemSlot))
			Player.SendEquipError(InventoryResult.InternalBagError, null);
		else
			guild.SwapItemsWithInventory(Player,
										true,
										storeGuildBankItem.BankTab,
										storeGuildBankItem.BankSlot,
										storeGuildBankItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0),
										storeGuildBankItem.ContainerItemSlot,
										0);
	}

	[WorldPacketHandler(ClientOpcodes.SwapItemWithGuildBankItem)]
	void HandleSwapItemWithGuildBankItem(SwapItemWithGuildBankItem swapItemWithGuildBankItem)
	{
		if (!Player.GetGameObjectIfCanInteractWith(swapItemWithGuildBankItem.Banker, GameObjectTypes.GuildBank))
			return;

		var guild = Player.Guild;

		if (guild == null)
			return;

		if (!Player.IsInventoryPos(swapItemWithGuildBankItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0), swapItemWithGuildBankItem.ContainerItemSlot))
			Player.SendEquipError(InventoryResult.InternalBagError, null);
		else
			guild.SwapItemsWithInventory(Player,
										false,
										swapItemWithGuildBankItem.BankTab,
										swapItemWithGuildBankItem.BankSlot,
										swapItemWithGuildBankItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0),
										swapItemWithGuildBankItem.ContainerItemSlot,
										0);
	}

	[WorldPacketHandler(ClientOpcodes.SwapGuildBankItemWithGuildBankItem)]
	void HandleSwapGuildBankItemWithGuildBankItem(SwapGuildBankItemWithGuildBankItem swapGuildBankItemWithGuildBankItem)
	{
		if (!Player.GetGameObjectIfCanInteractWith(swapGuildBankItemWithGuildBankItem.Banker, GameObjectTypes.GuildBank))
			return;

		var guild = Player.Guild;

		if (guild == null)
			return;

		guild.SwapItems(Player,
						swapGuildBankItemWithGuildBankItem.BankTab[0],
						swapGuildBankItemWithGuildBankItem.BankSlot[0],
						swapGuildBankItemWithGuildBankItem.BankTab[1],
						swapGuildBankItemWithGuildBankItem.BankSlot[1],
						0);
	}

	[WorldPacketHandler(ClientOpcodes.MoveGuildBankItem)]
	void HandleMoveGuildBankItem(MoveGuildBankItem moveGuildBankItem)
	{
		if (!Player.GetGameObjectIfCanInteractWith(moveGuildBankItem.Banker, GameObjectTypes.GuildBank))
			return;

		var guild = Player.Guild;

		if (guild == null)
			return;

		guild.SwapItems(Player, moveGuildBankItem.BankTab, moveGuildBankItem.BankSlot, moveGuildBankItem.BankTab1, moveGuildBankItem.BankSlot1, 0);
	}

	[WorldPacketHandler(ClientOpcodes.MergeItemWithGuildBankItem)]
	void HandleMergeItemWithGuildBankItem(MergeItemWithGuildBankItem mergeItemWithGuildBankItem)
	{
		if (!Player.GetGameObjectIfCanInteractWith(mergeItemWithGuildBankItem.Banker, GameObjectTypes.GuildBank))
			return;

		var guild = Player.Guild;

		if (guild == null)
			return;

		if (!Player.IsInventoryPos(mergeItemWithGuildBankItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0), mergeItemWithGuildBankItem.ContainerItemSlot))
			Player.SendEquipError(InventoryResult.InternalBagError, null);
		else
			guild.SwapItemsWithInventory(Player,
										false,
										mergeItemWithGuildBankItem.BankTab,
										mergeItemWithGuildBankItem.BankSlot,
										mergeItemWithGuildBankItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0),
										mergeItemWithGuildBankItem.ContainerItemSlot,
										mergeItemWithGuildBankItem.StackCount);
	}

	[WorldPacketHandler(ClientOpcodes.SplitItemToGuildBank)]
	void HandleSplitItemToGuildBank(SplitItemToGuildBank splitItemToGuildBank)
	{
		if (!Player.GetGameObjectIfCanInteractWith(splitItemToGuildBank.Banker, GameObjectTypes.GuildBank))
			return;

		var guild = Player.Guild;

		if (guild == null)
			return;

		if (!Player.IsInventoryPos(splitItemToGuildBank.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0), splitItemToGuildBank.ContainerItemSlot))
			Player.SendEquipError(InventoryResult.InternalBagError, null);
		else
			guild.SwapItemsWithInventory(Player,
										false,
										splitItemToGuildBank.BankTab,
										splitItemToGuildBank.BankSlot,
										splitItemToGuildBank.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0),
										splitItemToGuildBank.ContainerItemSlot,
										splitItemToGuildBank.StackCount);
	}

	[WorldPacketHandler(ClientOpcodes.MergeGuildBankItemWithItem)]
	void HandleMergeGuildBankItemWithItem(MergeGuildBankItemWithItem mergeGuildBankItemWithItem)
	{
		if (!Player.GetGameObjectIfCanInteractWith(mergeGuildBankItemWithItem.Banker, GameObjectTypes.GuildBank))
			return;

		var guild = Player.Guild;

		if (guild == null)
			return;

		if (!Player.IsInventoryPos(mergeGuildBankItemWithItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0), mergeGuildBankItemWithItem.ContainerItemSlot))
			Player.SendEquipError(InventoryResult.InternalBagError, null);
		else
			guild.SwapItemsWithInventory(Player,
										true,
										mergeGuildBankItemWithItem.BankTab,
										mergeGuildBankItemWithItem.BankSlot,
										mergeGuildBankItemWithItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0),
										mergeGuildBankItemWithItem.ContainerItemSlot,
										mergeGuildBankItemWithItem.StackCount);
	}

	[WorldPacketHandler(ClientOpcodes.SplitGuildBankItemToInventory)]
	void HandleSplitGuildBankItemToInventory(SplitGuildBankItemToInventory splitGuildBankItemToInventory)
	{
		if (!Player.GetGameObjectIfCanInteractWith(splitGuildBankItemToInventory.Banker, GameObjectTypes.GuildBank))
			return;

		var guild = Player.Guild;

		if (guild == null)
			return;

		if (!Player.IsInventoryPos(splitGuildBankItemToInventory.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0), splitGuildBankItemToInventory.ContainerItemSlot))
			Player.SendEquipError(InventoryResult.InternalBagError, null);
		else
			guild.SwapItemsWithInventory(Player,
										true,
										splitGuildBankItemToInventory.BankTab,
										splitGuildBankItemToInventory.BankSlot,
										splitGuildBankItemToInventory.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0),
										splitGuildBankItemToInventory.ContainerItemSlot,
										splitGuildBankItemToInventory.StackCount);
	}

	[WorldPacketHandler(ClientOpcodes.AutoStoreGuildBankItem)]
	void HandleAutoStoreGuildBankItem(AutoStoreGuildBankItem autoStoreGuildBankItem)
	{
		if (!Player.GetGameObjectIfCanInteractWith(autoStoreGuildBankItem.Banker, GameObjectTypes.GuildBank))
			return;

		var guild = Player.Guild;

		if (guild == null)
			return;

		guild.SwapItemsWithInventory(Player, true, autoStoreGuildBankItem.BankTab, autoStoreGuildBankItem.BankSlot, InventorySlots.Bag0, ItemConst.NullSlot, 0);
	}

	[WorldPacketHandler(ClientOpcodes.MergeGuildBankItemWithGuildBankItem)]
	void HandleMergeGuildBankItemWithGuildBankItem(MergeGuildBankItemWithGuildBankItem mergeGuildBankItemWithGuildBankItem)
	{
		if (!Player.GetGameObjectIfCanInteractWith(mergeGuildBankItemWithGuildBankItem.Banker, GameObjectTypes.GuildBank))
			return;

		var guild = Player.Guild;

		if (guild == null)
			return;

		guild.SwapItems(Player,
						mergeGuildBankItemWithGuildBankItem.BankTab,
						mergeGuildBankItemWithGuildBankItem.BankSlot,
						mergeGuildBankItemWithGuildBankItem.BankTab1,
						mergeGuildBankItemWithGuildBankItem.BankSlot1,
						mergeGuildBankItemWithGuildBankItem.StackCount);
	}

	[WorldPacketHandler(ClientOpcodes.SplitGuildBankItem)]
	void HandleSplitGuildBankItem(SplitGuildBankItem splitGuildBankItem)
	{
		if (!Player.GetGameObjectIfCanInteractWith(splitGuildBankItem.Banker, GameObjectTypes.GuildBank))
			return;

		var guild = Player.Guild;

		if (guild == null)
			return;

		guild.SwapItems(Player,
						splitGuildBankItem.BankTab,
						splitGuildBankItem.BankSlot,
						splitGuildBankItem.BankTab1,
						splitGuildBankItem.BankSlot1,
						splitGuildBankItem.StackCount);
	}

	[WorldPacketHandler(ClientOpcodes.GuildBankBuyTab)]
	void HandleGuildBankBuyTab(GuildBankBuyTab packet)
	{
		if (packet.Banker.IsEmpty || Player.GetGameObjectIfCanInteractWith(packet.Banker, GameObjectTypes.GuildBank))
		{
			var guild = Player.Guild;

			if (guild)
				guild.HandleBuyBankTab(this, packet.BankTab);
		}
	}

	[WorldPacketHandler(ClientOpcodes.GuildBankUpdateTab)]
	void HandleGuildBankUpdateTab(GuildBankUpdateTab packet)
	{
		if (!DisallowHyperlinksAndMaybeKick(packet.Name))
			return;

		if ((packet.Name.Length > 15) || (packet.Icon.Length > 127))
			return;

		if (!string.IsNullOrEmpty(packet.Name) && !string.IsNullOrEmpty(packet.Icon))
			if (Player.GetGameObjectIfCanInteractWith(packet.Banker, GameObjectTypes.GuildBank))
			{
				var guild = Player.Guild;

				if (guild)
					guild.HandleSetBankTabInfo(this, packet.BankTab, packet.Name, packet.Icon);
			}
	}

	[WorldPacketHandler(ClientOpcodes.GuildBankLogQuery)]
	void HandleGuildBankLogQuery(GuildBankLogQuery packet)
	{
		var guild = Player.Guild;

		if (guild)
			guild.SendBankLog(this, (byte)packet.Tab);
	}

	[WorldPacketHandler(ClientOpcodes.GuildBankTextQuery)]
	void HandleGuildBankTextQuery(GuildBankTextQuery packet)
	{
		var guild = Player.Guild;

		if (guild)
			guild.SendBankTabText(this, (byte)packet.Tab);
	}

	[WorldPacketHandler(ClientOpcodes.GuildBankSetTabText)]
	void HandleGuildBankSetTabText(GuildBankSetTabText packet)
	{
		if (!DisallowHyperlinksAndMaybeKick(packet.TabText))
			return;

		if (packet.TabText.Length > 500)
			return;

		var guild = Player.Guild;

		if (guild)
			guild.SetBankTabText((byte)packet.Tab, packet.TabText);
	}

	[WorldPacketHandler(ClientOpcodes.GuildSetRankPermissions)]
	void HandleGuildSetRankPermissions(GuildSetRankPermissions packet)
	{
		if (!DisallowHyperlinksAndMaybeKick(packet.RankName))
			return;

		if (packet.RankName.Length > 15)
			return;

		var guild = Player.Guild;

		if (guild == null)
			return;

		var rightsAndSlots = new Guild.GuildBankRightsAndSlots[GuildConst.MaxBankTabs];

		for (byte tabId = 0; tabId < GuildConst.MaxBankTabs; ++tabId)
			rightsAndSlots[tabId] = new Guild.GuildBankRightsAndSlots(tabId, (sbyte)packet.TabFlags[tabId], (int)packet.TabWithdrawItemLimit[tabId]);

		guild.HandleSetRankInfo(this, (GuildRankId)packet.RankID, packet.RankName, (GuildRankRights)packet.Flags, packet.WithdrawGoldLimit, rightsAndSlots);
	}

	[WorldPacketHandler(ClientOpcodes.GuildChangeNameRequest, Processing = PacketProcessing.Inplace)]
	void HandleGuildChallengeUpdateRequest(GuildChallengeUpdateRequest packet)
	{
		var guild = Player.Guild;

		if (guild)
			guild.HandleGuildRequestChallengeUpdate(this);
	}

	[WorldPacketHandler(ClientOpcodes.DeclineGuildInvites)]
	void HandleDeclineGuildInvites(DeclineGuildInvites packet)
	{
		if (packet.Allow)
			Player.SetPlayerFlag(PlayerFlags.AutoDeclineGuild);
		else
			Player.RemovePlayerFlag(PlayerFlags.AutoDeclineGuild);
	}

	[WorldPacketHandler(ClientOpcodes.GuildQueryNews)]
	void HandleGuildQueryNews(GuildQueryNews packet)
	{
		var guild = Player.Guild;

		if (guild)
			if (guild.GetGUID() == packet.GuildGUID)
				guild.SendNewsUpdate(this);
	}

	[WorldPacketHandler(ClientOpcodes.GuildNewsUpdateSticky)]
	void HandleGuildNewsUpdateSticky(GuildNewsUpdateSticky packet)
	{
		var guild = Player.Guild;

		if (guild)
			guild.HandleNewsSetSticky(this, (uint)packet.NewsID, packet.Sticky);
	}

	[WorldPacketHandler(ClientOpcodes.GuildReplaceGuildMaster)]
	void HandleGuildReplaceGuildMaster(GuildReplaceGuildMaster replaceGuildMaster)
	{
		var guild = Player.Guild;

		if (guild)
			guild.HandleSetNewGuildMaster(this, "", true);
	}

	[WorldPacketHandler(ClientOpcodes.GuildSetGuildMaster)]
	void HandleGuildSetGuildMaster(GuildSetGuildMaster packet)
	{
		var guild = Player.Guild;

		if (guild)
			guild.HandleSetNewGuildMaster(this, packet.NewMasterName, false);
	}

	[WorldPacketHandler(ClientOpcodes.GuildSetAchievementTracking)]
	void HandleGuildSetAchievementTracking(GuildSetAchievementTracking packet)
	{
		var guild = Player.Guild;

		if (guild)
			guild.HandleSetAchievementTracking(this, packet.AchievementIDs);
	}

	[WorldPacketHandler(ClientOpcodes.GuildGetAchievementMembers)]
	void HandleGuildGetAchievementMembers(GuildGetAchievementMembers getAchievementMembers)
	{
		var guild = Player.Guild;

		if (guild)
			guild.HandleGetAchievementMembers(this, getAchievementMembers.AchievementID);
	}
}
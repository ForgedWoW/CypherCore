// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Guilds;
using Game.Networking;
using Game.Networking.Packets;

namespace Game;

public partial class WorldSession
{
	[WorldPacketHandler(ClientOpcodes.QueryGuildInfo, Status = SessionStatus.Authed)]
	void HandleGuildQuery(QueryGuildInfo query)
	{
		var guild = Global.GuildMgr.GetGuildByGuid(query.GuildGuid);

		if (guild)
		{
			guild.SendQueryResponse(this);

			return;
		}

		QueryGuildInfoResponse response = new();
		response.GuildGUID = query.GuildGuid;
		SendPacket(response);
	}


	[WorldPacketHandler(ClientOpcodes.GuildGetRoster)]
	void HandleGuildGetRoster(GuildGetRoster packet)
	{
		var guild = Player.Guild;

		if (guild)
			guild.HandleRoster(this);
		else
			Guild.SendCommandResult(this, GuildCommandType.GetRoster, GuildCommandError.PlayerNotInGuild);
	}

	[WorldPacketHandler(ClientOpcodes.GuildGetRanks)]
	void HandleGuildGetRanks(GuildGetRanks packet)
	{
		var guild = Global.GuildMgr.GetGuildByGuid(packet.GuildGUID);

		if (guild)
			if (guild.IsMember(Player.GUID))
				guild.SendGuildRankInfo(this);
	}

	[WorldPacketHandler(ClientOpcodes.GuildBankRemainingWithdrawMoneyQuery)]
	void HandleGuildBankMoneyWithdrawn(GuildBankRemainingWithdrawMoneyQuery packet)
	{
		var guild = Player.Guild;

		if (guild)
			guild.SendMoneyInfo(this);
	}

	[WorldPacketHandler(ClientOpcodes.GuildPermissionsQuery)]
	void HandleGuildPermissionsQuery(GuildPermissionsQuery packet)
	{
		var guild = Player.Guild;

		if (guild)
			guild.SendPermissions(this);
	}

	[WorldPacketHandler(ClientOpcodes.GuildBankActivate)]
	void HandleGuildBankActivate(GuildBankActivate packet)
	{
		var go = Player.GetGameObjectIfCanInteractWith(packet.Banker, GameObjectTypes.GuildBank);

		if (go == null)
			return;

		var guild = Player.Guild;

		if (guild == null)
		{
			Guild.SendCommandResult(this, GuildCommandType.ViewTab, GuildCommandError.PlayerNotInGuild);

			return;
		}

		guild.SendBankList(this, 0, packet.FullUpdate);
	}

	[WorldPacketHandler(ClientOpcodes.GuildBankQueryTab)]
	void HandleGuildBankQueryTab(GuildBankQueryTab packet)
	{
		if (Player.GetGameObjectIfCanInteractWith(packet.Banker, GameObjectTypes.GuildBank))
		{
			var guild = Player.Guild;

			if (guild)
				guild.SendBankList(this, packet.Tab, true /*packet.FullUpdate*/);
			// HACK: client doesn't query entire tab content if it had received SMSG_GUILD_BANK_LIST in this session
			// but we broadcast bank updates to entire guild when *ANYONE* changes anything, incorrectly initializing clients
			// tab content with only data for that change
		}
	}

	[WorldPacketHandler(ClientOpcodes.RequestGuildPartyState)]
	void HandleGuildRequestPartyState(RequestGuildPartyState packet)
	{
		var guild = Global.GuildMgr.GetGuildByGuid(packet.GuildGUID);

		if (guild)
			guild.HandleGuildPartyRequest(this);
	}



	[WorldPacketHandler(ClientOpcodes.RequestGuildRewardsList)]
	void HandleRequestGuildRewardsList(RequestGuildRewardsList packet)
	{
		if (Global.GuildMgr.GetGuildById(Player.GuildId))
		{
			var rewards = Global.GuildMgr.GetGuildRewards();

			GuildRewardList rewardList = new();
			rewardList.Version = GameTime.GetGameTime();

			for (var i = 0; i < rewards.Count; i++)
			{
				GuildRewardItem rewardItem = new();
				rewardItem.ItemID = rewards[i].ItemID;
				rewardItem.RaceMask = (uint)rewards[i].RaceMask;
				rewardItem.MinGuildLevel = 0;
				rewardItem.MinGuildRep = rewards[i].MinGuildRep;
				rewardItem.AchievementsRequired = rewards[i].AchievementsRequired;
				rewardItem.Cost = rewards[i].Cost;
				rewardList.RewardItems.Add(rewardItem);
			}

			SendPacket(rewardList);
		}
	}
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Guilds;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Guild;
using Game.Common.Server;

namespace Game.Common.Handlers;

public class GuildHandler
{
    private readonly WorldSession _session;
    private readonly GuildManager _guildManager;

    public GuildHandler(WorldSession session, GuildManager guildManager)
    {
        _session = session;
        _guildManager = guildManager;
    }


    [WorldPacketHandler(ClientOpcodes.QueryGuildInfo, Status = SessionStatus.Authed)]
	void HandleGuildQuery(QueryGuildInfo query)
	{
		var guild = _guildManager.GetGuildByGuid(query.GuildGuid);

		if (guild)
		{
			guild.SendQueryResponse(_session);

			return;
		}

		QueryGuildInfoResponse response = new();
		response.GuildGUID = query.GuildGuid;
        _session.SendPacket(response);
	}


	[WorldPacketHandler(ClientOpcodes.GuildGetRoster)]
	void HandleGuildGetRoster(GuildGetRoster packet)
	{
		var guild = _session.Player.Guild;

		if (guild)
			guild.HandleRoster(_session);
		else
			Guild.SendCommandResult(_session, GuildCommandType.GetRoster, GuildCommandError.PlayerNotInGuild);
	}

	[WorldPacketHandler(ClientOpcodes.GuildGetRanks)]
	void HandleGuildGetRanks(GuildGetRanks packet)
	{
		var guild = _guildManager.GetGuildByGuid(packet.GuildGUID);

		if (guild)
			if (guild.IsMember(_session.Player.GUID))
				guild.SendGuildRankInfo(_session);
	}

	[WorldPacketHandler(ClientOpcodes.GuildBankRemainingWithdrawMoneyQuery)]
	void HandleGuildBankMoneyWithdrawn(GuildBankRemainingWithdrawMoneyQuery packet)
	{
		var guild = _session.Player.Guild;

		if (guild)
			guild.SendMoneyInfo(_session);
	}

	[WorldPacketHandler(ClientOpcodes.GuildPermissionsQuery)]
	void HandleGuildPermissionsQuery(GuildPermissionsQuery packet)
	{
		var guild = _session.Player.Guild;

		if (guild)
			guild.SendPermissions(_session);
	}

	[WorldPacketHandler(ClientOpcodes.GuildBankActivate)]
	void HandleGuildBankActivate(GuildBankActivate packet)
	{
		var go = _session.Player.GetGameObjectIfCanInteractWith(packet.Banker, GameObjectTypes.GuildBank);

		if (go == null)
			return;

		var guild = _session.Player.Guild;

		if (guild == null)
		{
			Guild.SendCommandResult(_session, GuildCommandType.ViewTab, GuildCommandError.PlayerNotInGuild);

			return;
		}

		guild.SendBankList(_session, 0, packet.FullUpdate);
	}

	[WorldPacketHandler(ClientOpcodes.GuildBankQueryTab)]
	void HandleGuildBankQueryTab(GuildBankQueryTab packet)
	{
		if (_session.Player.GetGameObjectIfCanInteractWith(packet.Banker, GameObjectTypes.GuildBank))
		{
			var guild = _session.Player.Guild;

			if (guild)
				guild.SendBankList(_session, packet.Tab, true /*packet.FullUpdate*/);
			// HACK: client doesn't query entire tab content if it had received SMSG_GUILD_BANK_LIST in this session
			// but we broadcast bank updates to entire guild when *ANYONE* changes anything, incorrectly initializing clients
			// tab content with only data for that change
		}
	}

	[WorldPacketHandler(ClientOpcodes.RequestGuildPartyState)]
	void HandleGuildRequestPartyState(RequestGuildPartyState packet)
	{
		var guild = _guildManager.GetGuildByGuid(packet.GuildGUID);

		if (guild)
			guild.HandleGuildPartyRequest(_session);
	}



	[WorldPacketHandler(ClientOpcodes.RequestGuildRewardsList)]
	void HandleRequestGuildRewardsList(RequestGuildRewardsList packet)
	{
		if (_guildManager.GetGuildById(_session.Player.GuildId))
		{
			var rewards = _guildManager.GetGuildRewards();

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

            _session.SendPacket(rewardList);
		}
	}
}

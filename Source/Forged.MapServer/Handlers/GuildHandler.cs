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

    [WorldPacketHandler(ClientOpcodes.GuildGetRoster)]
    void HandleGuildGetRoster(GuildGetRoster packet)
    {
        var guild = Player.Guild;

        if (guild)
            guild.HandleRoster(this);
        else
            Guild.SendCommandResult(this, GuildCommandType.GetRoster, GuildCommandError.PlayerNotInGuild);
    }
}
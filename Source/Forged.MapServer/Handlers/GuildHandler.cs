// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Guilds;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Guild;
using Framework.Constants;
using Game.Common.Handlers;

namespace Forged.MapServer.Handlers;

public class GuildHandler : IWorldSessionHandler
{
    [WorldPacketHandler(ClientOpcodes.GuildGetRanks)]
    private void HandleGuildGetRanks(GuildGetRanks packet)
    {
        var guild = Global.GuildMgr.GetGuildByGuid(packet.GuildGUID);

        if (guild)
            if (guild.IsMember(Player.GUID))
                guild.SendGuildRankInfo(this);
    }

    [WorldPacketHandler(ClientOpcodes.GuildBankRemainingWithdrawMoneyQuery)]
    private void HandleGuildBankMoneyWithdrawn(GuildBankRemainingWithdrawMoneyQuery packet)
    {
        var guild = Player.Guild;

        if (guild)
            guild.SendMoneyInfo(this);
    }

    [WorldPacketHandler(ClientOpcodes.GuildPermissionsQuery)]
    private void HandleGuildPermissionsQuery(GuildPermissionsQuery packet)
    {
        var guild = Player.Guild;

        if (guild)
            guild.SendPermissions(this);
    }

    [WorldPacketHandler(ClientOpcodes.GuildGetRoster)]
    private void HandleGuildGetRoster(GuildGetRoster packet)
    {
        var guild = Player.Guild;

        if (guild)
            guild.HandleRoster(this);
        else
            Guild.SendCommandResult(this, GuildCommandType.GetRoster, GuildCommandError.PlayerNotInGuild);
    }
}
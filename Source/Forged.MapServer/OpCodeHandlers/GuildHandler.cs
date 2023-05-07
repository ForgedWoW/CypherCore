// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Guilds;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Guild;
using Forged.MapServer.Server;
using Framework.Constants;
using Game.Common.Handlers;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class GuildHandler : IWorldSessionHandler
{
    private readonly GuildManager _guildManager;
    private readonly WorldSession _session;

    public GuildHandler(WorldSession session, GuildManager guildManager)
    {
        _session = session;
        _guildManager = guildManager;
    }

    [WorldPacketHandler(ClientOpcodes.GuildBankRemainingWithdrawMoneyQuery)]
    private void HandleGuildBankMoneyWithdrawn(GuildBankRemainingWithdrawMoneyQuery packet)
    {
        if (packet != null)
            _session.Player.Guild?.SendMoneyInfo(_session);
    }

    [WorldPacketHandler(ClientOpcodes.GuildGetRanks)]
    private void HandleGuildGetRanks(GuildGetRanks packet)
    {
        var guild = _guildManager.GetGuildByGuid(packet.GuildGUID);

        if (guild == null)
            return;

        if (guild.IsMember(_session.Player.GUID))
            guild.SendGuildRankInfo(_session);
    }

    [WorldPacketHandler(ClientOpcodes.GuildGetRoster)]
    private void HandleGuildGetRoster(GuildGetRoster packet)
    {
        if (_session.Player.Guild != null && packet != null)
            _session.Player.Guild.HandleRoster(_session);
        else
            Guild.SendCommandResult(_session, GuildCommandType.GetRoster, GuildCommandError.PlayerNotInGuild);
    }

    [WorldPacketHandler(ClientOpcodes.GuildPermissionsQuery)]
    private void HandleGuildPermissionsQuery(GuildPermissionsQuery packet)
    {
        if (packet != null)
            _session.Player.Guild?.SendPermissions(_session);
    }
}
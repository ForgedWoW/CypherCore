// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Linq;
using Forged.MapServer.Chat.Channels;
using Framework.Constants;
using Framework.Database;

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("channel")]
internal class ChannelCommands
{
    [CommandGroup("set")]
    private class ChannelSetCommands
    {
        [Command("ownership", RBACPermissions.CommandChannelSetOwnership)]
        private static bool HandleChannelSetOwnership(CommandHandler handler, string channelName, bool grantOwnership)
        {
            var channelId = (from channelEntry
                                 in
                                 handler.CliDB.ChatChannelsStorage.Values
                             where
                                 channelEntry.Name[handler.SessionDbcLocale].Equals(channelName, StringComparison.OrdinalIgnoreCase)
                             select
                                 channelEntry.Id).FirstOrDefault();

            var zoneEntry = handler.CliDB.AreaTableStorage.Values.FirstOrDefault(entry => entry.AreaName[handler.SessionDbcLocale].Equals(channelName, StringComparison.OrdinalIgnoreCase));

            var player = handler.Session.Player;
            Channel channel = null;
            var charDB = handler.ClassFactory.Resolve<CharacterDatabase>();
            var cMgr = handler.ClassFactory.Resolve<ChannelManagerFactory>().ForTeam(player.Team);

            if (cMgr != null)
                channel = cMgr.GetChannel(channelId, channelName, player, false, zoneEntry);

            if (grantOwnership)
            {
                channel?.SetOwnership(true);

                var stmt = charDB.GetPreparedStatement(CharStatements.UPD_CHANNEL_OWNERSHIP);
                stmt.AddValue(0, 1);
                stmt.AddValue(1, channelName);
                charDB.Execute(stmt);
                handler.SendSysMessage(CypherStrings.ChannelEnableOwnership, channelName);
            }
            else
            {
                channel?.SetOwnership(false);

                var stmt = charDB.GetPreparedStatement(CharStatements.UPD_CHANNEL_OWNERSHIP);
                stmt.AddValue(0, 0);
                stmt.AddValue(1, channelName);
                charDB.Execute(stmt);
                handler.SendSysMessage(CypherStrings.ChannelDisableOwnership, channelName);
            }

            return true;
        }
    }
}
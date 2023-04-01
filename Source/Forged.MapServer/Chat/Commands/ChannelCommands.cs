// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Chat.Channels;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.A;
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
            uint channelId = 0;

            foreach (var channelEntry in CliDB.ChatChannelsStorage.Values)
                if (channelEntry.Name[handler.SessionDbcLocale].Equals(channelName, StringComparison.OrdinalIgnoreCase))
                {
                    channelId = channelEntry.Id;

                    break;
                }

            AreaTableRecord zoneEntry = null;

            foreach (var entry in CliDB.AreaTableStorage.Values)
                if (entry.AreaName[handler.SessionDbcLocale].Equals(channelName, StringComparison.OrdinalIgnoreCase))
                {
                    zoneEntry = entry;

                    break;
                }

            var player = handler.Session.Player;
            Channel channel = null;

            var cMgr = ChannelManager.ForTeam(player.Team);

            if (cMgr != null)
                channel = cMgr.GetChannel(channelId, channelName, player, false, zoneEntry);

            if (grantOwnership)
            {
                channel?.SetOwnership(true);

                var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHANNEL_OWNERSHIP);
                stmt.AddValue(0, 1);
                stmt.AddValue(1, channelName);
                DB.Characters.Execute(stmt);
                handler.SendSysMessage(CypherStrings.ChannelEnableOwnership, channelName);
            }
            else
            {
                channel?.SetOwnership(false);

                var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHANNEL_OWNERSHIP);
                stmt.AddValue(0, 0);
                stmt.AddValue(1, channelName);
                DB.Characters.Execute(stmt);
                handler.SendSysMessage(CypherStrings.ChannelDisableOwnership, channelName);
            }

            return true;
        }
    }
}
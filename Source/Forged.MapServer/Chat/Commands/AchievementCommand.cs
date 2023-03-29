// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.Structs.A;
using Framework.Constants;

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("achievement")]
internal class AchievementCommand
{
    [Command("add", CypherStrings.CommandAchievementAddHelp, RBACPermissions.CommandAchievementAdd)]
    private static bool HandleAchievementAddCommand(CommandHandler handler, AchievementRecord achievementEntry)
    {
        var target = handler.SelectedPlayer;

        if (!target)
        {
            handler.SendSysMessage(CypherStrings.NoCharSelected);

            return false;
        }

        target.CompletedAchievement(achievementEntry);

        return true;
    }
}
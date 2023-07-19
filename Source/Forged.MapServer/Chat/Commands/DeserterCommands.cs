// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

// ReSharper disable UnusedType.Local
// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("deserter")]
internal class DeserterCommands
{
    private static bool HandleDeserterAdd(CommandHandler handler, uint time, bool isInstance)
    {
        var player = handler.SelectedPlayer;

        if (player == null)
        {
            handler.SendSysMessage(CypherStrings.NoCharSelected);

            return false;
        }

        var aura = player.AddAura(isInstance ? Spells.LFG_DUNDEON_DESERTER : Spells.BG_DESERTER, player);

        if (aura == null)
        {
            handler.SendSysMessage(CypherStrings.BadValue);

            return false;
        }

        aura.SetDuration((int)(time * Time.IN_MILLISECONDS));

        return true;
    }

    private static bool HandleDeserterRemove(CommandHandler handler, bool isInstance)
    {
        var player = handler.SelectedPlayer;

        if (player == null)
        {
            handler.SendSysMessage(CypherStrings.NoCharSelected);

            return false;
        }

        player.RemoveAura(isInstance ? Spells.LFG_DUNDEON_DESERTER : Spells.BG_DESERTER);

        return true;
    }

    [CommandGroup("bg")]
    private class DeserterBGCommands
    {
        [Command("add", RBACPermissions.CommandDeserterBgAdd)]
        private static bool HandleDeserterBGAdd(CommandHandler handler, uint time)
        {
            return HandleDeserterAdd(handler, time, false);
        }

        [Command("remove", RBACPermissions.CommandDeserterBgRemove)]
        private static bool HandleDeserterBGRemove(CommandHandler handler)
        {
            return HandleDeserterRemove(handler, false);
        }
    }

    [CommandGroup("instance")]
    private class DeserterInstanceCommands
    {
        [Command("add", RBACPermissions.CommandDeserterInstanceAdd)]
        private static bool HandleDeserterInstanceAdd(CommandHandler handler, uint time)
        {
            return HandleDeserterAdd(handler, time, true);
        }

        [Command("remove", RBACPermissions.CommandDeserterInstanceRemove)]
        private static bool HandleDeserterInstanceRemove(CommandHandler handler)
        {
            return HandleDeserterRemove(handler, true);
        }
    }
}
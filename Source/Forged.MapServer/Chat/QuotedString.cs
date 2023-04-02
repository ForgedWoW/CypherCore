// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Forged.MapServer.Chat;

internal struct QuotedString
{
    private string _str;

    public static implicit operator string(QuotedString quotedString)
    {
        return quotedString._str;
    }

    public bool IsEmpty()
    {
        return _str.IsEmpty();
    }
    public ChatCommandResult TryConsume(CommandHandler handler, string args)
    {
        _str = "";

        if (args.IsEmpty())
            return ChatCommandResult.FromErrorMessage("");

        if ((args[0] != '"') && (args[0] != '\''))
            return CommandArgs.TryConsume(out var str, typeof(string), handler, args);

        var QUOTE = args[0];

        for (var i = 1; i < args.Length; ++i)
        {
            if (args[i] == QUOTE)
            {
                var (remainingToken, tail) = args.Substring(i + 1).Tokenize();

                if (remainingToken.IsEmpty()) // if this is not empty, then we did not consume the full token
                    return new ChatCommandResult(tail);
                else
                    return ChatCommandResult.FromErrorMessage("");
            }

            if (args[i] == '\\')
            {
                ++i;

                if (!(i < args.Length))
                    break;
            }

            _str += args[i];
        }

        // if we reach this, we did not find a closing quote
        return ChatCommandResult.FromErrorMessage("");
    }
}
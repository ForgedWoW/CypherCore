// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Forged.MapServer.Chat;

internal struct Tail
{
    private string _str;

    public static implicit operator string(Tail tail)
    {
        return tail._str;
    }

    public bool IsEmpty()
    {
        return _str.IsEmpty();
    }
    public ChatCommandResult TryConsume(CommandHandler handler, string args)
    {
        _str = args;

        return new ChatCommandResult(_str);
    }
}
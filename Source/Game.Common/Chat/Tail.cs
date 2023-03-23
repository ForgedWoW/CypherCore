// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Chat;

namespace Game.Common.Chat;

struct Tail
{
	string _str;

	public bool IsEmpty()
	{
		return _str.IsEmpty();
	}

	public static implicit operator string(Tail tail)
	{
		return tail._str;
	}

	public ChatCommandResult TryConsume(CommandHandler handler, string args)
	{
		_str = args;

		return new ChatCommandResult(_str);
	}
}

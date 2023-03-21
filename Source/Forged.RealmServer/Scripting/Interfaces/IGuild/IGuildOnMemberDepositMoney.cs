﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;
using Forged.RealmServer.Guilds;

namespace Forged.RealmServer.Scripting.Interfaces.IGuild;

public interface IGuildOnMemberDepositMoney : IScriptObject
{
	void OnMemberDepositMoney(Guild guild, Player player, ulong amount);
}
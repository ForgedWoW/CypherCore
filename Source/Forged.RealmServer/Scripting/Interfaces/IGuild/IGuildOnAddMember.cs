﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Forged.RealmServer.Guilds;
using Game.Common.Entities.Players;

namespace Forged.RealmServer.Scripting.Interfaces.IGuild;

public interface IGuildOnAddMember : IScriptObject
{
	void OnAddMember(Guild guild, Player player, byte plRank);
}
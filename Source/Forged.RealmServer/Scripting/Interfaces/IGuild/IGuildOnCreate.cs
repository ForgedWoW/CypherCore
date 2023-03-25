// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;
using Forged.RealmServer.Guilds;
using Forged.RealmServer.Entities.Players;

namespace Forged.RealmServer.Scripting.Interfaces.IGuild;

public interface IGuildOnCreate : IScriptObject
{
	void OnCreate(Guild guild, Player leader, string name);
}
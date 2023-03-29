// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Guilds;

namespace Forged.MapServer.Scripting.Interfaces.IGuild;

public interface IGuildOnCreate : IScriptObject
{
    void OnCreate(Guild guild, Player leader, string name);
}
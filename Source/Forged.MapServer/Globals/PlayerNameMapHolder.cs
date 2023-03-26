// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Players;

namespace Forged.MapServer.Globals;

internal class PlayerNameMapHolder
{
    private static readonly Dictionary<string, Player> PlayerNameMap = new();

    public static void Insert(Player p)
    {
        PlayerNameMap[p.GetName()] = p;
    }

    public static void Remove(Player p)
    {
        PlayerNameMap.Remove(p.GetName());
    }

    public static Player Find(string name)
    {
        if (!GameObjectManager.NormalizePlayerName(ref name))
            return null;

        return PlayerNameMap.LookupByKey(name);
    }
}
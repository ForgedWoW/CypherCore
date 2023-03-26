using System.Collections.Generic;
using Forged.MapServer.Entities.Players;

namespace Forged.MapServer.Globals;

class PlayerNameMapHolder
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
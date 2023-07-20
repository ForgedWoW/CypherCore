// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Players;

namespace Forged.MapServer.Globals;

public class PlayerNameMapHolder
{
    private readonly GameObjectManager _gameObjectManager;
    private readonly Dictionary<string, Player> _playerNameMap = new();

    public PlayerNameMapHolder(GameObjectManager gameObjectManager)
    {
        _gameObjectManager = gameObjectManager;
    }

    public Player Find(string name)
    {
        return !_gameObjectManager.NormalizePlayerName(ref name) ? null : _playerNameMap.LookupByKey(name);
    }

    public void Insert(Player p)
    {
        _playerNameMap[p.GetName()] = p;
    }

    public void Remove(Player p)
    {
        _playerNameMap.Remove(p.GetName());
    }
}
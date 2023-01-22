﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;
using Game.Maps;
using Game.Scripting.Interfaces;

namespace Game.Scripting.Interfaces.IMap
{
    public interface IMapOnPlayerEnter<T> : IScriptObject where T : Map
    {
        void OnPlayerEnter(T map, Player player);
    }
}
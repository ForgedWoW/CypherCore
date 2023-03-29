﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Maps;

namespace Forged.MapServer.Scripting.Interfaces.IMap;

public interface IMapOnCreate<T> : IScriptObject where T : Map
{
    void OnCreate(T map);
}
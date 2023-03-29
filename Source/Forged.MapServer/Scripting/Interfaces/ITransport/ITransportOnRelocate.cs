﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities;

namespace Forged.MapServer.Scripting.Interfaces.ITransport;

public interface ITransportOnRelocate : IScriptObject
{
    void OnRelocate(Transport transport, uint mapId, double x, double y, double z);
}
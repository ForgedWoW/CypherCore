﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.World;

internal struct Autobroadcast
{
    public string Message;

    public byte Weight;

    public Autobroadcast(string message, byte weight)
    {
        Message = message;
        Weight = weight;
    }
}
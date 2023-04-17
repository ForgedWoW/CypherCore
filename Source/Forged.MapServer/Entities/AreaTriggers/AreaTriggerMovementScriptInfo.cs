﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.Networking;

namespace Forged.MapServer.Entities.AreaTriggers;

public struct AreaTriggerMovementScriptInfo
{
    public Vector3 Center;
    public uint SpellScriptID;

    public void Write(WorldPacket data)
    {
        data.WriteUInt32(SpellScriptID);
        data.WriteVector3(Center);
    }
}
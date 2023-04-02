// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Services;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ServiceAttribute : Attribute
{
    public ServiceAttribute(OriginalHash serviceHash, uint methodId)
    {
        ServiceHash = (uint)serviceHash;
        MethodId = methodId;
    }

    public uint MethodId { get; set; }
    public uint ServiceHash { get; set; }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Forged.MapServer.Chat;

[AttributeUsage(AttributeTargets.Parameter)]
public class VariantArgAttribute : Attribute
{
    public VariantArgAttribute(params Type[] types)
    {
        Types = types;
    }

    public Type[] Types { get; set; }
}
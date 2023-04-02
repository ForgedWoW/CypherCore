// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Spells;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class SpellEffectHandlerAttribute : Attribute
{
    public SpellEffectHandlerAttribute(SpellEffectName effectName)
    {
        EffectName = effectName;
    }

    public SpellEffectName EffectName { get; set; }
}
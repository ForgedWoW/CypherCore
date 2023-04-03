// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Scripting.Interfaces.ISpell;

public class SpellEffect : ISpellEffect
{
    public int EffectIndex { get; private set; }

    public SpellScriptHookType HookType { get; private set; }

    public SpellEffect(int effectIndex, SpellScriptHookType hookType)
    {
        EffectIndex = effectIndex;
        HookType = hookType;
    }
}
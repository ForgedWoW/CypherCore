// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.BLACK_DEEP_BREATH_EFFECT)]
public class spell_evoker_deep_breath_effect : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        Caster.Events.AddEventAtOffset(() => { Caster.CastSpell(Caster, EvokerSpells.BLACK_DEEP_BREATH_END); }, TimeSpan.FromMilliseconds(880));
    }
}
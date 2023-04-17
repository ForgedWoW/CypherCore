// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.DemonHunter;

[SpellScript(197923)]
public class SpellDhFelRushDashSpellScript : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(PreventTrigger, 6, SpellEffectName.TriggerSpell, SpellScriptHookType.Launch));
        SpellEffects.Add(new EffectHandler(PreventTrigger, 6, SpellEffectName.TriggerSpell, SpellScriptHookType.EffectHit));
    }

    private void PreventTrigger(int effIndex)
    {
        PreventHitEffect(effIndex);
    }
}
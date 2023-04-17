// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[SpellScript(115151)]
public class SpellMonkRenewingMist : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.Launch));
    }

    private void HandleDummy(int effIndex)
    {
        PreventHitDefaultEffect(effIndex);
        var target = ExplTargetUnit;

        if (target != null)
            Caster.SpellFactory.CastSpell(target, MonkSpells.RENEWING_MIST_HOT, true);
    }
}
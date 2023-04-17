// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[SpellScript(203651)]
public class SpellDruOvergrowth : SpellScript, IHasSpellEffects
{
    private const int Rejuvenation = 774;
    private const int WildGrowth = 48438;
    private const int LifeBloom = 33763;
    private const int Regrowth = 8936;

    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster;

        if (caster != null)
        {
            var target = HitUnit;

            if (target != null)
            {
                caster.AddAura(Rejuvenation, target);
                caster.AddAura(WildGrowth, target);
                caster.AddAura(LifeBloom, target);
                caster.AddAura(Regrowth, target);
            }
        }
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// 30108 - Unstable Affliction
[SpellScript(30108)]
public class SpellWarlockUnstableAffliction : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleHit(int effIndex)
    {
        PreventHitDefaultEffect(effIndex);
        var caster = Caster;
        var target = HitUnit;

        if (caster == null || target == null)
            return;

        var uaspells = new List<int>()
        {
            (int)WarlockSpells.UNSTABLE_AFFLICTION_DOT5,
            (int)WarlockSpells.UNSTABLE_AFFLICTION_DOT4,
            (int)WarlockSpells.UNSTABLE_AFFLICTION_DOT3,
            (int)WarlockSpells.UNSTABLE_AFFLICTION_DOT2,
            (int)WarlockSpells.UNSTABLE_AFFLICTION_DOT1
        };

        uint spellToCast = 0;
        var minDuration = 10000;
        uint lowestDurationSpell = 0;

        foreach (uint spellId in uaspells)
        {
            var ua = target.GetAura(spellId, caster.GUID);

            if (ua != null)
            {
                if (ua.Duration < minDuration)
                {
                    minDuration = ua.Duration;
                    lowestDurationSpell = ua.SpellInfo.Id;
                }
            }
            else
                spellToCast = spellId;
        }

        if (spellToCast == 0)
            caster.SpellFactory.CastSpell(target, lowestDurationSpell, true);
        else
            caster.SpellFactory.CastSpell(target, spellToCast, true);

        if (caster.HasAura(WarlockSpells.CONTAGION))
            caster.SpellFactory.CastSpell(target, WarlockSpells.CONTAGION_DEBUFF, true);

        if (caster.HasAura(WarlockSpells.COMPOUNDING_HORROR))
            caster.SpellFactory.CastSpell(target, WarlockSpells.COMPOUNDING_HORROR_DAMAGE, true);
    }
}
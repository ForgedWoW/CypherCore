// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Mage;

[SpellScript(84721)]
public class SpellMageFrozenOrb : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleHit(int effIndex)
    {
        var caster = Caster;
        var target = HitUnit;

        if (caster == null || target == null)
            return;

        caster.SpellFactory.CastSpell(target, MageSpells.CHILLED, true);

        // Fingers of Frost
        if (caster.HasSpell(MageSpells.FINGERS_OF_FROST))
        {
            var fingersFrostChance = 10.0f;

            if (caster.HasAura(MageSpells.FROZEN_TOUCH))
            {
                var frozenEff0 = caster.GetAuraEffect(MageSpells.FROZEN_TOUCH, 0);

                if (frozenEff0 != null)
                {
                    var pct = frozenEff0.Amount;
                    MathFunctions.AddPct(ref fingersFrostChance, pct);
                }
            }

            if (RandomHelper.randChance(fingersFrostChance))
            {
                caster.SpellFactory.CastSpell(caster, MageSpells.FINGERS_OF_FROST_VISUAL_UI, true);
                caster.SpellFactory.CastSpell(caster, MageSpells.FINGERS_OF_FROST_AURA, true);
            }
        }
    }
}
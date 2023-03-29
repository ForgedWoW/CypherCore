// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(258922)]
public class spell_dh_immolation_aura_damage : SpellScript, IHasSpellEffects
{
    readonly uint[] _hit = new uint[]
    {
        DemonHunterSpells.FIERY_BRAND_DOT, DemonHunterSpells.FIERY_BRAND_MARKER
    };

    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleHit(int effIndex)
    {
        var target = HitUnit;

        if (target != null)
            if (Caster.HasAura(DemonHunterSpells.CHARRED_FLESH))
                foreach (var spellId in _hit)
                {
                    var fieryBrand = target.GetAura(spellId);

                    if (fieryBrand != null)
                    {
                        var durationMod = Caster.GetAuraEffectAmount(DemonHunterSpells.CHARRED_FLESH, 0);
                        fieryBrand.ModDuration(durationMod);
                    }
                }
    }
}
﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(47541)]
internal class spell_dk_death_coil : SpellScript, IHasSpellEffects
{
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
                if (target.IsFriendlyTo(caster) && target.CreatureType == CreatureType.Undead)
                {
                    caster.CastSpell(HitUnit, DeathKnightSpells.DEATH_COIL_HEAL, true);
                }
                else
                {
                    var spell = caster.CastSpell(HitUnit, DeathKnightSpells.DEATH_COIL_DAMAGE, true);
                }

                var unholyAura = caster.GetAuraEffect(DeathKnightSpells.Unholy, 6);

                if (unholyAura != null) // can be any effect, just here to send SpellFailedDontReport on failure
                    caster.CastSpell(caster, DeathKnightSpells.UnholyVigor, new CastSpellExtraArgs(unholyAura));

                var suddenDoom = caster.GetAura(DeathKnightSpells.DEATH_COIL_SUDDEN_DOOM_AURA);

                if (suddenDoom != null)
                    if (caster.HasAura(DeathKnightSpells.DEATH_COIL_ROTTENTOUCH))
                        caster.AddAura(DeathKnightSpells.DEATH_COIL_ROTTENTOUCH_AURA, target);
            }
        }
    }
}
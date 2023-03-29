// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(195072)]
public class spell_dh_fel_rush : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDashGround, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        SpellEffects.Add(new EffectHandler(HandleDashAir, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDashGround(int effIndex)
    {
        var caster = Caster;

        if (caster != null)
        {
            if (!caster.IsFalling || caster.IsInWater)
            {
                caster.RemoveAura(DemonHunterSpells.GLIDE);
                caster.CastSpell(caster, DemonHunterSpells.FEL_RUSH_DASH, true);

                if (HitUnit)
                    caster.CastSpell(HitUnit, DemonHunterSpells.FEL_RUSH_DAMAGE, true);

                if (caster.HasAura(ShatteredSoulsSpells.MOMENTUM))
                    caster.CastSpell(ShatteredSoulsSpells.MOMENTUM_BUFF, true);
            }

            caster.SpellHistory.AddCooldown(SpellInfo.Id, 0, TimeSpan.FromMicroseconds(750));
        }
    }

    private void HandleDashAir(int effIndex)
    {
        var caster = Caster;

        if (caster != null)
            if (caster.IsFalling)
            {
                caster.RemoveAura(DemonHunterSpells.GLIDE);
                caster.SetDisableGravity(true);
                caster.CastSpell(caster, DemonHunterSpells.FEL_RUSH_AIR, true);

                if (HitUnit)
                    caster.CastSpell(HitUnit, DemonHunterSpells.FEL_RUSH_DAMAGE, true);

                if (caster.HasAura(ShatteredSoulsSpells.MOMENTUM))
                    caster.CastSpell(ShatteredSoulsSpells.MOMENTUM_BUFF, true);

                caster.SpellHistory.AddCooldown(SpellInfo.Id, 0, TimeSpan.FromMicroseconds(750));
            }
    }
}
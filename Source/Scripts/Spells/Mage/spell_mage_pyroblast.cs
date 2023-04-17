// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Mage;

[SpellScript(11366)]
public class SpellMagePyroblast : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHit));
    }

    private void HandleOnHit(int effIndex)
    {
        var caster = Caster;

        if (caster == null)
            return;

        if (caster.HasAura(MageSpells.HOT_STREAK))
        {
            caster.RemoveAura(MageSpells.HOT_STREAK);

            if (caster.HasAura(MageSpells.PYROMANIAC))
            {
                var pyromaniacEff0 = caster.GetAuraEffect(MageSpells.PYROMANIAC, 0);

                if (pyromaniacEff0 != null)
                    if (RandomHelper.randChance(pyromaniacEff0.Amount))
                    {
                        if (caster.HasAura(MageSpells.HEATING_UP))
                            caster.RemoveAura(MageSpells.HEATING_UP);

                        caster.SpellFactory.CastSpell(caster, MageSpells.HOT_STREAK, true);
                    }
            }
        }
    }
}
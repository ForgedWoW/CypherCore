// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Paladin;

// Light of the Martyr - 183998
[SpellScript(183998)]
public class SpellPalLightOfTheMartyr : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleOnHit(int effIndex)
    {
        var caster = Caster;

        var dmg = (HitHeal * 50.0f) / 100.0f;
        caster.SpellFactory.CastSpell(caster, PaladinSpells.LIGHT_OF_THE_MARTYR_DAMAGE, (int)dmg);

        if (caster.HasAura(PaladinSpells.FERVENT_MARTYR_BUFF))
            caster.RemoveAura(PaladinSpells.FERVENT_MARTYR_BUFF);
    }
}
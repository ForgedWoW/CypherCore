// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

[SpellScript(100)] // 100 - Charge
internal class SpellWarrCharge : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var spellId = WarriorSpells.CHARGE_EFFECT;

        if (Caster.HasAura(WarriorSpells.GLYPH_OF_THE_BLAZING_TRAIL))
            spellId = WarriorSpells.CHARGE_EFFECT_BLAZING_TRAIL;

        Caster.SpellFactory.CastSpell(HitUnit, spellId, true);
    }
}
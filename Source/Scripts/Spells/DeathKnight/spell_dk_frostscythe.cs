// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[SpellScript(207230)]
public class SpellDkFrostscythe : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
    }

    private void HandleHit(int effIndex)
    {
        if (Caster.HasAura(DeathKnightSpells.INEXORABLE_ASSAULT_STACK))
            Caster.SpellFactory.CastSpell(HitUnit, DeathKnightSpells.INEXORABLE_ASSAULT_DAMAGE, true);

        if (Caster.HasAura(DeathKnightSpells.KILLING_MACHINE))
        {
            Caster.RemoveAura(DeathKnightSpells.KILLING_MACHINE);
            HitDamage = HitDamage * 4;
        }
    }
}
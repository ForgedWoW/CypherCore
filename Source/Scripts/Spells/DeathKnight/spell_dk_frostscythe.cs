// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(207230)]
public class spell_dk_frostscythe : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
    }

    private void HandleHit(int effIndex)
    {
        if (Caster.HasAura(DeathKnightSpells.INEXORABLE_ASSAULT_STACK))
            Caster.CastSpell(HitUnit, DeathKnightSpells.INEXORABLE_ASSAULT_DAMAGE, true);

        if (Caster.HasAura(DeathKnightSpells.KILLING_MACHINE))
        {
            Caster.RemoveAura(DeathKnightSpells.KILLING_MACHINE);
            HitDamage = HitDamage * 4;
        }
    }
}
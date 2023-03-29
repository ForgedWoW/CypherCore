// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin;

[SpellScript(53595)] // 53595 - Hammer of the Righteous
internal class spell_pal_hammer_of_the_righteous : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleAoEHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleAoEHit(int effIndex)
    {
        if (Caster.HasAura(PaladinSpells.ConsecrationProtectionAura))
            Caster.CastSpell(HitUnit, PaladinSpells.HammerOfTheRighteousAoe);
    }
}
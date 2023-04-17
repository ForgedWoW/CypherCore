// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Paladin;

[SpellScript(53595)] // 53595 - Hammer of the Righteous
internal class SpellPalHammerOfTheRighteous : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleAoEHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleAoEHit(int effIndex)
    {
        if (Caster.HasAura(PaladinSpells.CONSECRATION_PROTECTION_AURA))
            Caster.SpellFactory.CastSpell(HitUnit, PaladinSpells.HAMMER_OF_THE_RIGHTEOUS_AOE);
    }
}
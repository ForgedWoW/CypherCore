// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

// Cataclysm - 152108
[SpellScript(152108)]
internal class spell_warl_cataclysm : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleHit(int effIndex)
    {
        var caster = Caster;
        var target = HitUnit;

        if (caster == null || target == null)
            return;

        if (!caster.AsPlayer)
            return;

        if (Caster.AsPlayer.GetPrimarySpecialization() == TalentSpecialization.WarlockDestruction)
            caster.CastSpell(target, WarlockSpells.IMMOLATE_DOT, true);
    }
}
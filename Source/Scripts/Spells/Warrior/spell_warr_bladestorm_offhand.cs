// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

// 95738 - Bladestorm Offhand
[SpellScript(95738)]
public class SpellWarrBladestormOffhand : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        SpellEffects.Add(new EffectHandler(HandleOnHit, 1, SpellEffectName.WeaponPercentDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleOnHit(int effIndex)
    {
        var caster = Caster.AsPlayer;

        if (caster == null)
            return;

        var spec = caster.GetPrimarySpecialization();

        if (spec != TalentSpecialization.WarriorFury) //only fury warriors should deal damage with offhand
        {
            PreventHitDamage();
            PreventHitDefaultEffect(effIndex);
            PreventHitEffect(effIndex);
        }
    }
}
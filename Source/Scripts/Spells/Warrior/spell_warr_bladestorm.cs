// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

// Bladestorm - 227847, 46924
public class SpellWarrBladestorm : SpellScript, ISpellOnCast, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public void OnCast()
    {
        Caster.SpellFactory.CastSpell(Caster, WarriorSpells.NEW_BLADESTORM, true);
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
        SpellEffects.Add(new EffectHandler(HandleOnHit, 1, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
        SpellEffects.Add(new EffectHandler(HandleOnHit, 2, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleOnHit(int effIndex)
    {
        PreventHitAura();
        PreventHitDamage();
        PreventHitDefaultEffect(effIndex);
        PreventHitEffect(effIndex);
        PreventHitHeal();
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

// Ravager Damage - 156287
[SpellScript(156287)]
public class SpellWarrRavagerDamage : SpellScript, IHasSpellEffects
{
    private bool _alreadyProc = false;
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleOnHitTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleOnHitTarget(int effIndex)
    {
        if (!_alreadyProc)
        {
            Caster.SpellFactory.CastSpell(Caster, WarriorSpells.RAVAGER_ENERGIZE, true);
            _alreadyProc = true;
        }

        if (Caster.HasAura(262304))                  // Deep Wounds
            Caster.SpellFactory.CastSpell(HitUnit, 262115, true); // Deep Wounds
    }
}
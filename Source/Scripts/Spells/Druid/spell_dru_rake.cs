// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[SpellScript(1822)]
public class SpellDruRake : SpellScript, IHasSpellEffects
{
    private bool _stealthed = false;

    public List<ISpellEffect> SpellEffects { get; } = new();

    public override bool Load()
    {
        var caster = Caster;

        if (caster.HasAuraType(AuraType.ModStealth))
            _stealthed = true;

        return true;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleOnHit(int effIndex)
    {
        var caster = Caster;
        var target = ExplTargetUnit;

        if (caster == null || target == null)
            return;

        // While stealthed or have Incarnation: King of the Jungle aura, deal 100% increased damage
        if (_stealthed || caster.HasAura(ShapeshiftFormSpells.IncarnationKingOfJungle))
            HitDamage = HitDamage * 2;

        // Only stun if the caster was in stealth
        if (_stealthed)
            caster.SpellFactory.CastSpell(target, RakeSpells.RAKE_STUN, true);
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

[SpellScript(WarlockSpells.DEMON_SKIN)]
internal class AuraWarlDemonSkin : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(Periodic, 0, AuraType.PeriodicDummy));
    }

    void Periodic(AuraEffect eff)
    {
        if (!TryGetCaster(out var caster)) return;

        var absorb = (caster.MaxHealth * (GetEffect(0).BaseAmount / 10)) / 100.0f;

        if (caster.TryGetAura(WarlockSpells.SOUL_LEECH_ABSORB, out var aur) && aur.TryGetEffect(0, out var auraEffect))
            absorb += auraEffect.Amount;

        var threshold = (caster.MaxHealth * GetEffect(1).BaseAmount) / 100.0f;
        absorb = Math.Min(absorb, threshold);
        caster.SpellFactory.CastSpell(caster, WarlockSpells.SOUL_LEECH_ABSORB, absorb, true);
    }
}
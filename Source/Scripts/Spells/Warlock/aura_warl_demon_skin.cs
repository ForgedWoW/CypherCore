// Copyright(c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    [SpellScript(WarlockSpells.DEMON_SKIN)]
    internal class aura_warl_demon_skin : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

        void Periodic(AuraEffect eff)
        {
            if (!TryGetCaster(out var caster)) return;

            double absorb = (caster.GetMaxHealth() * (GetEffect(0).BaseAmount / 10)) / 100.0f;

            if (caster.TryGetAura(WarlockSpells.SOUL_LEECH_ABSORB, out var aur) && aur.TryGetEffect(0, out var auraEffect))
                absorb += auraEffect.Amount;

            var threshold = (caster.GetMaxHealth() * GetEffect(1).BaseAmount) / 100.0f;
            absorb = Math.Min(absorb, threshold);
            caster.CastSpell(caster, WarlockSpells.SOUL_LEECH_ABSORB, absorb, true);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectPeriodicHandler(Periodic, 0, AuraType.PeriodicDummy));
        }
    }
}

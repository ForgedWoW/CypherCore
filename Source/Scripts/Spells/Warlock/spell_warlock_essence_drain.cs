// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// 221711 - Essence Drain
// Called by Drain Soul (198590) and Drain Life (234153)
[SpellScript(221711)]
public class SpellWarlockEssenceDrain : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.Dummy));
    }

    private void PeriodicTick(AuraEffect unnamedParameter)
    {
        var caster = Caster;
        var target = OwnerAsUnit;

        if (caster == null || target == null)
            return;

        if (caster.HasAura(WarlockSpells.ESSENCE_DRAIN))
            caster.SpellFactory.CastSpell(target, WarlockSpells.ESSENCE_DRAIN_DEBUFF, true);

        var durationBonus = caster.GetAuraEffectAmount(WarlockSpells.ROT_AND_DECAY, 0);

        if (durationBonus != 0)
        {
            var dots = new List<uint>()
            {
                (uint)WarlockSpells.AGONY,
                (uint)WarlockSpells.CORRUPTION_TRIGGERED,
                (uint)WarlockSpells.UNSTABLE_AFFLICTION_DOT1,
                (uint)WarlockSpells.UNSTABLE_AFFLICTION_DOT2,
                (uint)WarlockSpells.UNSTABLE_AFFLICTION_DOT3,
                (uint)WarlockSpells.UNSTABLE_AFFLICTION_DOT4,
                (uint)WarlockSpells.UNSTABLE_AFFLICTION_DOT5
            };

            foreach (var dot in dots)
            {
                var aur = target.GetAura(dot, caster.GUID);

                if (aur != null)
                    aur.ModDuration(durationBonus);
            }
        }
    }
}
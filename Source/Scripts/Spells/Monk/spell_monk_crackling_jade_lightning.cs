// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[Script] // 117952 - Crackling Jade Lightning
internal class SpellMonkCracklingJadeLightning : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 0, AuraType.PeriodicDamage));
    }

    private void OnTick(AuraEffect aurEff)
    {
        var caster = Caster;

        if (caster)
            if (caster.HasAura(MonkSpells.StanceOfTheSpiritedCrane))
                caster.SpellFactory.CastSpell(caster, MonkSpells.CracklingJadeLightningChiProc, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
    }
}
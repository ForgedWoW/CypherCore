// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[SpellScript(145108)]
public class SpellDruYseraGift : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDummy));
    }

    private void HandlePeriodic(AuraEffect aurEff)
    {
        var caster = Caster;

        if (caster == null || !caster.IsAlive)
            return;

        var amount = MathFunctions.CalculatePct(caster.MaxHealth, aurEff.BaseAmount);
        var values = new CastSpellExtraArgs(TriggerCastFlags.FullMask);
        values.AddSpellMod(SpellValueMod.MaxTargets, 1);
        values.AddSpellMod(SpellValueMod.BasePoint0, (int)amount);

        if (caster.IsFullHealth)
            caster.SpellFactory.CastSpell(caster, DruidSpells.YseraGiftRaidHeal, values);
        else
            caster.SpellFactory.CastSpell(caster, DruidSpells.YseraGiftCasterHeal, values);
    }
}
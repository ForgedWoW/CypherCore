// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Scripts.Spells.Shaman;

//201633 - Earthen Shield
[SpellScript(201633)]
public class SpellShaEarthenShieldAbsorb : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcAbsorb, 0, AuraType.SchoolAbsorb));
        AuraEffects.Add(new AuraEffectAbsorbHandler(HandleAbsorb, 0));
    }

    private void CalcAbsorb(AuraEffect unnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        if (!Caster)
            return;

        amount.Value = Caster.Health;
    }

    private double HandleAbsorb(AuraEffect unnamedParameter, DamageInfo dmgInfo, double absorbAmount)
    {
        var caster = Caster;

        if (caster == null || !caster.IsTotem)
            return absorbAmount;

        var owner = caster.OwnerUnit;

        if (owner == null)
            return absorbAmount;

        if (dmgInfo.Damage - owner.GetTotalSpellPowerValue(SpellSchoolMask.All, true) > 0)
            absorbAmount = owner.GetTotalSpellPowerValue(SpellSchoolMask.All, true);
        else
            absorbAmount = dmgInfo.Damage;

        //201657 - The damager
        caster.SpellFactory.CastSpell(caster, 201657, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)absorbAmount));

        return absorbAmount;
    }
}
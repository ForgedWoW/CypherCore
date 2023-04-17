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

namespace Scripts.Spells.Paladin;

// 184662 - Shield of Vengeance
[SpellScript(184662)]
public class SpellPalShieldOfVengeance : AuraScript, IHasAuraEffects
{
    private int _absorb;
    private int _currentAbsorb;
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.SchoolAbsorb, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        AuraEffects.Add(new AuraEffectAbsorbHandler(Absorb, 0));
    }

    private void CalculateAmount(AuraEffect unnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        var caster = Caster;

        if (caster != null)
        {
            canBeRecalculated.Value = false;

            var ap = caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack);
            _absorb = (int)(ap * 20);
            amount.Value += _absorb;
        }
    }

    private double Absorb(AuraEffect aura, DamageInfo damageInfo, double absorbAmount)
    {
        var caster = Caster;

        if (caster == null)
            return absorbAmount;

        _currentAbsorb += (int)damageInfo.Damage;

        return absorbAmount;
    }

    private void OnRemove(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var caster = Caster;

        if (caster == null)
            return;

        if (_currentAbsorb < _absorb)
            return;

        var targets = new List<Unit>();
        caster.GetAttackableUnitListInRange(targets, 8.0f);

        var targetSize = (uint)targets.Count;

        if (targets.Count != 0)
            _absorb /= (int)targetSize;

        caster.SpellFactory.CastSpell(caster, PaladinSpells.SHIELD_OF_VENGEANCE_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)_absorb));
    }
}
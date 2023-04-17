// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.DemonHunter;

[SpellScript(207811)]
public class SpellDhNetherBondPeriodic : AuraScript, IHasAuraEffects
{
    private Unit _mBondUnit;
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real));
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDummy));
    }

    private void HandlePeriodic(AuraEffect unnamedParameter)
    {
        var caster = Caster;

        if (caster == null)
            return;

        // Try to get the bonded Unit
        if (_mBondUnit == null)
            _mBondUnit = GetBondUnit();

        // If still not found, return
        if (_mBondUnit == null)
            return;

        long casterHealBp = 0;
        long casterDamageBp = 0;
        long targetHealBp = 0;
        long targetDamageBp = 0;

        var casterHp = caster.HealthPct;
        var targetHp = _mBondUnit.HealthPct;
        var healthPct = (casterHp + targetHp) / 2.0f;

        if (casterHp < targetHp)
        {
            casterHealBp = caster.CountPctFromMaxHealth(healthPct) - caster.Health;
            targetDamageBp = _mBondUnit.Health - _mBondUnit.CountPctFromMaxHealth(healthPct);
        }
        else
        {
            casterDamageBp = caster.Health - caster.CountPctFromMaxHealth(healthPct);
            targetHealBp = _mBondUnit.CountPctFromMaxHealth(healthPct) - _mBondUnit.Health;
        }

        caster.SpellFactory.CastSpell(caster, DemonHunterSpells.NETHER_BOND_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, casterDamageBp).AddSpellMod(SpellValueMod.BasePoint1, casterHealBp));
        caster.SpellFactory.CastSpell(_mBondUnit, DemonHunterSpells.NETHER_BOND_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, targetDamageBp).AddSpellMod(SpellValueMod.BasePoint1, targetHealBp));
    }

    private Unit GetBondUnit()
    {
        var caster = Caster;

        if (caster == null)
            return null;

        var units = new List<Unit>();
        var check = new AnyUnitInObjectRangeCheck(caster, 100.0f);
        var search = new UnitListSearcher(caster, units, check, GridType.All);
        Cell.VisitGrid(caster, search, 100.0f);

        foreach (var u in units)
            if (u.HasAura(DemonHunterSpells.NETHER_BOND, caster.GUID))
                return u;

        return null;
    }

    private void HandleApply(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var caster = Caster;

        if (caster == null)
            return;

        _mBondUnit = GetBondUnit();
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// Immolate Dot - 157736
[SpellScript(157736)]
public class SpellWarlockImmolateDot : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.RealOrReapplyMask));
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDamage));
        AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectAfterRemove));
    }

    private void HandlePeriodic(AuraEffect unnamedParameter)
    {
        var caster = Caster;

        if (caster == null)
            return;

        Flashpoint(caster);
        ChannelDemonfire(caster);
        RoaringBlaze(caster);
    }

    private void Flashpoint(Unit caster)
    {
        var target = Target;

        if (target != null && caster.TryGetAura(WarlockSpells.FLASHPOINT, out var fp) && target.HealthAbovePct(fp.GetEffect(1).BaseAmount))
            caster.SpellFactory.CastSpell(caster, WarlockSpells.FLASHPOINT_AURA, true);
    }

    private void ChannelDemonfire(Unit caster)
    {
        var aur = caster.GetAura(WarlockSpells.CHANNEL_DEMONFIRE_ACTIVATOR);

        if (aur != null)
            aur.RefreshDuration();
    }

    private void RoaringBlaze(Unit caster)
    {
        if (Aura != null && caster.HasAura(WarlockSpells.ROARING_BLAZE))
        {
            var dmgEff = Global.SpellMgr.GetSpellInfo(WarlockSpells.ROARING_BLASE_DMG_PCT, Difficulty.None)?.GetEffect(0);

            if (dmgEff != null)
            {
                var damage = GetEffect(0).Amount;
                MathFunctions.AddPct(ref damage, dmgEff.BasePoints);

                GetEffect(0).SetAmount(damage);
                Aura.SetNeedClientUpdateForTargets();
            }
        }
    }

    private void HandleApply(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var caster = Caster;

        if (caster == null)
            return;

        caster.SpellFactory.CastSpell(caster, WarlockSpells.CHANNEL_DEMONFIRE_ACTIVATOR, true);
    }

    private void HandleRemove(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var caster = Caster;

        if (caster == null)
            return;

        var checker = new UnitAuraCheck<Unit>(true, WarlockSpells.IMMOLATE_DOT, caster.GUID);
        var enemies = new List<Unit>();
        var check = new AnyUnfriendlyUnitInObjectRangeCheck(caster, caster, 100.0f, checker.Invoke);
        var searcher = new UnitListSearcher(caster, enemies, check, GridType.All);
        Cell.VisitGrid(caster, searcher, 100.0f);

        if (enemies.Count == 0)
        {
            var aur = caster.GetAura(WarlockSpells.CHANNEL_DEMONFIRE_ACTIVATOR);

            if (aur != null)
                aur.SetDuration(0);
        }
    }
}
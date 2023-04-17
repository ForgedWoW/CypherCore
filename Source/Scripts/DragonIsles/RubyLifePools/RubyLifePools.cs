// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.DragonIsles.RubyLifePools;

internal struct SpellIds
{
    // Flashfrost Chillweaver
    public const uint ICE_SHIELD = 372749;

    // Primal Juggernaut
    public const uint EXCAVATE = 373497;
};

// 371652 - Executed
internal class SpellRubyLifePoolsExecuted : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectApply, 0, AuraType.ModStun, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
    }

    private void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var target = Target;
        target.SetUnitFlag3(UnitFlags3.FakeDead);
        target.SetUnitFlag2(UnitFlags2.FeignDeath);
        target.SetUnitFlag(UnitFlags.PreventEmotesFromChatText);
    }
}

// 384933 - Ice Shield
internal class SpellRubyLifePoolsIceShield : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
    }

    private void HandleEffectPeriodic(AuraEffect aurEff)
    {
        var iceShield = Target?.GetAura(SpellIds.ICE_SHIELD);
        iceShield?.RefreshDuration();
    }
}

// 372793 - Excavate
internal class SpellRubyLifePoolsExcavate : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
    }

    private void HandleEffectPeriodic(AuraEffect aurEff)
    {
        Caster?.SpellFactory.CastSpell(Target, SpellIds.EXCAVATE, true);
    }
}

// 395029 - Storm Infusion
internal class SpellRubyLifePoolsStormInfusion : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new DestinationTargetSelectHandler(SetDest, 1, Targets.DestDest));
    }

    private void SetDest(SpellDestination dest)
    {
        dest.RelocateOffset(new Position(9.0f, 0.0f, 4.0f, 0.0f));
    }
}
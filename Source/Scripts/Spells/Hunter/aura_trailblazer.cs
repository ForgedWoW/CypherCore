// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Forged.MapServer.Spells.Events;
using Framework.Constants;

namespace Scripts.Spells.Hunter;

[SpellScript(199921)]
public class AuraTrailblazer : AuraScript, IHasAuraEffects
{
    DelayedCastEvent _event;
    TimeSpan _ts;
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(EffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.ModIncreaseSpeed, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
    }

    private void EffectApply(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        RescheduleBuff();

        var player = Target.AsPlayer;

        if (player != null)
            player.SetSpeed(UnitMoveType.Run, player.GetSpeedRate(UnitMoveType.Run) + 0.15f);
    }

    private void HandleProc(AuraEffect unnamedParameter, ProcEventInfo unnamedParameter2)
    {
        RescheduleBuff();
    }

    private void RescheduleBuff()
    {
        var caster = Caster;
        caster.RemoveAura(HunterSpells.TRAILBLAZER_BUFF);

        if (_event == null)
        {
            _event = new DelayedCastEvent(caster, caster, HunterSpells.TRAILBLAZER_BUFF, new CastSpellExtraArgs(true));
            _ts = TimeSpan.FromSeconds(SpellInfo.GetEffect(0).BasePoints);
        }
        else
            caster.Events.ScheduleAbortOnFirstMatchingEvent(e =>
            {
                if (e is DelayedCastEvent dce)
                    return dce.SpellId == HunterSpells.TRAILBLAZER_BUFF;

                return false;
            });

        caster.Events.AddEventAtOffset(_event, _ts);
    }

    private void OnRemove(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var player = Target.AsPlayer;

        if (player != null)
            player.SetSpeed(UnitMoveType.Run, player.GetSpeedRate(UnitMoveType.Run) - 0.15f);
    }
}
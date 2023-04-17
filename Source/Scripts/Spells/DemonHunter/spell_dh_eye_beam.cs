// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.DemonHunter;

[SpellScript(198013)]
public class SpellDhEyeBeam : AuraScript, IHasAuraEffects
{
    private bool _firstTick = true;
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicTriggerSpell));
        AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 2, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 2, AuraType.Dummy, AuraEffectHandleModes.Real));
    }

    private void HandlePeriodic(AuraEffect unnamedParameter)
    {
        var caster = Caster;

        if (caster != null)
            if (!_firstTick)
            {
                caster.SpellFactory.CastSpell(caster, DemonHunterSpells.EYE_BEAM_DAMAGE, true);
                var energize = caster.GetAuraEffectAmount(DemonHunterSpells.BLIND_FURY, 2);

                if (energize != 0)
                    caster.ModifyPower(PowerType.Fury, energize * 2.0f / 50.0f);
            }

        _firstTick = false;
    }

    private void HandleRemove(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var caster = Caster;

        if (caster != null)
            caster.RemoveAura(DemonHunterSpells.EYE_BEAM_VISUAL);
    }

    private void HandleApply(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var caster = Caster;

        if (caster != null)
        {
            if (!caster.HasAura(DemonHunterSpells.DEMONIC))
                caster.SpellFactory.CastSpell(caster, DemonHunterSpells.EYE_BEAM_VISUAL, true);

            if (caster.HasAura(DemonHunterSpells.DEMONIC))
            {
                var aur = caster.GetAura(DemonHunterSpells.METAMORPHOSIS_HAVOC);

                if (aur != null)
                    aur.ModDuration(8 * Time.IN_MILLISECONDS);
                else
                    aur = caster.AddAura(DemonHunterSpells.METAMORPHOSIS_HAVOC, caster);

                if (aur != null)
                    aur.SetDuration(10 * Time.IN_MILLISECONDS);
            }
        }
    }
}
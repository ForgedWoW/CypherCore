// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[SpellScript(203974)]
public class SpellDruidEarthwarden : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        if (!Caster.AsPlayer.SpellHistory.HasCooldown(Spells.Earthwarden))
            Caster.AddAura(Spells.EarthwardenTriggered, Caster);

        Caster.AsPlayer.SpellHistory.AddCooldown(Spells.Earthwarden, 0, TimeSpan.FromMicroseconds(500));
    }

    private struct Spells
    {
        public static readonly uint Earthwarden = 203974;
        public static readonly uint EarthwardenTriggered = 203975;
        public static readonly uint Trash = 77758;
    }
}
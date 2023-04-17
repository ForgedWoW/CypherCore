// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// 260895 - Unlimited Power
[SpellScript(260895)]
internal class SpellShaUnlimitedPower : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        var caster = procInfo.Actor;
        var aura = caster.GetAura(ShamanSpells.UNLIMITED_POWER_BUFF);

        if (aura != null)
            aura.SetStackAmount((byte)(aura.StackAmount + 1));
        else
            caster.SpellFactory.CastSpell(caster, ShamanSpells.UNLIMITED_POWER_BUFF, procInfo.ProcSpell);
    }
}
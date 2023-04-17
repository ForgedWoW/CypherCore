// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Dynamic;

namespace Scripts.Spells.Shaman;

// 70817 - Item - Shaman T10 Elemental 4P Bonus
[SpellScript(70817)]
internal class SpellShaT10Elemental4PBonus : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        var caster = eventInfo.Actor;
        var target = eventInfo.ProcTarget;

        // try to find spell Flame Shock on the Target
        var flameShock = target.GetAuraEffect(AuraType.PeriodicDamage, SpellFamilyNames.Shaman, new FlagArray128(0x10000000), caster.GUID);

        if (flameShock == null)
            return;

        var flameShockAura = flameShock.Base;

        var maxDuration = flameShockAura.MaxDuration;
        var newDuration = flameShockAura.Duration + aurEff.Amount * Time.IN_MILLISECONDS;

        flameShockAura.SetDuration(newDuration);

        // is it blizzlike to change max duration for FS?
        if (newDuration > maxDuration)
            flameShockAura.SetMaxDuration(newDuration);
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 26467 - Persistent Shield
internal class SpellItemPersistentShield : AuraScript, IAuraCheckProc, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public bool CheckProc(ProcEventInfo eventInfo)
    {
        return eventInfo.HealInfo != null && eventInfo.HealInfo.Heal != 0;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.PeriodicTriggerSpell, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        var caster = eventInfo.Actor;
        var target = eventInfo.ProcTarget;
        var bp0 = (int)MathFunctions.CalculatePct(eventInfo.HealInfo.Heal, 15);

        // Scarab Brooch does not replace stronger shields
        var shield = target.GetAuraEffect(ItemSpellIds.PERSISTENT_SHIELD_TRIGGERED, 0, caster.GUID);

        if (shield != null)
            if (shield.Amount > bp0)
                return;

        CastSpellExtraArgs args = new(aurEff);
        args.AddSpellMod(SpellValueMod.BasePoint0, bp0);
        caster.SpellFactory.CastSpell(target, ItemSpellIds.PERSISTENT_SHIELD_TRIGGERED, args);
    }
}
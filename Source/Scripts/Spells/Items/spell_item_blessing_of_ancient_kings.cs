// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 64411 - Blessing of Ancient Kings (Val'anyr, Hammer of Ancient Kings)
internal class SpellItemBlessingOfAncientKings : AuraScript, IAuraCheckProc, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public bool CheckProc(ProcEventInfo eventInfo)
    {
        return eventInfo.ProcTarget != null;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        var healInfo = eventInfo.HealInfo;

        if (healInfo == null ||
            healInfo.Heal == 0)
            return;

        var absorb = (int)MathFunctions.CalculatePct(healInfo.Heal, 15.0f);
        var protEff = eventInfo.ProcTarget.GetAuraEffect(ItemSpellIds.PROTECTION_OF_ANCIENT_KINGS, 0, eventInfo.Actor.GUID);

        if (protEff != null)
        {
            // The shield can grow to a maximum size of 20,000 Damage absorbtion
            protEff.SetAmount(Math.Min(protEff.Amount + absorb, 20000));

            // Refresh and return to prevent replacing the aura
            protEff.Base.RefreshDuration();
        }
        else
        {
            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, absorb);
            Target.SpellFactory.CastSpell(eventInfo.ProcTarget, ItemSpellIds.PROTECTION_OF_ANCIENT_KINGS, args);
        }
    }
}
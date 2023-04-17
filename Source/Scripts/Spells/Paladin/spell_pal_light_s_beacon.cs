// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Paladin;

[SpellScript(53651)] // 53651 - Beacon of Light
internal class SpellPalLightSBeacon : AuraScript, IAuraCheckProc, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public bool CheckProc(ProcEventInfo eventInfo)
    {
        if (!eventInfo.ActionTarget)
            return false;

        if (eventInfo.ActionTarget.HasAura(PaladinSpells.BEACON_OF_LIGHT, eventInfo.Actor.GUID))
            return false;

        return true;
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

        var heal = MathFunctions.CalculatePct(healInfo.Heal, aurEff.Amount);

        var auras = Caster.SingleCastAuras;

        foreach (var eff in auras)
            if (eff.Id == PaladinSpells.BEACON_OF_LIGHT)
            {
                var applications = eff.GetApplicationList();

                if (!applications.Empty())
                {
                    CastSpellExtraArgs args = new(aurEff);
                    args.AddSpellMod(SpellValueMod.BasePoint0, (int)heal);
                    eventInfo.Actor.SpellFactory.CastSpell(applications[0].Target, PaladinSpells.BEACON_OF_LIGHT_HEAL, args);
                }

                return;
            }
    }
}
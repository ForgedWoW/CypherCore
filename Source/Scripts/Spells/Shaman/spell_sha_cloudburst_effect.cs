// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

//157504 - Cloudburst Totem
[SpellScript(157504)]
public class SpellShaCloudburstEffect : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
    }

    private void OnProc(AuraEffect pAurEff, ProcEventInfo pEventInfo)
    {
        PreventDefaultAction();

        var lHealInfo = pEventInfo.HealInfo;

        if (lHealInfo == null)
            return;

        if (Global.SpellMgr.GetSpellInfo(TotemSpells.TOTEM_CLOUDBURST, Difficulty.None) != null)
        {
            var lSpellInfo = Global.SpellMgr.GetSpellInfo(TotemSpells.TOTEM_CLOUDBURST, Difficulty.None);
            GetEffect((byte)pAurEff.EffIndex).SetAmount(pAurEff.Amount + (int)MathFunctions.CalculatePct(lHealInfo.Heal, lSpellInfo.GetEffect(0).BasePoints));
        }
    }

    private void OnRemove(AuraEffect pAurEff, AuraEffectHandleModes unnamedParameter)
    {
        var lOwner = Owner.AsUnit;

        if (lOwner != null)
        {
            var lAmount = pAurEff.Amount;

            if (pAurEff.Amount != 0)
            {
                lOwner.SpellFactory.CastSpell(lOwner, TotemSpells.TOTEM_CLOUDBURST, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)lAmount));
                GetEffect((byte)pAurEff.EffIndex).SetAmount(0);
            }
        }
    }
}
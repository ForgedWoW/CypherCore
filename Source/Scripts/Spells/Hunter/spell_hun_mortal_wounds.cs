// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Hunter;

[SpellScript(201075)]
public class SpellHunMortalWounds : AuraScript, IHasAuraEffects, IAuraCheckProc
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public bool CheckProc(ProcEventInfo eventInfo)
    {
        if ((eventInfo.HitMask & ProcFlagsHit.None) != 0 && eventInfo.SpellInfo.Id == HunterSpells.LACERATE)
            return true;

        return false;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect unnamedParameter, ProcEventInfo unnamedParameter2)
    {
        PreventDefaultAction();

        var player = Caster.AsPlayer;

        if (player != null)
        {
            var chargeCatId = Global.SpellMgr.GetSpellInfo(HunterSpells.MONGOOSE_BITE, Difficulty.None).ChargeCategoryId;

            var mongooseBite = CliDB.SpellCategoryStorage.LookupByKey(chargeCatId);

            if (mongooseBite != null)
                player.SpellHistory.RestoreCharge(chargeCatId);
        }
    }
}
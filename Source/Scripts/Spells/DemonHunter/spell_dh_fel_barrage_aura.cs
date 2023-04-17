// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.DemonHunter;

[SpellScript(222703)]
public class SpellDhFelBarrageAura : AuraScript, IHasAuraEffects, IAuraCheckProc
{
    //Blade Dance    //Chaos Strike   //Fel Barrage
    readonly List<uint> _removeSpellIds = new()
    {
        199552,
        210153,
        222031,
        227518,
        211052
    };

    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public bool CheckProc(ProcEventInfo eventInfo)
    {
        // Blade Dance, Chaos Strike and Annihilation have many damagers,
        // so we accept only 1 of those, and we remove the others
        // Also we remove fel barrage itself too.
        if (eventInfo.SpellInfo != null)
            return false;

        return !_removeSpellIds.Contains(eventInfo.SpellInfo.Id);
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect unnamedParameter, ProcEventInfo unnamedParameter2)
    {
        PreventDefaultAction();

        var caster = Caster;

        if (caster == null)
            return;

        var chargeCatId = Global.SpellMgr.GetSpellInfo(DemonHunterSpells.FEL_BARRAGE, Difficulty.None).ChargeCategoryId;

        if (chargeCatId != 0)
            caster.SpellHistory.RestoreCharge(chargeCatId);
    }
}
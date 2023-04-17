// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[SpellScript(116645)]
public class SpellMonkTeachingsOfTheMonasteryPassive : AuraScript, IHasAuraEffects, IAuraCheckProc
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public bool CheckProc(ProcEventInfo eventInfo)
    {
        if (eventInfo.SpellInfo.Id != MonkSpells.TIGER_PALM && eventInfo.SpellInfo.Id != MonkSpells.BLACKOUT_KICK && eventInfo.SpellInfo.Id != MonkSpells.BLACKOUT_KICK_TRIGGERED)
            return false;

        return true;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        if (eventInfo.SpellInfo.Id == MonkSpells.TIGER_PALM)
            Target.SpellFactory.CastSpell(Target, MonkSpells.TEACHINGS_OF_THE_MONASTERY, true);
        else if (RandomHelper.randChance(aurEff.Amount))
        {
            var spellInfo = Global.SpellMgr.GetSpellInfo(MonkSpells.RISING_SUN_KICK, Difficulty.None);

            if (spellInfo != null)
                Target.SpellHistory.RestoreCharge(spellInfo.ChargeCategoryId);
        }
    }
}
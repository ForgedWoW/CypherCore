// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[SpellScript(202090)]
public class SpellMonkTeachingsOfTheMonasteryBuff : AuraScript, IHasAuraEffects, IAuraCheckProc
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public bool CheckProc(ProcEventInfo eventInfo)
    {
        if (!Target.HasAura(MonkSpells.TEACHINGS_OF_THE_MONASTERY_PASSIVE))
            return false;

        if (eventInfo.SpellInfo.Id != MonkSpells.BLACKOUT_KICK)
            return false;

        return true;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect unnamedParameter, ProcEventInfo eventInfo)
    {
        var monasteryBuff = Aura;

        if (monasteryBuff != null)
        {
            for (byte i = 0; i < monasteryBuff.StackAmount; ++i)
                Target.SpellFactory.CastSpell(eventInfo.ProcTarget, MonkSpells.BLACKOUT_KICK_TRIGGERED);

            monasteryBuff.Remove();
        }
    }
}
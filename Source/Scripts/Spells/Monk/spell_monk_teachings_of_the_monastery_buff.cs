// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(202090)]
public class spell_monk_teachings_of_the_monastery_buff : AuraScript, IHasAuraEffects, IAuraCheckProc
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

    private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
    {
        var monasteryBuff = Aura;

        if (monasteryBuff != null)
        {
            for (byte i = 0; i < monasteryBuff.StackAmount; ++i)
                Target.CastSpell(eventInfo.ProcTarget, MonkSpells.BLACKOUT_KICK_TRIGGERED);

            monasteryBuff.Remove();
        }
    }
}
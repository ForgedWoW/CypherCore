// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using System;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.FIELD_OF_DREAMS)]
public class aura_evoker_field_of_dreams : AuraScript, IAuraCheckProc, IAuraOnProc
{
    public bool CheckProc(ProcEventInfo info)
    {
        return info.HealInfo != null 
            && info.HealInfo.SpellInfo.Id == EvokerSpells.FLUTTERING_SEEDLINGS_HEAL 
            && info.HealInfo.Healer.TryGetAsPlayer(out var player)
            && !player.SpellHistory.HasCooldown(EvokerSpells.FIELD_OF_DREAMS)
            && RandomHelper.randChance(Aura.SpellInfo.GetEffect(0).BasePoints);
    }

    public void OnProc(ProcEventInfo info)
    {
        var player = info.HealInfo.Healer.AsPlayer;
        player.SpellHistory.AddCooldown(EvokerSpells.FIELD_OF_DREAMS, 0, TimeSpan.FromMilliseconds(100));
        player.CastSpell(info.HealInfo.Target, EvokerSpells.EMERALD_BLOSSOM, true);
    }
}
﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[SpellScript(185789)]
public class spell_hun_wild_call : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public bool CheckProc(ProcEventInfo eventInfo)
    {
        if (eventInfo.SpellInfo.Id == HunterSpells.AUTO_SHOT && (eventInfo.HitMask & ProcFlagsHit.Critical) != 0)
            return true;

        return false;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
    {
        var player = Caster.AsPlayer;

        if (player != null)
            if (player.SpellHistory.HasCooldown(HunterSpells.BARBED_SHOT))
                player.SpellHistory.ResetCooldown(HunterSpells.BARBED_SHOT, true);
    }
}
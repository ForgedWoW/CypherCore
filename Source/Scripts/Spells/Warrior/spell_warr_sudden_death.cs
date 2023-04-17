// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

// 52437 - Sudden Death
[Script]
internal class SpellWarrSuddenDeath : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply)); // correct?
    }

    private void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        // Remove cooldown on Colossus Smash
        var player = Target.AsPlayer;

        if (player)
            player.SpellHistory.ResetCooldown(WarriorSpells.COLOSSUS_SMASH, true);
    }
}
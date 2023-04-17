﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script] // 132334 - Trainer Heal Cooldown (SERVERSIDE)
internal class SpellGenTrainerHealCooldown : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override bool Load()
    {
        return OwnerAsUnit.IsPlayer;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(UpdateReviveBattlePetCooldown, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
    }

    private void UpdateReviveBattlePetCooldown(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var target = OwnerAsUnit.AsPlayer;
        var reviveBattlePetSpellInfo = Global.SpellMgr.GetSpellInfo(SharedConst.SpellReviveBattlePets, Difficulty.None);

        if (target.Session.BattlePetMgr.IsBattlePetSystemEnabled)
        {
            var expectedCooldown = TimeSpan.FromMilliseconds(Aura.MaxDuration);
            var remainingCooldown = target.SpellHistory.GetRemainingCategoryCooldown(reviveBattlePetSpellInfo);

            if (remainingCooldown > TimeSpan.Zero)
            {
                if (remainingCooldown < expectedCooldown)
                    target.SpellHistory.ModifyCooldown(reviveBattlePetSpellInfo, expectedCooldown - remainingCooldown);
            }
            else
                target.SpellHistory.StartCooldown(reviveBattlePetSpellInfo, 0, null, false, expectedCooldown);
        }
    }
}
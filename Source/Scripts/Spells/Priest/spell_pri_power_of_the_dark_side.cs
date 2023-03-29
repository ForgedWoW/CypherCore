﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 198069 - Power of the Dark Side
internal class spell_pri_power_of_the_dark_side : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleOnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
        AuraEffects.Add(new AuraEffectApplyHandler(HandleOnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
    }

    private void HandleOnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var caster = Caster;

        caster?.CastSpell(caster, PriestSpells.POWER_OF_THE_DARK_SIDE_TINT, true);
    }

    private void HandleOnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var caster = Caster;

        caster?.RemoveAura(PriestSpells.POWER_OF_THE_DARK_SIDE_TINT);
    }
}
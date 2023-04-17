// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[SpellScript(197632)]
public class SpellDruBalanceAffinityResto : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(UnlearnSpells, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        AuraEffects.Add(new AuraEffectApplyHandler(LearnSpells, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
    }

    private void LearnSpells(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var caster = Caster;

        if (caster == null)
            return;

        var player = caster.AsPlayer;

        if (player != null)
        {
            player.AddTemporarySpell(ShapeshiftFormSpells.MoonkinForm);
            player.AddTemporarySpell(BalanceAffinitySpells.Starsurge);
            player.AddTemporarySpell(BalanceAffinitySpells.LunarStrike);
        }
    }

    private void UnlearnSpells(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var caster = Caster;

        if (caster == null)
            return;

        var player = caster.AsPlayer;

        if (player != null)
        {
            player.RemoveTemporarySpell(ShapeshiftFormSpells.MoonkinForm);
            player.RemoveTemporarySpell(BalanceAffinitySpells.Starsurge);
            player.RemoveTemporarySpell(BalanceAffinitySpells.LunarStrike);
        }
    }
}
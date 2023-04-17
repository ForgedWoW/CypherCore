// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[SpellScript(197492)]
public class AuraDruRestorationAffinity : AuraScript, IHasAuraEffects
{
    private readonly List<uint> _learnedSpells = new()
    {
        (uint)DruidSpells.YseraGift,
        (uint)DruidSpells.Rejuvenation,
        (uint)DruidSpells.HealingTouch,
        (uint)DruidSpells.Swiftmend
    };

    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(AfterApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void AfterApply(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var target = Target.AsPlayer;

        if (target != null)
            foreach (var spellId in _learnedSpells)
                target.LearnSpell(spellId, false);
    }

    private void AfterRemove(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var target = Target.AsPlayer;

        if (target != null)
            foreach (var spellId in _learnedSpells)
                target.RemoveSpell(spellId);
    }
}
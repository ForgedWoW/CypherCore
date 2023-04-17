// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[SpellScript(202157)]
public class AuraDruFeralAffinity : AuraScript, IHasAuraEffects
{
    private readonly List<uint> _learnedSpells = new()
    {
        (uint)DruidSpells.FelineSwiftness,
        (uint)DruidSpells.Shred,
        (uint)DruidSpells.Rake,
        (uint)DruidSpells.Rip,
        (uint)DruidSpells.FerociousBite,
        (uint)DruidSpells.SwipeCat
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
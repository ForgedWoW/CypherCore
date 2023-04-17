// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[SpellScript(51271)]
public class SpellDkPillarOfFrost : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.ModTotalStatPercentage, AuraEffectHandleModes.Real));
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
    }

    private void OnRemove(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var player = Target.AsPlayer;

        if (player != null)
            player.ApplySpellImmune(DeathKnightSpells.PILLAR_OF_FROST, SpellImmunity.Mechanic, Mechanics.Knockout, false);
    }

    private void OnApply(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var player = Target.AsPlayer;

        if (player != null)
            player.ApplySpellImmune(DeathKnightSpells.PILLAR_OF_FROST, SpellImmunity.Mechanic, Mechanics.Knockout, true);
    }
}
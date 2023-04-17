// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

// 215570 - Wrecking Ball
[SpellScript(215570)]
public class SpellWarrWreckingBallEffect : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.AddPctModifier, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect unnamedParameter, ProcEventInfo unnamedParameter2)
    {
        var caster = Caster;

        if (caster != null)
            if (caster.HasAura(WarriorSpells.WRECKING_BALL_EFFECT))
                caster.RemoveAura(WarriorSpells.WRECKING_BALL_EFFECT);
    }
}
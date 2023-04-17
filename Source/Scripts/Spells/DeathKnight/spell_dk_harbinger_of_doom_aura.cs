// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[SpellScript(276023)]
public class SpellDkHarbingerOfDoomAura : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
    }

    private void OnApply(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var caster = Caster;

        if (caster != null)
        {
            var player = caster.AsPlayer;

            if (player != null)
            {
                var spell = Target.FindCurrentSpellBySpellId(DeathKnightSpells.DEATH_COIL_SUDDEN_DOOM);
                spell.SpellInfo.ProcBasePpm = MathFunctions.CalculatePct(spell.SpellInfo.ProcBasePpm, 100 - 30);
            }
        }
    }

    private void OnRemove(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var spell = Target.FindCurrentSpellBySpellId(DeathKnightSpells.DEATH_COIL_SUDDEN_DOOM);
        spell.SpellInfo.ProcBasePpm = Global.SpellMgr.GetSpellInfo(DeathKnightSpells.DEATH_COIL_SUDDEN_DOOM).ProcBasePpm;
    }
}
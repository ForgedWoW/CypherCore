// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// 188070 Healing Surge
[SpellScript(188070)]
public class SpellShaHealingSurge : SpellScript, IHasSpellEffects, ISpellCalculateCastTime
{
    private int _takenPower = 0;
    public List<ISpellEffect> SpellEffects { get; } = new();

    public int CalcCastTime(int castTime)
    {
        var requiredMaelstrom = (int)GetEffectInfo(2).BasePoints;

        if (Caster.GetPower(PowerType.Maelstrom) >= requiredMaelstrom)
        {
            castTime = 0;
            _takenPower = requiredMaelstrom;
        }

        return castTime;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleEnergize, 1, SpellEffectName.Energize, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleEnergize(int effIndex)
    {
        EffectValue = -_takenPower;
    }
}
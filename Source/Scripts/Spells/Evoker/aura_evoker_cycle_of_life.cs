// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
using System.Collections.Generic;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.CYCLE_OF_LIFE_AURA)]
public class aura_evoker_cycle_of_life : AuraScript, IAuraOnProc, IAuraScriptValues, IAuraOnRemove, IAuraOnApply
{
    double _multiplier = 0;
	double _healAmount = 0;

    public Dictionary<string, object> ScriptValues { get; } = new();

    public void AuraApplied()
    {
        _multiplier = SpellManager.Instance.GetSpellInfo(EvokerSpells.CYCLE_OF_LIFE).GetEffect(0).BasePoints * 0.01;
    }

    public void AuraRemoved(AuraRemoveMode removeMode)
    {
        CastSpellExtraArgs args = new(true);
        args.SpellValueOverrides[SpellValueMod.BasePoint0] = _healAmount;

        OwnerAsUnit.CastSpell((Position)ScriptValues["pos"], EvokerSpells.CYCLE_OF_LIFE_HEAL, args);
    }

    public void OnProc(ProcEventInfo info)
    {
		if (info.HealInfo != null)
            _healAmount = info.HealInfo.Heal * _multiplier;
    }

    public void SetScriptValues(params KeyValuePair<string, object>[] values)
    {
        foreach (var value in values)
            ScriptValues[value.Key] = value.Value;
    }
}
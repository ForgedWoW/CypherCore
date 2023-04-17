// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

//Cloudburst - 157503
[SpellScript(157503)]
public class SpellShaCloudburst : SpellScript, IHasSpellEffects
{
    private byte _lTargetCount;
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override bool Load()
    {
        _lTargetCount = 0;

        return true;
    }

    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(CountTargets, 0, Targets.UnitDestAreaAlly));
        SpellEffects.Add(new EffectHandler(HandleHeal, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleHeal(int effIndex)
    {
        if (_lTargetCount != 0)
            HitHeal = HitHeal / _lTargetCount;
    }

    private void CountTargets(List<WorldObject> pTargets)
    {
        _lTargetCount = (byte)pTargets.Count;
    }
}
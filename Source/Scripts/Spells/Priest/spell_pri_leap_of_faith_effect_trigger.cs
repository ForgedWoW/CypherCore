// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Priest;

[Script] // 92833 - Leap of Faith
internal class SpellPriLeapOfFaithEffectTrigger : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleEffectDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleEffectDummy(int effIndex)
    {
        var destPos = HitDest;

        SpellCastTargets targets = new();
        targets.SetDst(destPos);
        targets.UnitTarget = Caster;
        HitUnit.SpellFactory.CastSpell(targets, (uint)EffectValue, new CastSpellExtraArgs(CastDifficulty));
    }
}
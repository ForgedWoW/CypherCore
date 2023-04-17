// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// Ascendance (Water)(heal) - 114083
[SpellScript(114083)]
public class SpellShaAscendanceWaterHeal : SpellScript, IHasSpellEffects
{
    private uint _mTargetSize = 0;
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(OnEffectHeal, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaAlly));
    }

    private void OnEffectHeal(int effIndex)
    {
        HitHeal = (int)(HitHeal / _mTargetSize);
    }

    private void FilterTargets(List<WorldObject> pTargets)
    {
        _mTargetSize = (uint)pTargets.Count;
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

[Script] // 46968 - Shockwave
internal class SpellWarrShockwave : SpellScript, ISpellAfterCast, IHasSpellEffects
{
    private uint _targetCount;

    public List<ISpellEffect> SpellEffects { get; } = new();

    public override bool Load()
    {
        return Caster.IsTypeId(TypeId.Player);
    }

    // Cooldown reduced by 20 sec if it strikes at least 3 targets.
    public void AfterCast()
    {
        if (_targetCount >= (uint)GetEffectInfo(0).CalcValue())
            Caster.AsPlayer.SpellHistory.ModifyCooldown(SpellInfo.Id, TimeSpan.FromSeconds(-GetEffectInfo(3).CalcValue()));
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleStun, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleStun(int effIndex)
    {
        Caster.SpellFactory.CastSpell(HitUnit, WarriorSpells.SHOCKWAVE_STUN, true);
        ++_targetCount;
    }
}
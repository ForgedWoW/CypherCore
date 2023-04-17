// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[Script] // 49576 - Death Grip Initial
internal class SpellDkDeathGripInitial : SpellScript, ISpellCheckCast, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public SpellCastResult CheckCast()
    {
        var caster = Caster;

        // Death Grip should not be castable while jumping/falling
        if (caster.HasUnitState(UnitState.Jumping) ||
            caster.HasUnitMovementFlag(MovementFlag.Falling))
            return SpellCastResult.Moving;

        return SpellCastResult.SpellCastOk;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        Caster.SpellFactory.CastSpell(HitUnit, DeathKnightSpells.DEATH_GRIP_DUMMY, true);
        HitUnit.SpellFactory.CastSpell(Caster, DeathKnightSpells.DEATH_GRIP_JUMP, true);

        if (Caster.HasAura(DeathKnightSpells.BLOOD))
            Caster.SpellFactory.CastSpell(HitUnit, DeathKnightSpells.DeathGripTaunt, true);
    }
}
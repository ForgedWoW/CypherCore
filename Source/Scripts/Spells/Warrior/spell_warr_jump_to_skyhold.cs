﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Movement;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior;

// Jump to Skyhold Jump - 192085
[SpellScript(192085)]
public class spell_warr_jump_to_skyhold : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleJump, 0, SpellEffectName.JumpDest, SpellScriptHookType.Launch));
    }

    private void HandleJump(int effIndex)
    {
        PreventHitDefaultEffect(effIndex);

        var caster = Caster;

        if (caster != null)
        {
            var pos_x = caster.Location.X;
            var pos_y = caster.Location.Y;
            var pos_z = caster.Location.Z + 30.0f;

            var arrivalCast = new JumpArrivalCastArgs();
            arrivalCast.SpellId = WarriorSpells.JUMP_TO_SKYHOLD_TELEPORT;
            arrivalCast.Target = caster.GUID;
            caster.MotionMaster.MoveJump(pos_x, pos_y, pos_z, caster.Location.Orientation, 20.0f, 20.0f, EventId.Jump, false, arrivalCast);

            caster.RemoveAura(WarriorSpells.JUMP_TO_SKYHOLD_AURA);
        }
    }
}
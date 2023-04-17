// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Movement;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

// Jump to Skyhold Jump - 192085
[SpellScript(192085)]
public class SpellWarrJumpToSkyhold : SpellScript, IHasSpellEffects
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
            var posX = caster.Location.X;
            var posY = caster.Location.Y;
            var posZ = caster.Location.Z + 30.0f;

            var arrivalCast = new JumpArrivalCastArgs();
            arrivalCast.SpellId = WarriorSpells.JUMP_TO_SKYHOLD_TELEPORT;
            arrivalCast.Target = caster.GUID;
            caster.MotionMaster.MoveJump(posX, posY, posZ, caster.Location.Orientation, 20.0f, 20.0f, EventId.Jump, false, arrivalCast);

            caster.RemoveAura(WarriorSpells.JUMP_TO_SKYHOLD_AURA);
        }
    }
}
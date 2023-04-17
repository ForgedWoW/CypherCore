// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Hunter;

[SpellScript(190925)]
public class SpellHunHarpoon : SpellScript, IHasSpellEffects, ISpellAfterCast, ISpellOnCast
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public void AfterCast()
    {
        var player = Caster.AsPlayer;

        if (player != null)
            if (player.HasSpell(HunterSpells.POSTHAST))
                player.SpellFactory.CastSpell(player, HunterSpells.POSTHAST_SPEED, true);
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.TriggerMissile, SpellScriptHookType.EffectHitTarget));
    }

    public void OnCast()
    {
        var player = Caster.AsPlayer;
        var target = ExplTargetUnit;

        if (player == null || target == null)
            return;

        player.SpellFactory.CastSpell(target, HunterSpells.HARPOON_ROOT, true);
    }

    private void HandleDummy(int effIndex)
    {
        var player = Caster.AsPlayer;
        var target = ExplTargetUnit;

        if (player == null || target == null)
            return;

        var pTarget = target.Location;

        float speedXy;
        float speedZ;
        speedZ = 1.8f;
        speedXy = player.Location.GetExactDist2d(pTarget) * 10.0f / speedZ;
        player.MotionMaster.MoveJump(pTarget, speedXy, speedZ, EventId.Jump);
    }
}
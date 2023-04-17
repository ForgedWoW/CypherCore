// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Priest;

[SpellScript(73325)]
public class SpellPriLeapOfFaith : SpellScript, IHasSpellEffects, ISpellOnHit
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public void OnHit()
    {
        var player = Caster.AsPlayer;

        if (player != null)
        {
            var target = HitUnit;

            if (target != null)
            {
                target.SpellFactory.CastSpell(player, PriestSpells.LEAP_OF_FAITH_JUMP, true);

                if (player.HasAura(PriestSpells.BODY_AND_SOUL_AURA))
                    player.SpellFactory.CastSpell(target, PriestSpells.BODY_AND_SOUL_SPEED, true);
            }
        }
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        var caster = Caster;

        if (caster == null)
            return;

        if (caster.HasAura(PriestSpells.LEAP_OF_FAITH_GLYPH))
            HitUnit.RemoveMovementImpairingAuras(false);

        HitUnit.SpellFactory.CastSpell(caster, PriestSpells.LEAP_OF_FAITH_EFFECT, true);
    }
}
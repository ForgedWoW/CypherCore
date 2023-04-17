// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[SpellScript(194913)]
public class SpellDkGlacialAdvance : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
    }

    private void HandleHit(int effIndex)
    {
        var caster = Caster;

        var castPosition = caster.Location;
        var collisonPos = caster.GetFirstCollisionPosition(EffectInfo.MaxRadiusEntry.RadiusMax, 0);
        var maxDistance = caster.GetDistance(collisonPos);

        for (var dist = 0.0f; dist <= maxDistance; dist += 1.5f)
            caster.Events.AddEventAtOffset(() =>
                                           {
                                               var targetPosition = new Position(castPosition);
                                               caster.MovePosition(targetPosition, dist, 0.0f);
                                               caster.SpellFactory.CastSpell(targetPosition, DeathKnightSpells.GLACIAL_ADVANCE_DAMAGE, true);
                                           },
                                           TimeSpan.FromMilliseconds(dist / 1.5f * 50.0f));
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.DemonHunter;

[SpellScript(209651)]
public class SpellDhShatteredSoulsMissile : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHit, 1, SpellEffectName.TriggerMissile, SpellScriptHookType.EffectHit));
    }

    private void HandleHit(int effIndex)
    {
        PreventHitDefaultEffect(effIndex);
        var caster = Caster;

        if (caster == null)
            return;

        var spellToCast = SpellValue.EffectBasePoints[0];

        var dest = HitDest;

        if (dest != null)
            caster.SpellFactory.CastSpell(new Position(dest.X, dest.Y, dest.Z), (uint)spellToCast, true);
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

[SpellScript(WarlockSpells.INQUISITORS_GAZE)]
public class SpellWarlockInquisitorsGaze : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleOnHit(int effectIndex)
    {
        var target = HitUnit;

        if (target != null)
        {
            var damage = (Caster.SpellBaseDamageBonusDone(SpellInfo.SchoolMask) * 15 * 16) / 100;
            Caster.SpellFactory.CastSpell(target, WarlockSpells.INQUISITORS_GAZE_EFFECT, new CastSpellExtraArgs(SpellValueMod.BasePoint0, damage));
        }
    }
}
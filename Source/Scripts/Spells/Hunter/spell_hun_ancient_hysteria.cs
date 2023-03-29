// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(90355)]
public class spell_hun_ancient_hysteria : SpellScript, IHasSpellEffects
{
    readonly UnitAuraCheck<WorldObject> _ins = new(true, AncientHysteriaSpells.INSANITY);
    readonly UnitAuraCheck<WorldObject> _dis = new(true, AncientHysteriaSpells.TEMPORAL_DISPLACEMENT);
    readonly UnitAuraCheck<WorldObject> _ex = new(true, AncientHysteriaSpells.EXHAUSTION);
    readonly UnitAuraCheck<WorldObject> _sa = new(true, AncientHysteriaSpells.SATED);
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(RemoveInvalidTargets, (byte)255, Targets.UnitCasterAreaRaid));
    }

    private void RemoveInvalidTargets(List<WorldObject> targets)
    {
        targets.RemoveIf(_ins);
        targets.RemoveIf(_dis);
        targets.RemoveIf(_ex);
        targets.RemoveIf(_sa);
    }

    private void ApplyDebuff()
    {
        var target = HitUnit;

        if (target != null)
            target.CastSpell(target, AncientHysteriaSpells.INSANITY, true);
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Mage;

[Script] // 80353 - Time Warp
internal class SpellMageTimeWarp : SpellScript, ISpellAfterHit, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public void AfterHit()
    {
        var target = HitUnit;

        if (target)
            target.SpellFactory.CastSpell(target, MageSpells.TEMPORAL_DISPLACEMENT, true);
    }

    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(RemoveInvalidTargets, SpellConst.EffectAll, Targets.UnitCasterAreaRaid));
    }

    private void RemoveInvalidTargets(List<WorldObject> targets)
    {
        targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, MageSpells.TEMPORAL_DISPLACEMENT));
        targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, MageSpells.HUNTER_INSANITY));
        targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, MageSpells.SHAMAN_EXHAUSTION));
        targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, MageSpells.SHAMAN_SATED));
    }
}
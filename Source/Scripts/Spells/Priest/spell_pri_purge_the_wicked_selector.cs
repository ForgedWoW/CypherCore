// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Priest;

[SpellScript(204215)]
public class SpellPriPurgeTheWickedSelector : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaEnemy));
        SpellEffects.Add(new EffectHandler(HandleDummy, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void FilterTargets(List<WorldObject> targets)
    {
        targets.RemoveIf(new UnitAuraCheck<WorldObject>(true, PriestSpells.PURGE_THE_WICKED_DOT, Caster.GUID));
        targets.Sort(new ObjectDistanceOrderPred(ExplTargetUnit));

        if (targets.Count > 1)
            targets.Resize(1);
    }

    private void HandleDummy(int effIndex)
    {
        Caster.AddAura(PriestSpells.PURGE_THE_WICKED_DOT, HitUnit);
    }
}
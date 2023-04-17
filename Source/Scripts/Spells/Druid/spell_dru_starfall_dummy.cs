// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[Script] // 50286 - Starfall (Dummy)
internal class SpellDruStarfallDummy : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaEnemy));
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void FilterTargets(List<WorldObject> targets)
    {
        targets.Resize(2);
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster;

        // Shapeshifting into an animal form or mounting cancels the effect
        if (caster.CreatureType == CreatureType.Beast ||
            caster.IsMounted)
        {
            var spellInfo = TriggeringSpell;

            if (spellInfo != null)
                caster.RemoveAura(spellInfo.Id);

            return;
        }

        // Any effect which causes you to lose control of your character will supress the starfall effect.
        if (caster.HasUnitState(UnitState.Controlled))
            return;

        caster.SpellFactory.CastSpell(HitUnit, (uint)EffectValue, true);
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// 210621 - Path of Flames Spread
[SpellScript(210621)]
internal class SpellShaPathOfFlamesSpread : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaEnemy));
        SpellEffects.Add(new EffectHandler(HandleScript, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void FilterTargets(List<WorldObject> targets)
    {
        targets.Remove(ExplTargetUnit);
        targets.RandomResize(target => target.IsTypeId(TypeId.Unit) && !target.AsUnit.HasAura(ShamanSpells.FlameShock, Caster.GUID), 1);
    }

    private void HandleScript(int effIndex)
    {
        var mainTarget = ExplTargetUnit;

        if (mainTarget)
        {
            var flameShock = mainTarget.GetAura(ShamanSpells.FlameShock, Caster.GUID);

            if (flameShock != null)
            {
                var newAura = Caster.AddAura(ShamanSpells.FlameShock, HitUnit);

                if (newAura != null)
                {
                    newAura.SetDuration(flameShock.Duration);
                    newAura.SetMaxDuration(flameShock.Duration);
                }
            }
        }
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenReplenishment : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(RemoveInvalidTargets, 255, Targets.UnitCasterAreaRaid));
    }

    private void RemoveInvalidTargets(List<WorldObject> targets)
    {
        // In arenas Replenishment may only affect the caster
        var caster = Caster.AsPlayer;

        if (caster)
            if (caster.InArena)
            {
                targets.Clear();
                targets.Add(caster);

                return;
            }

        targets.RemoveAll(obj =>
        {
            var target = obj.AsUnit;

            if (target)
                return target.DisplayPowerType != PowerType.Mana;

            return true;
        });

        byte maxTargets = 10;

        if (targets.Count > maxTargets)
        {
            targets.Sort(new PowerPctOrderPred(PowerType.Mana));
            targets.Resize(maxTargets);
        }
    }
}
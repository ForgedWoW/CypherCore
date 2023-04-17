// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[SpellScript(197531)]
public class SpellDkBloodworms : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
    }

    private void FilterTargets(List<WorldObject> targets)
    {
        targets.Clear();

        var caster = Caster;

        if (caster != null)
            foreach (var itr in caster.Controlled)
            {
                var unit = ObjectAccessor.Instance.GetUnit(caster, itr.GUID);

                if (unit != null)
                    if (unit.Entry == 99773)
                        targets.Add(unit);
            }
    }
}
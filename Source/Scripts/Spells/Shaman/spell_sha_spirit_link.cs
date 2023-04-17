// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// Spirit link
[SpellScript(98021)]
public class SpellShaSpiritLink : SpellScript, ISpellOnHit
{
    private readonly SortedDictionary<ObjectGuid, double> _targets = new();
    private double _averagePercentage;
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override bool Load()
    {
        _averagePercentage = 0.0f;

        return true;
    }

    public void OnHit()
    {
        var target = HitUnit;

        if (target != null)
        {
            if (!_targets.ContainsKey(target.GUID))
                return;

            var bp0 = 0.0f;
            var bp1 = 0.0f;
            var percentage = _targets[target.GUID];
            var currentHp = target.CountPctFromMaxHealth((int)percentage);
            var desiredHp = target.CountPctFromMaxHealth((int)_averagePercentage);

            if (desiredHp > currentHp)
                bp1 = desiredHp - currentHp;
            else
                bp0 = currentHp - desiredHp;

            var args = new CastSpellExtraArgs();

            Caster
                .SpellFactory.CastSpell(target,
                                        98021,
                                        new CastSpellExtraArgs(TriggerCastFlags.None)
                                            .AddSpellMod(SpellValueMod.BasePoint0, (int)bp0)
                                            .AddSpellMod(SpellValueMod.BasePoint1, (int)bp1));
        }
    }

    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitCasterAreaRaid));
    }

    private void FilterTargets(List<WorldObject> unitList)
    {
        uint targetCount = 0;

        for (var itr = unitList.GetEnumerator(); itr.MoveNext();)
        {
            var target = itr.Current.AsUnit;

            if (target != null)
            {
                _targets[target.GUID] = target.HealthPct;
                _averagePercentage += target.HealthPct;
                ++targetCount;
            }
        }

        _averagePercentage /= targetCount;
    }
}
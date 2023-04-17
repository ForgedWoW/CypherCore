// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// Thal'kiel's Consumption - 211714
[SpellScript(211714)]
public class SpellArtiWarlThalkielsConsumption : SpellScript, IHasSpellEffects
{
    private int _damage = 0;

    public List<ISpellEffect> SpellEffects { get; } = new();

    public void HandleHit(int effIndex)
    {
        var caster = Caster;
        var target = HitUnit;

        if (target == null || caster == null)
            return;

        caster.SpellFactory.CastSpell(target, WarlockSpells.THALKIELS_CONSUMPTION_DAMAGE, new CastSpellExtraArgs(SpellValueMod.BasePoint0, _damage));
    }

    public void SaveDamage(List<WorldObject> targets)
    {
        targets.RemoveIf((WorldObject target) =>
        {
            if (!target.IsCreature)
                return true;

            if (!target.AsCreature.IsPet || target.AsCreature.AsPet.OwningPlayer != Caster)
                return true;

            if (target.AsCreature.CreatureType != CreatureType.Demon)
                return true;

            return false;
        });

        var basePoints = SpellInfo.GetEffect(1).BasePoints;

        foreach (var pet in targets)
            _damage += (int)pet.AsUnit.CountPctFromMaxHealth(basePoints);
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(SaveDamage, 1, Targets.UnitCasterAndSummons));
    }
}
﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 187837 - Lightning Bolt
[SpellScript(187837)]
public class spell_sha_enhancement_lightning_bolt : SpellScript, IHasSpellEffects, ISpellOnTakePower
{
    private int _takenPower;
    private int _maxTakenPower;
    private int _maxDamagePercent;
    public List<ISpellEffect> SpellEffects { get; } = new();


    public void TakePower(SpellPowerCost powerCost)
    {
        _maxTakenPower = 0;
        _maxDamagePercent = 0;

        var overcharge = Caster.GetAura(ShamanSpells.OVERCHARGE);

        if (overcharge != null)
        {
            _maxTakenPower = (int)overcharge.SpellInfo.GetEffect(0).BasePoints;
            _maxDamagePercent = (int)overcharge.SpellInfo.GetEffect(1).BasePoints;
        }

        _takenPower = powerCost.Amount = Math.Min(Caster.GetPower(PowerType.Maelstrom), _maxTakenPower);
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDamage, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDamage(int effIndex)
    {
        if (_maxTakenPower > 0)
        {
            var increasedDamagePercent = MathFunctions.CalculatePct(_maxDamagePercent, _takenPower / _maxTakenPower * 100.0f);
            var hitDamage = MathFunctions.CalculatePct(HitDamage, 100 + increasedDamagePercent);
            HitDamage = hitDamage;
        }
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// 8042 Earth Shock
[SpellScript(51556)]
public class SpellShaEarthShock : SpellScript, IHasSpellEffects, ISpellOnTakePower
{
    private int _takenPower = 0;
    public List<ISpellEffect> SpellEffects { get; } = new();

    public void TakePower(SpellPowerCost powerCost)
    {
        _takenPower = powerCost.Amount;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleCalcDamage, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleCalcDamage(int effIndex)
    {
        HitDamage = MathFunctions.CalculatePct(HitDamage, _takenPower);
    }
}
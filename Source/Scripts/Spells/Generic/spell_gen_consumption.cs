// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script] // 64208 - Consumption
internal class SpellGenConsumption : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDamageCalc, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.LaunchTarget));
    }

    private void HandleDamageCalc(int effIndex)
    {
        var caster = Caster.AsCreature;

        if (caster == null)
            return;

        double damage = 0f;
        var createdBySpell = Global.SpellMgr.GetSpellInfo(caster.UnitData.CreatedBySpell, CastDifficulty);

        if (createdBySpell != null)
            damage = createdBySpell.GetEffect(2).CalcValue();

        if (damage != 0)
            EffectValue = damage;
    }
}
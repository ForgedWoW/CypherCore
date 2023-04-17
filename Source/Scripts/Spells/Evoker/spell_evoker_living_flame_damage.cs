// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.RED_LIVING_FLAME_DAMAGE)]
public class SpellEvokerLivingFlameDamage : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleManaRestored, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleManaRestored(int effIndex)
    {
        if (Caster.TryGetAuraEffect(EvokerSpells.ENERGIZING_FLAME, 0, out var auraEffect))
        {
            var spellInfo = Global.SpellMgr.AssertSpellInfo(EvokerSpells.RED_LIVING_FLAME, CastDifficulty);

            var cost = spellInfo.CalcPowerCost(PowerType.Mana, false, Caster, SpellInfo.GetSchoolMask(), null);

            if (cost == null)
                return;

            var manaRestored = MathFunctions.CalculatePct(cost.Amount, auraEffect.Amount);
            Caster.ModifyPower(PowerType.Mana, manaRestored);
        }
    }
}
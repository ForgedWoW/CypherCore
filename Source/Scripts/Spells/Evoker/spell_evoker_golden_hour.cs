// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.REVERSION)]
public class spell_evoker_golden_hour : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        if (Caster.TryGetAura(EvokerSpells.GOLDEN_HOUR, out var gaAura)
            && ExplTargetUnit.TryGetAsPlayer(out var target))
            Caster.CastSpell(target, EvokerSpells.GOLDEN_HOUR_HEAL,
                target.GetDamageOverLastSeconds((uint)gaAura.SpellInfo.GetEffect(1).BasePoints) 
                                                * (gaAura.SpellInfo.GetEffect(0).BasePoints * 0.01));
    }
}
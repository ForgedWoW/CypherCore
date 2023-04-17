// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.RED_PYRE,
             EvokerSpells.RED_FIRE_STORM_DAMAGE,
             EvokerSpells.RED_LIVING_FLAME_DAMAGE)]
public class SpellEvokerEverburningFlame : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        if (!TryGetCaster(out Player caster) || !caster.HasSpell(EvokerSpells.EVERBURNING_FLAME))
            return;

        if (TryGetExplTargetUnit(out var target) && target.TryGetAura(EvokerSpells.RED_FIRE_BREATH_CHARGED, out var aura))
            aura.ModDuration(SpellManager.Instance.GetSpellInfo(EvokerSpells.EVERBURNING_FLAME).GetEffect(0).BasePoints * 1000);
    }
}
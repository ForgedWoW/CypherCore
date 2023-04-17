// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.GREEN_EMERALD_BLOSSOM)]
public class SpellEvokerCycleOfLife : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        if (Caster.TryGetAsPlayer(out var player) && player.HasSpell(EvokerSpells.CYCLE_OF_LIFE))
        {
            player.AddAura(EvokerSpells.CYCLE_OF_LIFE_USE_COUNT);

            if (!player.TryGetAura(EvokerSpells.CYCLE_OF_LIFE_USE_COUNT, out var colAura))
                return;

            if (colAura.StackAmount < SpellManager.Instance.GetSpellInfo(EvokerSpells.CYCLE_OF_LIFE).GetEffect(1).BasePoints)
                return;

            player.RemoveAura(EvokerSpells.CYCLE_OF_LIFE_USE_COUNT);
            player.AddAura(EvokerSpells.CYCLE_OF_LIFE_AURA);
            player.SpellFactory.CastSpell(TargetPosition, EvokerSpells.CYCLE_OF_LIFE_SUMMON, true);
        }
    }
}
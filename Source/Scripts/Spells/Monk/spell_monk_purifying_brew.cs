// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(119582)]
public class SpellMonkPurifyingBrew : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var caster = Caster;

        if (caster != null)
        {
            var player = caster.AsPlayer;

            if (player != null)
            {
                var staggerAmount = player.GetAura(MonkSpells.LIGHT_STAGGER);

                if (staggerAmount == null)
                    staggerAmount = player.GetAura(MonkSpells.MODERATE_STAGGER);

                if (staggerAmount == null)
                    staggerAmount = player.GetAura(MonkSpells.HEAVY_STAGGER);

                if (staggerAmount != null)
                {
                    var newStagger = staggerAmount.GetEffect(1).Amount;
                    newStagger = (int)(newStagger * 0.5);
                    staggerAmount.GetEffect(1).ChangeAmount(newStagger);
                }
            }
        }
    }
}
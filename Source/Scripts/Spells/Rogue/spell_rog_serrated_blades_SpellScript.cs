// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Rogue;

[SpellScript(14172)]
public class SpellRogSerratedBladesSpellScript : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var blade = Caster.GetAuraEffectOfRankedSpell(RogueSpells.SERRATED_BLADES_R1, 0);

        if (blade != null)
        {
            var combo = Caster.AsPlayer.GetPower(PowerType.ComboPoints);

            if (RandomHelper.randChance(blade.Amount * combo))
            {
                var dot = HitUnit.GetAura(RogueSpells.RUPTURE, Caster.GUID);

                if (dot != null)
                    dot.RefreshDuration();
            }
        }
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman;

// 73920 - Healing Rain
[SpellScript(73920)]
internal class SpellShaHealingRain : SpellScript, ISpellAfterHit
{
    public void AfterHit()
    {
        var aura = GetHitAura();

        if (aura != null)
        {
            var dest = ExplTargetDest;

            if (dest != null)
            {
                var duration = SpellInfo.CalcDuration(OriginalCaster);
                var summon = Caster.Map.SummonCreature(CreatureIds.HEALING_RAIN_INVISIBLE_STALKER, dest, null, (uint)duration, OriginalCaster);

                if (summon == null)
                    return;

                summon.SpellFactory.CastSpell(summon, ShamanSpells.HEALING_RAIN_VISUAL, true);

                var script = aura.GetScript<SpellShaHealingRainAuraScript>();

                script?.SetVisualDummy(summon);
            }
        }
    }
}
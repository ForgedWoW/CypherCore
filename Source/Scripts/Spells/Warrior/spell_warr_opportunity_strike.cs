// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warrior;

// 203179 - Opportunity Strike
[SpellScript(203179)]
public class SpellWarrOpportunityStrike : AuraScript, IAuraOnProc
{
    public void OnProc(ProcEventInfo eventInfo)
    {
        if (!Caster)
            return;

        if (eventInfo?.DamageInfo?.SpellInfo != null && eventInfo.DamageInfo.SpellInfo.Id == WarriorSpells.OPPORTUNITY_STRIKE_DAMAGE)
            return;

        var target = eventInfo.ActionTarget;

        if (target != null)
        {
            var player = Caster.AsPlayer;

            if (player != null)
            {
                var aur = Aura;

                if (aur != null)
                {
                    var eff = aur.GetEffect(0);

                    if (eff != null)
                        if (RandomHelper.randChance(eff.Amount))
                            player.SpellFactory.CastSpell(target, WarriorSpells.OPPORTUNITY_STRIKE_DAMAGE, true);
                }
            }
        }
    }
}
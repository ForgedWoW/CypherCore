// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Linq;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 29601 - Enlightenment (Pendant of the Violet Eye)
internal class SpellItemPendantOfTheVioletEye : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        var spell = eventInfo.ProcSpell;

        if (spell != null)
        {
            var costs = spell.PowerCost;
            var m = costs.FirstOrDefault(cost => cost.Power == PowerType.Mana && cost.Amount > 0);

            if (m != null)
                return true;
        }

        return false;
    }
}
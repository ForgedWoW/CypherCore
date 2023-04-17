// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[Script] // 57934 - Tricks of the Trade
internal class SpellRogTricksOfTheTrade : SpellScript, ISpellAfterHit
{
    public void AfterHit()
    {
        var aura = GetHitAura();

        if (aura != null)
        {
            var script = aura.GetScript<SpellRogTricksOfTheTradeAura>();

            if (script != null)
            {
                var explTarget = ExplTargetUnit;

                if (explTarget != null)
                    script.SetRedirectTarget(explTarget.GUID);
                else
                    script.SetRedirectTarget(ObjectGuid.Empty);
            }
        }
    }
}
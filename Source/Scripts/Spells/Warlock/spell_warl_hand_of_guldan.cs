// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// Hand of Gul'Dan - 105174
[SpellScript(105174)]
public class SpellWarlHandOfGuldan : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var caster = Caster;

        if (caster != null)
        {
            var target = HitUnit;

            if (target != null)
            {
                var nrofsummons = 1;
                nrofsummons += caster.GetPower(PowerType.SoulShards);

                if (nrofsummons > 4)
                    nrofsummons = 4;

                sbyte[] offsetX =
                {
                    0, 0, 1, 1
                };

                sbyte[] offsetY =
                {
                    0, 1, 0, 1
                };

                for (var i = 0; i < nrofsummons; i++)
                    caster.SpellFactory.CastSpell(new Position(target.Location.X + offsetX[i], target.Location.Y + offsetY[i], target.Location.Z), 104317, true);

                caster.SpellFactory.CastSpell(target, WarlockSpells.HAND_OF_GULDAN_DAMAGE, true);
            }
        }
    }
}
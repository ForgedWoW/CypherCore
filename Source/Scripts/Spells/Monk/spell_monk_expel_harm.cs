// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[SpellScript(115072)]
public class SpellMonkExpelHarm : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        if (!Caster)
            return;

        var player = Caster.AsPlayer;

        if (player != null)
        {
            var targetList = new List<Unit>();
            player.GetAttackableUnitListInRange(targetList, 10.0f);

            foreach (var itr in targetList)
                if (player.IsValidAttackTarget(itr))
                {
                    var bp = MathFunctions.CalculatePct((-HitDamage), 50);
                    var args = new CastSpellExtraArgs();
                    args.AddSpellMod(SpellValueMod.BasePoint0, (int)bp);
                    args.SetTriggerFlags(TriggerCastFlags.FullMask);
                    player.SpellFactory.CastSpell(itr, 115129, args);
                }
        }
    }
}
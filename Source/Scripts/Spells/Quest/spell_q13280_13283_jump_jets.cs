// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Quest;

[Script] // 4336 - Jump Jets
internal class SpellQ1328013283JumpJets : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        var caster = Caster;

        if (caster.IsVehicle)
        {
            var rocketBunny = caster.VehicleKit1.GetPassenger(1);

            rocketBunny?.SpellFactory.CastSpell(rocketBunny, QuestSpellIds.JUMP_ROCKET_BLAST, true);
        }
    }
}
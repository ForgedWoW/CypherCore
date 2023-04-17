// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

//73899
[SpellScript(73899)]
public class SpellShamanPrimalStrike : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var player = Caster.AsPlayer;

        if (player != null)
            HitUnit.SpellFactory.CastSpell(HitUnit, 73899, (int)(player.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 0.34f));
    }
}
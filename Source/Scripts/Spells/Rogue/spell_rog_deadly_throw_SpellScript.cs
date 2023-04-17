// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Rogue;

[SpellScript(26679)]
public class SpellRogDeadlyThrowSpellScript : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var target = HitUnit;

        if (target != null)
        {
            var caster = Caster.AsPlayer;

            if (caster != null)
                if (caster.GetPower(PowerType.ComboPoints) >= 5)
                    caster.SpellFactory.CastSpell(target, 137576, true);
        }
    }
}
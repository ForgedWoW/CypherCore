// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Hunter;

[SpellScript(53209)]
public class SpellHunChimeraShot : SpellScript, ISpellOnHit
{
    public override bool Load()
    {
        return Caster.TypeId == TypeId.Player;
    }

    public void OnHit()
    {
        var caster = Caster;
        var target = HitUnit;

        if (RandomHelper.URand(0, 1) == 1)
            caster.SpellFactory.CastSpell(target, 171454, true);
        else
            caster.SpellFactory.CastSpell(target, 171457, true);
    }
}
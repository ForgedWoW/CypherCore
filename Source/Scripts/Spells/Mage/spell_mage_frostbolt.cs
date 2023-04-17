// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Mage;

[Script] // 116 - Frostbolt
internal class SpellMageFrostbolt : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var target = HitUnit;

        if (target != null)
            Caster.SpellFactory.CastSpell(target, MageSpells.Chilled, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress));
    }
}
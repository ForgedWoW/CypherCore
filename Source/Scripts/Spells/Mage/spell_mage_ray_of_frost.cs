// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Mage;

[Script] // 205021 - Ray of Frost
internal class SpellMageRayOfFrost : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var caster = Caster;

        caster?.SpellFactory.CastSpell(caster, MageSpells.RAY_OF_FROST_FINGERS_OF_FROST, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress));
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Hunter;

[Script]
internal class SpellHunSteadyShot : SpellScript, ISpellOnHit
{
    public override bool Load()
    {
        return Caster.IsTypeId(TypeId.Player);
    }

    public void OnHit()
    {
        Caster.SpellFactory.CastSpell(Caster, HunterSpells.SteadyShotFocus, true);
    }
}
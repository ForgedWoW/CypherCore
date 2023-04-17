// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.BLACK_LANDSLIDE)]
internal class SpellEvokerLandslide : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        Caster.SpellFactory.CastSpell(Spell.Targets.DstPos, EvokerSpells.BLACK_LANDSLIDE_AREA_TRIGGER, true);
    }
}
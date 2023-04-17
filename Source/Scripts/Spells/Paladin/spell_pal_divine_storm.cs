// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin;

[SpellScript(224239)] // 224239 - Divine Storm
internal class SpellPalDivineStorm : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        Caster.SendPlaySpellVisualKit(PaladinSpellVisualKit.DIVINE_STORM, 0, 0);
    }
}
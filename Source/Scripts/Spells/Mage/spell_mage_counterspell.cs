// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;

namespace Scripts.Spells.Mage;

[SpellScript(2139)]
public class SpellMageCounterspell : SpellScript, ISpellOnSucessfulInterrupt
{
    public void SucessfullyInterrupted(Spell spellInterrupted)
    {
        var caster = Caster;
        var spellHistory = caster.SpellHistory;
        var spellInfo = Global.SpellMgr.GetSpellInfo(2139);

        if (Caster.TryGetAura(MageSpells.QUICK_WITTED, out var quickWitted))
            Caster.Events.AddEventAtOffset(() => spellHistory.ModifyCooldown(2139, System.TimeSpan.FromMilliseconds(-4000)), System.TimeSpan.FromMilliseconds(1));
    }
}
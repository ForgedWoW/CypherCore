// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[SpellScript(210802)]
public class BfaSpellBlackoutKickSpiritOfTheCraneTalent : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        var caster = Caster.AsPlayer;

        if (caster == null)
            return;

        if (caster.HasAura(MonkSpells.SPIRIT_OF_THE_CRANE))
            caster.SetPower(PowerType.Mana, caster.GetPower(PowerType.Mana) + ((caster.GetMaxPower(PowerType.Mana) * 0.65f) / 100));
    }
}
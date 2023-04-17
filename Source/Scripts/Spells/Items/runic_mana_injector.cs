// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Items;

[SpellScript(42545)]
internal class RunicManaInjector : SpellScript, ISpellEnergizedBySpell
{
    public void EnergizeBySpell(Unit target, SpellInfo spellInfo, ref double amount, PowerType powerType)
    {
        var player = target.AsPlayer;

        if (player != null)
            if (player.HasSkill(SkillType.Engineering))
                MathFunctions.AddPct(ref amount, 25);
    }
}
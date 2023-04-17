// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.RED_FIRE_BREATH_CHARGED)]
internal class SpellEvokerFireBreathCharged : SpellScript, ISpellCalculateBonusCoefficient
{
    public double CalcBonusCoefficient(double bonusCoefficient)
    {
        var multi = 0;

        switch (Spell.EmpoweredStage)
        {
            case 1:
                multi = 3;

                break;
            case 2:
                multi = 6;

                break;
            case 3:
                multi = 9;

                break;
        }

        return bonusCoefficient + (GetEffectInfo(1).BonusCoefficient * multi);
    }
}
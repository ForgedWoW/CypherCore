using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker
{
    [SpellScript(EvokerSpells.FIRE_BREATH_CHARGED)]
    internal class spell_evoker_fire_breath_charged : SpellScript, ISpellCalculateBonusCoefficient
    {
        public double CalcBonusCoefficient(double bonusCoefficient)
        {
            int multi = 0;
            switch (GetSpell().EmpoweredStage)
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
                default:
                    break;
            }

            return bonusCoefficient + (GetEffectInfo(1).BonusCoefficient * multi);
        }
    }
}

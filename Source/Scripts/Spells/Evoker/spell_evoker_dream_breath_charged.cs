using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker
{
    [SpellScript(EvokerSpells.DREAM_BREATH_CHARGED)]
    internal class spell_evoker_dream_breath_charged : SpellScript, ISpellCalculateBonusCoefficient
    {
        public double CalcBonusCoefficient(double bonusCoefficient)
        {
            int multi = 0;
            switch (GetSpell().EmpoweredStage)
            {
                case 1:
                    multi = 2;
                    break;
                case 2:
                    multi = 4;
                    break;
                case 3:
                    multi = 6;
                    break;
                default:
                    break;
            }

            return bonusCoefficient + (GetEffectInfo(0).BonusCoefficient * multi);
        }
    }
}

using Bgs.Protocol.Notification.V1;
using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Scripts.Spells.Shaman;
using System.Collections.Generic;

namespace Scripts.Spells.Evoker
{
    [SpellScript(EvokerSpells.SPIRITBLOOM, EvokerSpells.SPIRITBLOOM_2)]
    internal class spell_evoker_spiritbloom : SpellScript, ISpellOnEpowerSpellEnd
    {
        public void EmpowerSpellEnd(SpellEmpowerStageRecord stage, uint stageDelta)
        {
            var caster = GetCaster();

            // cast on primary target
            caster.CastSpell(EvokerSpells.SPIRITBLOOM_CHARGED, true, stage.Stage);

            // determine number of additional targets
            int targets = 0;
            switch (GetSpell().EmpoweredStage)
            {
                case 1:
                    targets = 1;
                    break;
                case 2:
                    targets = 2;
                    break;
                case 3:
                    targets = 3;
                    break;
                default:
                    break;
            }

            if (targets > 0)
            {
                // get targets that are injured
                List<Unit> targetList = new List<Unit>();
                caster.GetAlliesWithinRange(targetList, GetSpellInfo().GetMaxRange());
                targetList.RemoveIf(a => a.IsFullHealth());

                // reduce targetList to the number allowed
                while (targetList.Count > targets)
                    targetList.RemoveAt(targetList.Count - 1);

                // cast on targets
                foreach (var target in targetList)
                    caster.CastSpell(target, EvokerSpells.SPIRITBLOOM_CHARGED, true, stage.Stage);
            }
        }
    }
}

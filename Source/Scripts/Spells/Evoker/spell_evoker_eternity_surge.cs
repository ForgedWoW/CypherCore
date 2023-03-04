using Bgs.Protocol.Notification.V1;
using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Scripts.Spells.Shaman;
using System.Collections.Generic;

namespace Scripts.Spells.Evoker
{
    [SpellScript(EvokerSpells.ETERNITY_SURGE, EvokerSpells.ETERNITY_SURGE_2)]
    internal class spell_evoker_eternity_surge : SpellScript, ISpellOnEpowerSpellEnd
    {
        public void EmpowerSpellEnd(SpellEmpowerStageRecord stage, uint stageDelta)
        {
            var caster = GetCaster();

            // cast on primary target
            caster.CastSpell(EvokerSpells.ETERNITY_SURGE_CHARGED, true, stage.Stage);

            // determine number of additional targets
            int multi = 1;
            if(caster.TryGetAura(EvokerSpells.ETERNITYS_SPAN, out var aura))
                multi = 2;

            int targets = 1 * multi;
            switch (GetSpell().EmpoweredStage)
            {
                case 1:
                    targets = 2 * multi;
                    break;
                case 2:
                    targets = 3 * multi;
                    break;
                case 3:
                    targets = 4 * multi;
                    break;
                default:
                    break;
            }

            targets--;

            if (targets > 0)
            {
                // get targets
                List<Unit> targetList = new List<Unit>();
                caster.GetEnemiesWithinRange(targetList, GetEffectInfo(1).MaxRadiusEntry.RadiusMax);

                // reduce targetList to the number allowed
                while (targetList.Count > targets)
                    targetList.RemoveAt(targetList.Count - 1);

                // cast on targets
                foreach (var target in targetList)
                    caster.CastSpell(target, EvokerSpells.ETERNITY_SURGE_CHARGED, true, stage.Stage);
            }
        }
    }
}

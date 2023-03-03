using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.DataStorage;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
using static System.Net.Mime.MediaTypeNames;
using static Game.Scripting.Interfaces.ISpell.EffectHandler;

namespace Scripts.Spells.Evoker
{
    [SpellScript(EvokerSpells.FIRE_BREATH_CHARGED)]
    internal class spell_evoker_fire_breath_charged : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new List<ISpellEffect>();

        private void CalcDirectDamage(int index)
        {
            var caster = GetCaster();

            if (!caster.TryGetAura(EvokerSpells.FIRE_BREATH, out var aura))
                caster.TryGetAura(EvokerSpells.FIRE_BREATH_2, out aura);

            if (aura != null)
            {
                int multi = 1;
                switch (aura.EmpowerStage)
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

                if (multi != 1)
                {
                    var target = GetHitUnit();
                    var spellInfo = GetSpellInfo();
                    var spell = GetSpell();
                    double damage = caster.CalculateSpellDamage(target, GetEffectInfo(1)) * multi;
                    var bonus = caster.SpellDamageBonusDone(target, spellInfo, damage, DamageEffectType.SpellDirect, GetEffectInfo(1), 1, spell);
                    damage = bonus + (bonus * spell.variance);
                    spell.damage += target.SpellDamageBonusTaken(caster, spellInfo, damage, DamageEffectType.SpellDirect);
                }
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(CalcDirectDamage, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.Hit));
        }
    }
}

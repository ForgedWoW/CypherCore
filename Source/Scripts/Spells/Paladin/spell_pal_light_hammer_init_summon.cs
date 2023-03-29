// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Paladin;

[SpellScript(122773)] // 122773 - Light's Hammer
internal class spell_pal_light_hammer_init_summon : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        foreach (var summonedObject in Spell.GetExecuteLogEffect(SpellEffectName.Summon).GenericVictimTargets)
        {
            var hammer = Global.ObjAccessor.GetUnit(Caster, summonedObject.Victim);

            if (hammer != null)
            {
                hammer.CastSpell(hammer,
                                 PaladinSpells.LightHammerCosmetic,
                                 new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress).SetTriggeringSpell(Spell));

                hammer.CastSpell(hammer,
                                 PaladinSpells.LightHammerPeriodic,
                                 new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress).SetTriggeringSpell(Spell));
            }
        }
    }
}
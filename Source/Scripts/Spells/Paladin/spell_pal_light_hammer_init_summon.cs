// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Paladin;

[SpellScript(122773)] // 122773 - Light's Hammer
internal class SpellPalLightHammerInitSummon : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        foreach (var summonedObject in Spell.GetExecuteLogEffect(SpellEffectName.Summon).GenericVictimTargets)
        {
            var hammer = Global.ObjAccessor.GetUnit(Caster, summonedObject.Victim);

            if (hammer != null)
            {
                hammer.SpellFactory.CastSpell(hammer,
                                              PaladinSpells.LIGHT_HAMMER_COSMETIC,
                                              new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress).SetTriggeringSpell(Spell));

                hammer.SpellFactory.CastSpell(hammer,
                                              PaladinSpells.LIGHT_HAMMER_PERIODIC,
                                              new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress).SetTriggeringSpell(Spell));
            }
        }
    }
}
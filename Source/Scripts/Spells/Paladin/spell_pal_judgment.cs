// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin;

[SpellScript(new uint[]
{
    20271, 275779, 275773
})] // 20271/275779/275773 - Judgement (Retribution/Protection/Holy)
internal class SpellPalJudgment : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var caster = Caster;

        if (caster.HasSpell(PaladinSpells.JUDGMENT_PROT_RET_R3))
            caster.SpellFactory.CastSpell(caster, PaladinSpells.JUDGMENT_GAIN_HOLY_POWER, Spell);

        if (caster.HasSpell(PaladinSpells.JUDGMENT_HOLY_R3))
            caster.SpellFactory.CastSpell(HitUnit, PaladinSpells.JUDGMENT_HOLY_R_3DEBUFF, Spell);
    }
}
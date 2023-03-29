// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin;

[SpellScript(190784)] // 190784 - Divine Steed
internal class spell_pal_divine_steed : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        var caster = Caster;

        var spellId = PaladinSpells.DivineSteedHuman;

        switch (caster.Race)
        {
            case Race.Human:
                spellId = PaladinSpells.DivineSteedHuman;

                break;
            case Race.Dwarf:
                spellId = PaladinSpells.DivineSteedDwarf;

                break;
            case Race.Draenei:
            case Race.LightforgedDraenei:
                spellId = PaladinSpells.DivineSteedDraenei;

                break;
            case Race.DarkIronDwarf:
                spellId = PaladinSpells.DivineSteedDarkIronDwarf;

                break;
            case Race.BloodElf:
                spellId = PaladinSpells.DivineSteedBloodelf;

                break;
            case Race.Tauren:
                spellId = PaladinSpells.DivineSteedTauren;

                break;
            case Race.ZandalariTroll:
                spellId = PaladinSpells.DivineSteedZandalariTroll;

                break;
            default:
                break;
        }

        caster.CastSpell(caster, spellId, true);
    }
}
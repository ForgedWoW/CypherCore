// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Paladin;

[SpellScript(190784)] // 190784 - Divine Steed
internal class SpellPalDivineSteed : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        var caster = Caster;

        var spellId = PaladinSpells.DIVINE_STEED_HUMAN;

        switch (caster.Race)
        {
            case Race.Human:
                spellId = PaladinSpells.DIVINE_STEED_HUMAN;

                break;
            case Race.Dwarf:
                spellId = PaladinSpells.DIVINE_STEED_DWARF;

                break;
            case Race.Draenei:
            case Race.LightforgedDraenei:
                spellId = PaladinSpells.DIVINE_STEED_DRAENEI;

                break;
            case Race.DarkIronDwarf:
                spellId = PaladinSpells.DIVINE_STEED_DARK_IRON_DWARF;

                break;
            case Race.BloodElf:
                spellId = PaladinSpells.DIVINE_STEED_BLOODELF;

                break;
            case Race.Tauren:
                spellId = PaladinSpells.DIVINE_STEED_TAUREN;

                break;
            case Race.ZandalariTroll:
                spellId = PaladinSpells.DIVINE_STEED_ZANDALARI_TROLL;

                break;
        }

        caster.SpellFactory.CastSpell(caster, spellId, true);
    }
}
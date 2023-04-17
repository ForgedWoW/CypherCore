// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(120679)]
public class SpellHunDireBeast : SpellScript, ISpellAfterCast, ISpellOnHit
{
    public void AfterCast()
    {
        var player = Caster.AsPlayer;

        if (player != null)
            if (player.HasAura(HunterSpells.WILD_CALL_AURA))
                player.RemoveAura(HunterSpells.WILD_CALL_AURA);
    }

    public void OnHit()
    {
        var player = Caster.AsPlayer;

        if (player != null)
        {
            var target = HitUnit;

            if (target != null)
                // Summon's skin is different function of Map or Zone ID
                switch (player.Zone)
                {
                    case 5785: // The Jade Forest
                        player.SpellFactory.CastSpell(target, DireBeastSpells.DIRE_BEAST_JADE_FOREST, true);

                        break;
                    case 5805: // Valley of the Four Winds
                        player.SpellFactory.CastSpell(target, DireBeastSpells.DIRE_BEAST_VALLEY_OF_THE_FOUR_WINDS, true);

                        break;
                    case 5840: // Vale of Eternal Blossoms
                        player.SpellFactory.CastSpell(target, DireBeastSpells.DIRE_BEAST_VALE_OF_THE_ETERNAL_BLOSSOM, true);

                        break;
                    case 5841: // Kun-Lai Summit
                        player.SpellFactory.CastSpell(target, DireBeastSpells.DIRE_BEAST_KUN_LAI_SUMMIT, true);

                        break;
                    case 5842: // Townlong Steppes
                        player.SpellFactory.CastSpell(target, DireBeastSpells.DIRE_BEAST_TOWNLONG_STEPPES, true);

                        break;
                    case 6134: // Krasarang Wilds
                        player.SpellFactory.CastSpell(target, DireBeastSpells.DIRE_BEAST_KRASARANG_WILDS, true);

                        break;
                    case 6138: // Dread Wastes
                        player.SpellFactory.CastSpell(target, DireBeastSpells.DIRE_BEAST_DREAD_WASTES, true);

                        break;
                    default:
                    {
                        switch (player.Location.MapId)
                        {
                            case 0: // Eastern Kingdoms
                                player.SpellFactory.CastSpell(target, DireBeastSpells.DIRE_BEAST_EASTERN_KINGDOMS, true);

                                break;
                            case 1: // Kalimdor
                                player.SpellFactory.CastSpell(target, DireBeastSpells.DIRE_BEAST_KALIMDOR, true);

                                break;
                            case 8: // Outland
                                player.SpellFactory.CastSpell(target, DireBeastSpells.DIRE_BEAST_OUTLAND, true);

                                break;
                            case 10: // Northrend
                                player.SpellFactory.CastSpell(target, DireBeastSpells.DIRE_BEAST_NORTHREND, true);

                                break;
                            default:
                                if (player.Map.IsDungeon)
                                    player.SpellFactory.CastSpell(target, DireBeastSpells.DIRE_BEAST_DUNGEONS, true);
                                else // Default
                                    player.SpellFactory.CastSpell(target, DireBeastSpells.DIRE_BEAST_KALIMDOR, true);

                                break;
                        }

                        break;
                    }
                }
        }
    }
}
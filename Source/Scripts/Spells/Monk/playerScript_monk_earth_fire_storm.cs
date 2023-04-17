// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[Script]
public class PlayerScriptMonkEarthFireStorm : ScriptObjectAutoAdd, IPlayerOnSpellCast
{
    public PlayerScriptMonkEarthFireStorm() : base("playerScript_monk_earth_fire_storm") { }
    public PlayerClass PlayerClass => PlayerClass.Monk;

    public void OnSpellCast(Player player, Spell spell, bool re)
    {
        if (player.Class != PlayerClass.Monk)
            return;

        var spellInfo = spell.SpellInfo;

        if (player.HasAura(StormEarthAndFireSpells.SEF) && !spellInfo.IsPositive)
        {
            var target = ObjectAccessor.Instance.GetUnit(player, player.Target);

            if (target != null)
            {
                var fireSpirit = player.GetSummonedCreatureByEntry(StormEarthAndFireSpells.NPC_FIRE_SPIRIT);

                if (fireSpirit != null)
                {
                    fireSpirit.SetFacingToObject(target, true);
                    fireSpirit.SpellFactory.CastSpell(target, spellInfo.Id, true);
                }

                var earthSpirit = player.GetSummonedCreatureByEntry(StormEarthAndFireSpells.NPC_EARTH_SPIRIT);

                if (earthSpirit != null)
                {
                    earthSpirit.SetFacingToObject(target, true);
                    earthSpirit.SpellFactory.CastSpell(target, spellInfo.Id, true);
                }
            }
        }

        if (player.HasAura(StormEarthAndFireSpells.SEF) && spellInfo.IsPositive)
        {
            var getTarget = player.SelectedUnit;

            if (getTarget != null)
            {
                if (!getTarget.IsFriendlyTo(player))
                    return;

                var fireSpirit = player.GetSummonedCreatureByEntry(StormEarthAndFireSpells.NPC_FIRE_SPIRIT);

                if (fireSpirit != null)
                {
                    fireSpirit.SetFacingToObject(getTarget, true);
                    fireSpirit.SpellFactory.CastSpell(getTarget, spellInfo.Id, true);
                }

                var earthSpirit = player.GetSummonedCreatureByEntry(StormEarthAndFireSpells.NPC_EARTH_SPIRIT);

                if (earthSpirit != null)
                {
                    earthSpirit.SetFacingToObject(getTarget, true);
                    earthSpirit.SpellFactory.CastSpell(getTarget, spellInfo.Id, true);
                }
            }
        }
    }
}
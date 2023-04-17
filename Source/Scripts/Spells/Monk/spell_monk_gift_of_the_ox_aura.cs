// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[Script]
public class SpellMonkGiftOfTheOxAura : ScriptObjectAutoAdd, IPlayerOnTakeDamage
{
    public enum UsedSpells
    {
        HealingSphereCooldown = 224863
    }

    public List<uint> SpellsToCast = new()
    {
        MonkSpells.GIFT_OF_THE_OX_AT_RIGHT,
        MonkSpells.GIFT_OF_THE_OX_AT_LEFT
    };

    public SpellMonkGiftOfTheOxAura() : base("spell_monk_gift_of_the_ox_aura") { }

    public PlayerClass PlayerClass { get; } = PlayerClass.Monk;

    public void OnPlayerTakeDamage(Player victim, double damage, SpellSchoolMask unnamedParameter)
    {
        if (damage == 0 || victim == null)
            return;

        if (!victim.HasAura(MonkSpells.GIFT_OF_THE_OX_AURA))
            return;

        var spellToCast = SpellsToCast[RandomHelper.IRand(0, (SpellsToCast.Count - 1))];

        if (RandomHelper.randChance((0.75 * damage / victim.MaxHealth) * (3 - 2 * (victim.HealthPct / 100)) * 100))
            if (!victim.HasAura(UsedSpells.HealingSphereCooldown))
            {
                victim.SpellFactory.CastSpell(victim, UsedSpells.HealingSphereCooldown, true);
                victim.SpellFactory.CastSpell(victim, spellToCast, true);
            }
    }
}
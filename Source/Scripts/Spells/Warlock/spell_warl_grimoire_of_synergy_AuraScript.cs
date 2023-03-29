﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warlock;

// Grimoire of Synergy - 171975
[SpellScript(171975, "spell_warl_grimoire_of_synergy")]
public class spell_warl_grimoire_of_synergy_AuraScript : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        var actor = eventInfo.Actor;

        if (actor == null)
            return false;

        if (actor.IsPet ||
            actor.IsGuardian)
        {
            var owner = actor.OwnerUnit;

            if (owner == null)
                return false;

            if (RandomHelper.randChance(10))
                owner.CastSpell(owner, WarlockSpells.GRIMOIRE_OF_SYNERGY_BUFF, true);

            return true;
        }

        var player = actor.AsPlayer;

        if (actor.AsPlayer)
        {
            var guardian = player.GetGuardianPet();

            if (guardian == null)
                return false;

            if (RandomHelper.randChance(10))
                player.CastSpell(guardian, WarlockSpells.GRIMOIRE_OF_SYNERGY_BUFF, true);

            return true;
        }

        return false;
    }
}
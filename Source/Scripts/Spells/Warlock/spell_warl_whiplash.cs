// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// Whiplash - 119909
[SpellScript(119909)]
public class SpellWarlWhiplash : SpellScript, ISpellAfterHit, ISpellCheckCast
{
    public void AfterHit()
    {
        var caster = Caster;
        var dest = ExplTargetDest;
        var pet = caster.GetGuardianPet();

        if (caster == null || pet == null || dest == null)
            return;

        /*if (pet->GetEntry() != PET_ENTRY_SUCCUBUS)
            return;*/

        pet.SpellFactory.CastSpell(new Position(dest.X, dest.Y, dest.Z), WarlockSpells.SUCCUBUS_WHIPLASH, true);
        caster.AsPlayer.SpellHistory.ModifyCooldown(SpellInfo.Id, TimeSpan.FromSeconds(25));
    }

    public SpellCastResult CheckCast()
    {
        var caster = Caster;
        var pet = caster.GetGuardianPet();

        if (caster == null || pet == null)
            return SpellCastResult.DontReport;

        if (pet.SpellHistory.HasCooldown(WarlockSpells.SUCCUBUS_WHIPLASH))
            return SpellCastResult.CantDoThatRightNow;

        return SpellCastResult.SpellCastOk;
    }
}
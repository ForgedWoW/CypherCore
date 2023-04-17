// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// 111898 - Grimoire: Felguard
[SpellScript(111898)]
public class SpellWarlockGrimoireFelguard : SpellScript, ISpellCheckCast
{
    public SpellCastResult CheckCast()
    {
        var caster = Caster.AsPlayer;

        if (caster == null)
            return SpellCastResult.CantDoThatRightNow;

        // allow only in Demonology spec
        if (caster.GetPrimarySpecialization() != TalentSpecialization.WarlockDemonology)
            return SpellCastResult.NoSpec;

        return SpellCastResult.SpellCastOk;
    }
}
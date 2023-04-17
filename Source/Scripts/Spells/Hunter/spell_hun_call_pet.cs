// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Hunter;

[SpellScript(new uint[]
{
    883, 83242, 83243, 83244, 83245
})]
public class SpellHunCallPet : SpellScript, ISpellCheckCast
{
    public SpellCastResult CheckCast()
    {
        return Caster.HasAura(HunterSpells.LONE_WOLF) ? SpellCastResult.SpellUnavailable : SpellCastResult.SpellCastOk;
    }
}
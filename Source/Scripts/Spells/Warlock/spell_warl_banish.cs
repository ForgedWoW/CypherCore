// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

[Script] // 710 - Banish
internal class SpellWarlBanish : SpellScript, ISpellBeforeHit
{
    public void BeforeHit(SpellMissInfo missInfo)
    {
        if (missInfo != SpellMissInfo.Immune)
            return;

        var target = HitUnit;

        if (target)
        {
            // Casting Banish on a banished Target will Remove applied aura
            var banishAura = target.GetAura(SpellInfo.Id, Caster.GUID);

            banishAura?.Remove();
        }
    }
}
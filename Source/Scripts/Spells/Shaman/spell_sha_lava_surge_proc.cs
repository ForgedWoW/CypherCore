// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// 77762 - Lava Surge
[SpellScript(77762)]
internal class SpellShaLavaSurgeProc : SpellScript, ISpellAfterHit
{
    public override bool Load()
    {
        return Caster.IsTypeId(TypeId.Player);
    }

    public void AfterHit()
    {
        Caster.SpellHistory.RestoreCharge(Global.SpellMgr.GetSpellInfo(ShamanSpells.LavaBurst, CastDifficulty).ChargeCategoryId);
    }
}
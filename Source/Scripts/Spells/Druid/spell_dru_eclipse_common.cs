// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Druid;

internal class SpellDruEclipseCommon
{
    public static void SetSpellCount(Unit unitOwner, uint spellId, uint amount)
    {
        var aura = unitOwner.GetAura(spellId);

        if (aura == null)
            unitOwner.SpellFactory.CastSpell(unitOwner, spellId, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.AuraStack, (int)amount));
        else
            aura.SetStackAmount((byte)amount);
    }
}
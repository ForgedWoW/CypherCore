// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Spell;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script] // Used for some spells cast by vehicles or charmed creatures that do not send a cooldown event on their own
internal class SpellGenCharmedUnitSpellCooldown : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        var caster = Caster;
        var owner = caster.CharmerOrOwnerPlayerOrPlayerItself;

        if (owner != null)
        {
            SpellCooldownPkt spellCooldown = new();
            spellCooldown.Caster = owner.GUID;
            spellCooldown.Flags = SpellCooldownFlags.None;
            spellCooldown.SpellCooldowns.Add(new SpellCooldownStruct(SpellInfo.Id, SpellInfo.RecoveryTime));
            owner.SendPacket(spellCooldown);
        }
    }
}
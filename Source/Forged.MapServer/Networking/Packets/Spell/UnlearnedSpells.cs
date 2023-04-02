// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

public class UnlearnedSpells : ServerPacket
{
    public List<uint> SpellID = new();
    public bool SuppressMessaging;
    public UnlearnedSpells() : base(ServerOpcodes.UnlearnedSpells, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteInt32(SpellID.Count);

        foreach (var spellId in SpellID)
            WorldPacket.WriteUInt32(spellId);

        WorldPacket.WriteBit(SuppressMessaging);
        WorldPacket.FlushBits();
    }
}
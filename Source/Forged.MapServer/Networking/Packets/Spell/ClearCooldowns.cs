// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

public class ClearCooldowns : ServerPacket
{
    public bool IsPet;
    public List<uint> SpellID = new();
    public ClearCooldowns() : base(ServerOpcodes.ClearCooldowns, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteInt32(SpellID.Count);

        if (!SpellID.Empty())
            SpellID.ForEach(p => WorldPacket.WriteUInt32(p));

        WorldPacket.WriteBit(IsPet);
        WorldPacket.FlushBits();
    }
}
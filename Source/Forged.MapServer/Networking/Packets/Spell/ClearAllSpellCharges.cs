// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

public class ClearAllSpellCharges : ServerPacket
{
    public bool IsPet;
    public ClearAllSpellCharges() : base(ServerOpcodes.ClearAllSpellCharges, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteBit(IsPet);
        WorldPacket.FlushBits();
    }
}
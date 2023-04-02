// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

public class ClearCooldown : ServerPacket
{
    public bool ClearOnHold;
    public bool IsPet;
    public uint SpellID;
    public ClearCooldown() : base(ServerOpcodes.ClearCooldown, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(SpellID);
        WorldPacket.WriteBit(ClearOnHold);
        WorldPacket.WriteBit(IsPet);
        WorldPacket.FlushBits();
    }
}
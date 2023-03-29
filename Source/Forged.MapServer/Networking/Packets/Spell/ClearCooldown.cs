// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

public class ClearCooldown : ServerPacket
{
    public bool IsPet;
    public uint SpellID;
    public bool ClearOnHold;
    public ClearCooldown() : base(ServerOpcodes.ClearCooldown, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WriteUInt32(SpellID);
        _worldPacket.WriteBit(ClearOnHold);
        _worldPacket.WriteBit(IsPet);
        _worldPacket.FlushBits();
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

public class SetAnimTier : ServerPacket
{
    public int Tier;
    public ObjectGuid Unit;
    public SetAnimTier() : base(ServerOpcodes.SetAnimTier, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(Unit);
        _worldPacket.WriteBits(Tier, 3);
        _worldPacket.FlushBits();
    }
}
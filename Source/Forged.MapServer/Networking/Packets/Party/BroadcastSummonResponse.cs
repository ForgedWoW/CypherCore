// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Party;

internal class BroadcastSummonResponse : ServerPacket
{
    public bool Accepted;
    public ObjectGuid Target;
    public BroadcastSummonResponse() : base(ServerOpcodes.BroadcastSummonResponse) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Target);
        WorldPacket.WriteBit(Accepted);
        WorldPacket.FlushBits();
    }
}
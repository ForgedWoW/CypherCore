// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Party;

internal class BroadcastSummonResponse : ServerPacket
{
    public ObjectGuid Target;
    public bool Accepted;

    public BroadcastSummonResponse() : base(ServerOpcodes.BroadcastSummonResponse) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(Target);
        _worldPacket.WriteBit(Accepted);
        _worldPacket.FlushBits();
    }
}
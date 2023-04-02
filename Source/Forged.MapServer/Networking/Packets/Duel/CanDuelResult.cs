// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Duel;

public class CanDuelResult : ServerPacket
{
    public bool Result;
    public ObjectGuid TargetGUID;
    public CanDuelResult() : base(ServerOpcodes.CanDuelResult) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(TargetGUID);
        WorldPacket.WriteBit(Result);
        WorldPacket.FlushBits();
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Combat;

internal class PvPCredit : ServerPacket
{
    public int Honor;
    public int OriginalHonor;
    public uint Rank;
    public ObjectGuid Target;
    public PvPCredit() : base(ServerOpcodes.PvpCredit) { }

    public override void Write()
    {
        WorldPacket.WriteInt32(OriginalHonor);
        WorldPacket.WriteInt32(Honor);
        WorldPacket.WritePackedGuid(Target);
        WorldPacket.WriteUInt32(Rank);
    }
}
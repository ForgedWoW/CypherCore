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
        _worldPacket.WriteInt32(OriginalHonor);
        _worldPacket.WriteInt32(Honor);
        _worldPacket.WritePackedGuid(Target);
        _worldPacket.WriteUInt32(Rank);
    }
}
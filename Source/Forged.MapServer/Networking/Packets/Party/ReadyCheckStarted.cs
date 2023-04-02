// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Party;

internal class ReadyCheckStarted : ServerPacket
{
    public uint Duration;
    public ObjectGuid InitiatorGUID;
    public ObjectGuid PartyGUID;
    public sbyte PartyIndex;
    public ReadyCheckStarted() : base(ServerOpcodes.ReadyCheckStarted) { }

    public override void Write()
    {
        WorldPacket.WriteInt8(PartyIndex);
        WorldPacket.WritePackedGuid(PartyGUID);
        WorldPacket.WritePackedGuid(InitiatorGUID);
        WorldPacket.WriteUInt32(Duration);
    }
}
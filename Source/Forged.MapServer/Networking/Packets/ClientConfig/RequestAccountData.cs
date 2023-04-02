// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.ClientConfig;

public class RequestAccountData : ClientPacket
{
    public AccountDataTypes DataType = 0;
    public ObjectGuid PlayerGuid;
    public RequestAccountData(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        PlayerGuid = WorldPacket.ReadPackedGuid();
        DataType = (AccountDataTypes)WorldPacket.ReadBits<uint>(4);
    }
}
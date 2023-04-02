// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Loot;

internal class LootList : ServerPacket
{
    public ObjectGuid LootObj;
    public ObjectGuid? Master;
    public ObjectGuid Owner;
    public ObjectGuid? RoundRobinWinner;
    public LootList() : base(ServerOpcodes.LootList, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Owner);
        WorldPacket.WritePackedGuid(LootObj);

        WorldPacket.WriteBit(Master.HasValue);
        WorldPacket.WriteBit(RoundRobinWinner.HasValue);
        WorldPacket.FlushBits();

        if (Master.HasValue)
            WorldPacket.WritePackedGuid(Master.Value);

        if (RoundRobinWinner.HasValue)
            WorldPacket.WritePackedGuid(RoundRobinWinner.Value);
    }
}
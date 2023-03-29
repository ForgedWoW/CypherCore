// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Loot;

internal class LootRemoved : ServerPacket
{
    public ObjectGuid LootObj;
    public ObjectGuid Owner;
    public byte LootListID;
    public LootRemoved() : base(ServerOpcodes.LootRemoved, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(Owner);
        _worldPacket.WritePackedGuid(LootObj);
        _worldPacket.WriteUInt8(LootListID);
    }
}
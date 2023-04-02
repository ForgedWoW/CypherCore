// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Loot;

internal class LootRollPacket : ClientPacket
{
    public byte LootListID;
    public ObjectGuid LootObj;
    public RollVote RollType;
    public LootRollPacket(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        LootObj = _worldPacket.ReadPackedGuid();
        LootListID = _worldPacket.ReadUInt8();
        RollType = (RollVote)_worldPacket.ReadUInt8();
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Loot;

internal class LootRollBroadcast : ServerPacket
{
    public bool Autopassed;
    public LootItemData Item = new();
    public ObjectGuid LootObj;
    public ObjectGuid Player;
    public int Roll; // Roll value can be negative, it means that it is an "offspec" roll but only during roll selection broadcast (not when sending the result)
    public RollVote RollType;
     // Triggers message |HlootHistory:%d|h[Loot]|h: You automatically passed on: %s because you cannot loot that item.
    public LootRollBroadcast() : base(ServerOpcodes.LootRoll) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(LootObj);
        WorldPacket.WritePackedGuid(Player);
        WorldPacket.WriteInt32(Roll);
        WorldPacket.WriteUInt8((byte)RollType);
        Item.Write(WorldPacket);
        WorldPacket.WriteBit(Autopassed);
        WorldPacket.FlushBits();
    }
}
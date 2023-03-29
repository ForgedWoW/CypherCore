// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Loot;

internal class LootRollWon : ServerPacket
{
    public ObjectGuid LootObj;
    public ObjectGuid Winner;
    public int Roll;
    public RollVote RollType;
    public LootItemData Item = new();
    public bool MainSpec;
    public LootRollWon() : base(ServerOpcodes.LootRollWon) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(LootObj);
        _worldPacket.WritePackedGuid(Winner);
        _worldPacket.WriteInt32(Roll);
        _worldPacket.WriteUInt8((byte)RollType);
        Item.Write(_worldPacket);
        _worldPacket.WriteBit(MainSpec);
        _worldPacket.FlushBits();
    }
}
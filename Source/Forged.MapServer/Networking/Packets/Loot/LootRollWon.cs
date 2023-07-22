// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Loot;

internal class LootRollWon : ServerPacket
{
    public LootItemData Item = new();
    public ObjectGuid LootObj;
    public bool MainSpec;
    public int Roll;
    public RollVote RollType;
    public ObjectGuid Winner;
    public uint DungeonEncounterID;

    public LootRollWon() : base(ServerOpcodes.LootRollWon) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(LootObj);
        WorldPacket.WritePackedGuid(Winner);
        WorldPacket.WriteInt32(Roll);
        WorldPacket.WriteUInt8((byte)RollType);
        WorldPacket.WriteUInt32(DungeonEncounterID);
        Item.Write(WorldPacket);
        WorldPacket.WriteBit(MainSpec);
        WorldPacket.FlushBits();
    }
}
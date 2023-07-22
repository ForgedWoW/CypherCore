// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Loot;

internal class LootRollsComplete : ServerPacket
{
    public byte LootListID;
    public ObjectGuid LootObj;
    public int DungeonEncounterID;

    public LootRollsComplete() : base(ServerOpcodes.LootRollsComplete) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(LootObj);
        WorldPacket.WriteUInt8(LootListID);
        WorldPacket.WriteInt32(DungeonEncounterID);
    }
}
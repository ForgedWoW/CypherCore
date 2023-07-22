// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Loot;

internal class LootAllPassed : ServerPacket
{
    public LootItemData Item = new();
    public ObjectGuid LootObj;
    public uint DungeonEncounterID;

    public LootAllPassed() : base(ServerOpcodes.LootAllPassed) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(LootObj);
        WorldPacket.WriteUInt32(DungeonEncounterID);
        Item.Write(WorldPacket);
    }
}
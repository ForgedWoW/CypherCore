// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Party;

internal class SetLootMethod : ClientPacket
{
    public ObjectGuid LootMasterGUID;
    public LootMethod LootMethod;
    public ItemQuality LootThreshold;
    public sbyte PartyIndex;
    public SetLootMethod(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        PartyIndex = WorldPacket.ReadInt8();
        LootMethod = (LootMethod)WorldPacket.ReadUInt8();
        LootMasterGUID = WorldPacket.ReadPackedGuid();
        LootThreshold = (ItemQuality)WorldPacket.ReadUInt32();
    }
}
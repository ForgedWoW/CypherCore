// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Addon;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.AuctionHouse;

internal class AuctionBrowseQuery : ClientPacket
{
    public ObjectGuid Auctioneer;
    public AuctionHouseFilterMask Filters;
    public Array<AuctionListFilterClass> ItemClassFilters = new(7);
    public byte[] KnownPets;
    public byte MaxLevel = SharedConst.MaxLevel;
    public sbyte MaxPetLevel;
    public byte MinLevel = 1;
    public string Name;
    public uint Offset;
    public Array<AuctionSortDef> Sorts = new(2);
    public AddOnInfo? TaintedBy;
    public AuctionBrowseQuery(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Auctioneer = WorldPacket.ReadPackedGuid();
        Offset = WorldPacket.ReadUInt32();
        MinLevel = WorldPacket.ReadUInt8();
        MaxLevel = WorldPacket.ReadUInt8();
        Filters = (AuctionHouseFilterMask)WorldPacket.ReadUInt32();
        var knownPetSize = WorldPacket.ReadUInt32();
        MaxPetLevel = WorldPacket.ReadInt8();

        var sizeLimit = CliDB.BattlePetSpeciesStorage.GetNumRows() / 8 + 1;

        if (knownPetSize >= sizeLimit)
            throw new global::System.Exception($"Attempted to read more array elements from packet {knownPetSize} than allowed {sizeLimit}");

        KnownPets = new byte[knownPetSize];

        for (var i = 0; i < knownPetSize; ++i)
            KnownPets[i] = WorldPacket.ReadUInt8();

        if (WorldPacket.HasBit())
            TaintedBy = new AddOnInfo();

        var nameLength = WorldPacket.ReadBits<uint>(8);
        var itemClassFilterCount = WorldPacket.ReadBits<uint>(3);
        var sortSize = WorldPacket.ReadBits<uint>(2);

        for (var i = 0; i < sortSize; ++i)
            Sorts[i] = new AuctionSortDef(WorldPacket);

        TaintedBy?.Read(WorldPacket);

        Name = WorldPacket.ReadString(nameLength);

        for (var i = 0; i < itemClassFilterCount; ++i) // AuctionListFilterClass filterClass in ItemClassFilters)
            ItemClassFilters[i] = new AuctionListFilterClass(WorldPacket);
    }
}

//Structs
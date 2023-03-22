// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;

namespace Game.Networking.Packets;

public class AuctionBrowseQuery : ClientPacket
{
	public ObjectGuid Auctioneer;
	public uint Offset;
	public byte MinLevel = 1;
	public byte MaxLevel = SharedConst.MaxLevel;
	public AuctionHouseFilterMask Filters;
	public byte[] KnownPets;
	public sbyte MaxPetLevel;
	public AddOnInfo? TaintedBy;
	public string Name;
	public Array<AuctionListFilterClass> ItemClassFilters = new(7);
	public Array<AuctionSortDef> Sorts = new(2);

	public AuctionBrowseQuery(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Auctioneer = _worldPacket.ReadPackedGuid();
		Offset = _worldPacket.ReadUInt32();
		MinLevel = _worldPacket.ReadUInt8();
		MaxLevel = _worldPacket.ReadUInt8();
		Filters = (AuctionHouseFilterMask)_worldPacket.ReadUInt32();
		var knownPetSize = _worldPacket.ReadUInt32();
		MaxPetLevel = _worldPacket.ReadInt8();

		var sizeLimit = CliDB.BattlePetSpeciesStorage.GetNumRows() / 8 + 1;

		if (knownPetSize >= sizeLimit)
			throw new System.Exception($"Attempted to read more array elements from packet {knownPetSize} than allowed {sizeLimit}");

		KnownPets = new byte[knownPetSize];

		for (var i = 0; i < knownPetSize; ++i)
			KnownPets[i] = _worldPacket.ReadUInt8();

		if (_worldPacket.HasBit())
			TaintedBy = new AddOnInfo();

		var nameLength = _worldPacket.ReadBits<uint>(8);
		var itemClassFilterCount = _worldPacket.ReadBits<uint>(3);
		var sortSize = _worldPacket.ReadBits<uint>(2);

		for (var i = 0; i < sortSize; ++i)
			Sorts[i] = new AuctionSortDef(_worldPacket);

		if (TaintedBy.HasValue)
			TaintedBy.Value.Read(_worldPacket);

		Name = _worldPacket.ReadString(nameLength);

		for (var i = 0; i < itemClassFilterCount; ++i) // AuctionListFilterClass filterClass in ItemClassFilters)
			ItemClassFilters[i] = new AuctionListFilterClass(_worldPacket);
	}
}

//Structs
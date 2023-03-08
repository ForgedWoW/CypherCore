// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class LootUnit : ClientPacket
{
	public ObjectGuid Unit;
	public bool IsSoftInteract;
	public LootUnit(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Unit = _worldPacket.ReadPackedGuid();
		IsSoftInteract = _worldPacket.HasBit();
	}
}

public class LootResponse : ServerPacket
{
	public ObjectGuid LootObj;
	public ObjectGuid Owner;
	public byte Threshold = 2; // Most common value, 2 = Uncommon
	public LootMethod LootMethod;
	public byte AcquireReason;
	public LootError FailureReason = LootError.NoLoot; // Most common value
	public uint Coins;
	public List<LootItemData> Items = new();
	public List<LootCurrency> Currencies = new();
	public bool Acquired;
	public bool AELooting;
	public LootResponse() : base(ServerOpcodes.LootResponse, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Owner);
		_worldPacket.WritePackedGuid(LootObj);
		_worldPacket.WriteUInt8((byte)FailureReason);
		_worldPacket.WriteUInt8(AcquireReason);
		_worldPacket.WriteUInt8((byte)LootMethod);
		_worldPacket.WriteUInt8(Threshold);
		_worldPacket.WriteUInt32(Coins);
		_worldPacket.WriteInt32(Items.Count);
		_worldPacket.WriteInt32(Currencies.Count);
		_worldPacket.WriteBit(Acquired);
		_worldPacket.WriteBit(AELooting);
		_worldPacket.FlushBits();

		foreach (var item in Items)
			item.Write(_worldPacket);

		foreach (var currency in Currencies)
		{
			_worldPacket.WriteUInt32(currency.CurrencyID);
			_worldPacket.WriteUInt32(currency.Quantity);
			_worldPacket.WriteUInt8(currency.LootListID);
			_worldPacket.WriteBits(currency.UIType, 3);
			_worldPacket.FlushBits();
		}
	}
}

class LootItemPkt : ClientPacket
{
	public List<LootRequest> Loot = new();
	public LootItemPkt(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var Count = _worldPacket.ReadUInt32();

		for (uint i = 0; i < Count; ++i)
		{
			var loot = new LootRequest()
			{
				Object = _worldPacket.ReadPackedGuid(),
				LootListID = _worldPacket.ReadUInt8()
			};

			Loot.Add(loot);
		}
	}
}

class MasterLootItem : ClientPacket
{
	public Array<LootRequest> Loot = new(1000);
	public ObjectGuid Target;
	public MasterLootItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var Count = _worldPacket.ReadUInt32();
		Target = _worldPacket.ReadPackedGuid();

		for (var i = 0; i < Count; ++i)
		{
			LootRequest lootRequest = new();
			lootRequest.Object = _worldPacket.ReadPackedGuid();
			lootRequest.LootListID = _worldPacket.ReadUInt8();
			Loot[i] = lootRequest;
		}
	}
}

class LootRemoved : ServerPacket
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

class LootRelease : ClientPacket
{
	public ObjectGuid Unit;
	public LootRelease(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Unit = _worldPacket.ReadPackedGuid();
	}
}

class LootMoney : ClientPacket
{
	public LootMoney(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

class LootMoneyNotify : ServerPacket
{
	public ulong Money;
	public ulong MoneyMod;
	public bool SoleLooter;
	public LootMoneyNotify() : base(ServerOpcodes.LootMoneyNotify) { }

	public override void Write()
	{
		_worldPacket.WriteUInt64(Money);
		_worldPacket.WriteUInt64(MoneyMod);
		_worldPacket.WriteBit(SoleLooter);
		_worldPacket.FlushBits();
	}
}

class CoinRemoved : ServerPacket
{
	public ObjectGuid LootObj;
	public CoinRemoved() : base(ServerOpcodes.CoinRemoved) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(LootObj);
	}
}

class LootRollPacket : ClientPacket
{
	public ObjectGuid LootObj;
	public byte LootListID;
	public RollVote RollType;
	public LootRollPacket(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		LootObj = _worldPacket.ReadPackedGuid();
		LootListID = _worldPacket.ReadUInt8();
		RollType = (RollVote)_worldPacket.ReadUInt8();
	}
}

class LootReleaseResponse : ServerPacket
{
	public ObjectGuid LootObj;
	public ObjectGuid Owner;
	public LootReleaseResponse() : base(ServerOpcodes.LootRelease) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(LootObj);
		_worldPacket.WritePackedGuid(Owner);
	}
}

class LootReleaseAll : ServerPacket
{
	public LootReleaseAll() : base(ServerOpcodes.LootReleaseAll, ConnectionType.Instance) { }

	public override void Write() { }
}

class LootList : ServerPacket
{
	public ObjectGuid Owner;
	public ObjectGuid LootObj;
	public ObjectGuid? Master;
	public ObjectGuid? RoundRobinWinner;
	public LootList() : base(ServerOpcodes.LootList, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Owner);
		_worldPacket.WritePackedGuid(LootObj);

		_worldPacket.WriteBit(Master.HasValue);
		_worldPacket.WriteBit(RoundRobinWinner.HasValue);
		_worldPacket.FlushBits();

		if (Master.HasValue)
			_worldPacket.WritePackedGuid(Master.Value);

		if (RoundRobinWinner.HasValue)
			_worldPacket.WritePackedGuid(RoundRobinWinner.Value);
	}
}

class SetLootSpecialization : ClientPacket
{
	public uint SpecID;
	public SetLootSpecialization(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		SpecID = _worldPacket.ReadUInt32();
	}
}

class StartLootRoll : ServerPacket
{
	public ObjectGuid LootObj;
	public int MapID;
	public uint RollTime;
	public LootMethod Method;
	public RollMask ValidRolls;
	public Array<LootRollIneligibilityReason> LootRollIneligibleReason = new(4);
	public LootItemData Item = new();
	public StartLootRoll() : base(ServerOpcodes.StartLootRoll) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(LootObj);
		_worldPacket.WriteInt32(MapID);
		_worldPacket.WriteUInt32(RollTime);
		_worldPacket.WriteUInt8((byte)ValidRolls);

		foreach (var reason in LootRollIneligibleReason)
			_worldPacket.WriteUInt32((uint)reason);

		_worldPacket.WriteUInt8((byte)Method);
		Item.Write(_worldPacket);
	}
}

class LootRollBroadcast : ServerPacket
{
	public ObjectGuid LootObj;
	public ObjectGuid Player;
	public int Roll; // Roll value can be negative, it means that it is an "offspec" roll but only during roll selection broadcast (not when sending the result)
	public RollVote RollType;
	public LootItemData Item = new();
	public bool Autopassed; // Triggers message |HlootHistory:%d|h[Loot]|h: You automatically passed on: %s because you cannot loot that item.
	public LootRollBroadcast() : base(ServerOpcodes.LootRoll) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(LootObj);
		_worldPacket.WritePackedGuid(Player);
		_worldPacket.WriteInt32(Roll);
		_worldPacket.WriteUInt8((byte)RollType);
		Item.Write(_worldPacket);
		_worldPacket.WriteBit(Autopassed);
		_worldPacket.FlushBits();
	}
}

class LootRollWon : ServerPacket
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

class LootAllPassed : ServerPacket
{
	public ObjectGuid LootObj;
	public LootItemData Item = new();
	public LootAllPassed() : base(ServerOpcodes.LootAllPassed) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(LootObj);
		Item.Write(_worldPacket);
	}
}

class LootRollsComplete : ServerPacket
{
	public ObjectGuid LootObj;
	public byte LootListID;
	public LootRollsComplete() : base(ServerOpcodes.LootRollsComplete) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(LootObj);
		_worldPacket.WriteUInt8(LootListID);
	}
}

class MasterLootCandidateList : ServerPacket
{
	public List<ObjectGuid> Players = new();
	public ObjectGuid LootObj;
	public MasterLootCandidateList() : base(ServerOpcodes.MasterLootCandidateList, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(LootObj);
		_worldPacket.WriteInt32(Players.Count);
		Players.ForEach(guid => _worldPacket.WritePackedGuid(guid));
	}
}

class AELootTargets : ServerPacket
{
	readonly uint Count;

	public AELootTargets(uint count) : base(ServerOpcodes.AeLootTargets, ConnectionType.Instance)
	{
		Count = count;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32(Count);
	}
}

class AELootTargetsAck : ServerPacket
{
	public AELootTargetsAck() : base(ServerOpcodes.AeLootTargetAck, ConnectionType.Instance) { }

	public override void Write() { }
}

//Structs
public class LootItemData
{
	public byte Type;
	public LootSlotType UIType;
	public uint Quantity;
	public byte LootItemType;
	public byte LootListID;
	public bool CanTradeToTapList;
	public ItemInstance Loot;

	public void Write(WorldPacket data)
	{
		data.WriteBits(Type, 2);
		data.WriteBits(UIType, 3);
		data.WriteBit(CanTradeToTapList);
		data.FlushBits();
		Loot.Write(data); // WorldPackets::Item::ItemInstance
		data.WriteUInt32(Quantity);
		data.WriteUInt8(LootItemType);
		data.WriteUInt8(LootListID);
	}
}

public struct LootCurrency
{
	public uint CurrencyID;
	public uint Quantity;
	public byte LootListID;
	public byte UIType;
}

public struct LootRequest
{
	public ObjectGuid Object;
	public byte LootListID;
}
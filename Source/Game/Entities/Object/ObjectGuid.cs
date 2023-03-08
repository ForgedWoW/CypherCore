// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Game.Entities;

public struct ObjectGuid : IEquatable<ObjectGuid>
{
	public static ObjectGuid Empty = new();
	public static ObjectGuid FromStringFailed = Create(HighGuid.Uniq, 4);
	public static ObjectGuid TradeItem = Create(HighGuid.Uniq, 10);

	ulong _low;
	ulong _high;

	public ObjectGuid(ulong high, ulong low)
	{
		_low = low;
		_high = high;
	}

	public static ObjectGuid Create(HighGuid type, ulong dbId)
	{
		switch (type)
		{
			case HighGuid.Null:
				return ObjectGuidFactory.CreateNull();
			case HighGuid.Uniq:
				return ObjectGuidFactory.CreateUniq(dbId);
			case HighGuid.Player:
				return ObjectGuidFactory.CreatePlayer(0, dbId);
			case HighGuid.Item:
				return ObjectGuidFactory.CreateItem(0, dbId);
			case HighGuid.StaticDoor:
			case HighGuid.Transport:
				return ObjectGuidFactory.CreateTransport(type, dbId);
			case HighGuid.Party:
			case HighGuid.WowAccount:
			case HighGuid.BNetAccount:
			case HighGuid.GMTask:
			case HighGuid.RaidGroup:
			case HighGuid.Spell:
			case HighGuid.Mail:
			case HighGuid.UserRouter:
			case HighGuid.PVPQueueGroup:
			case HighGuid.UserClient:
			case HighGuid.BattlePet:
			case HighGuid.CommerceObj:
				return ObjectGuidFactory.CreateGlobal(type, 0, dbId);
			case HighGuid.Guild:
				return ObjectGuidFactory.CreateGuild(type, 0, dbId);
			default:
				return Empty;
		}
	}

	public static ObjectGuid Create(HighGuid type, ushort ownerType, ushort ownerId, uint counter)
	{
		if (type != HighGuid.ClientActor)
			return Empty;

		return ObjectGuidFactory.CreateClientActor(ownerType, ownerId, counter);
	}

	public static ObjectGuid Create(HighGuid type, bool builtIn, bool trade, ushort zoneId, byte factionGroupMask, ulong counter)
	{
		if (type != HighGuid.ChatChannel)
			return Empty;

		return ObjectGuidFactory.CreateChatChannel(0, builtIn, trade, zoneId, factionGroupMask, counter);
	}

	public static ObjectGuid Create(HighGuid type, ushort arg1, ulong counter)
	{
		if (type != HighGuid.MobileSession)
			return Empty;

		return ObjectGuidFactory.CreateMobileSession(0, arg1, counter);
	}

	public static ObjectGuid Create(HighGuid type, byte arg1, byte arg2, ulong counter)
	{
		if (type != HighGuid.WebObj)
			return Empty;

		return ObjectGuidFactory.CreateWebObj(0, arg1, arg2, counter);
	}

	public static ObjectGuid Create(HighGuid type, byte arg1, byte arg2, byte arg3, byte arg4, bool arg5, byte arg6, ulong counter)
	{
		if (type != HighGuid.LFGObject)
			return Empty;

		return ObjectGuidFactory.CreateLFGObject(arg1, arg2, arg3, arg4, arg5, arg6, counter);
	}

	public static ObjectGuid Create(HighGuid type, byte arg1, ulong counter)
	{
		if (type != HighGuid.LFGList)
			return Empty;

		return ObjectGuidFactory.CreateLFGList(arg1, counter);
	}

	public static ObjectGuid Create(HighGuid type, uint arg1, ulong counter)
	{
		switch (type)
		{
			case HighGuid.PetBattle:
			case HighGuid.UniqUserClient:
			case HighGuid.ClientSession:
			case HighGuid.ClientConnection:
				return ObjectGuidFactory.CreateClient(type, 0, arg1, counter);
			default:
				return Empty;
		}
	}

	public static ObjectGuid Create(HighGuid type, byte clubType, uint clubFinderId, ulong counter)
	{
		if (type != HighGuid.ClubFinder)
			return Empty;

		return ObjectGuidFactory.CreateClubFinder(0, clubType, clubFinderId, counter);
	}

	public static ObjectGuid Create(HighGuid type, uint mapId, uint entry, ulong counter)
	{
		switch (type)
		{
			case HighGuid.WorldTransaction:
			case HighGuid.Conversation:
			case HighGuid.Creature:
			case HighGuid.Vehicle:
			case HighGuid.Pet:
			case HighGuid.GameObject:
			case HighGuid.DynamicObject:
			case HighGuid.AreaTrigger:
			case HighGuid.Corpse:
			case HighGuid.LootObject:
			case HighGuid.SceneObject:
			case HighGuid.Scenario:
			case HighGuid.AIGroup:
			case HighGuid.DynamicDoor:
			case HighGuid.Vignette:
			case HighGuid.CallForHelp:
			case HighGuid.AIResource:
			case HighGuid.AILock:
			case HighGuid.AILockTicket:
				return ObjectGuidFactory.CreateWorldObject(type, 0, 0, (ushort)mapId, 0, entry, counter);
			default:
				return Empty;
		}
	}

	public static ObjectGuid Create(HighGuid type, SpellCastSource subType, uint mapId, uint entry, ulong counter)
	{
		switch (type)
		{
			case HighGuid.Cast:
				return ObjectGuidFactory.CreateWorldObject(type, (byte)subType, 0, (ushort)mapId, 0, entry, counter);
			default:
				return Empty;
		}
	}

	public byte[] GetRawValue()
	{
		var temp = new byte[16];
		var hiBytes = BitConverter.GetBytes(_high);
		var lowBytes = BitConverter.GetBytes(_low);

		for (var i = 0; i < temp.Length / 2; ++i)
		{
			temp[i] = lowBytes[i];
			temp[8 + i] = hiBytes[i];
		}

		return temp;
	}

	public void SetRawValue(byte[] bytes)
	{
		_low = BitConverter.ToUInt64(bytes, 0);
		_high = BitConverter.ToUInt64(bytes, 8);
	}

	public void SetRawValue(ulong high, ulong low)
	{
		_high = high;
		_low = low;
	}

	public void Clear()
	{
		_high = 0;
		_low = 0;
	}

	public ulong GetHighValue()
	{
		return _high;
	}

	public ulong GetLowValue()
	{
		return _low;
	}

	public HighGuid GetHigh()
	{
		return (HighGuid)(_high >> 58);
	}

	public byte GetSubType()
	{
		return (byte)(_high & 0x3F);
	}

	public uint GetRealmId()
	{
		return (uint)((_high >> 42) & 0x1FFF);
	}

	public uint GetServerId()
	{
		return (uint)((_low >> 40) & 0x1FFF);
	}

	public uint GetMapId()
	{
		return (uint)((_high >> 29) & 0x1FFF);
	}

	public uint GetEntry()
	{
		return (uint)((_high >> 6) & 0x7FFFFF);
	}

	public ulong GetCounter()
	{
		if (GetHigh() == HighGuid.Transport)
			return (_high >> 38) & 0xFFFFF;
		else
			return _low & 0xFFFFFFFFFF;
	}

	public static ulong GetMaxCounter(HighGuid highGuid)
	{
		if (highGuid == HighGuid.Transport)
			return 0xFFFFF;
		else
			return 0xFFFFFFFFFF;
	}

	public bool IsEmpty()
	{
		return _low == 0 && _high == 0;
	}

	public bool IsCreature()
	{
		return GetHigh() == HighGuid.Creature;
	}

	public bool IsPet()
	{
		return GetHigh() == HighGuid.Pet;
	}

	public bool IsVehicle()
	{
		return GetHigh() == HighGuid.Vehicle;
	}

	public bool IsCreatureOrPet()
	{
		return IsCreature() || IsPet();
	}

	public bool IsCreatureOrVehicle()
	{
		return IsCreature() || IsVehicle();
	}

	public bool IsAnyTypeCreature()
	{
		return IsCreature() || IsPet() || IsVehicle();
	}

	public bool IsPlayer()
	{
		return !IsEmpty() && GetHigh() == HighGuid.Player;
	}

	public bool IsUnit()
	{
		return IsAnyTypeCreature() || IsPlayer();
	}

	public bool IsItem()
	{
		return GetHigh() == HighGuid.Item;
	}

	public bool IsGameObject()
	{
		return GetHigh() == HighGuid.GameObject;
	}

	public bool IsDynamicObject()
	{
		return GetHigh() == HighGuid.DynamicObject;
	}

	public bool IsCorpse()
	{
		return GetHigh() == HighGuid.Corpse;
	}

	public bool IsAreaTrigger()
	{
		return GetHigh() == HighGuid.AreaTrigger;
	}

	public bool IsMOTransport()
	{
		return GetHigh() == HighGuid.Transport;
	}

	public bool IsAnyTypeGameObject()
	{
		return IsGameObject() || IsMOTransport();
	}

	public bool IsParty()
	{
		return GetHigh() == HighGuid.Party;
	}

	public bool IsGuild()
	{
		return GetHigh() == HighGuid.Guild;
	}

	public bool IsSceneObject()
	{
		return GetHigh() == HighGuid.SceneObject;
	}

	public bool IsConversation()
	{
		return GetHigh() == HighGuid.Conversation;
	}

	public bool IsCast()
	{
		return GetHigh() == HighGuid.Cast;
	}

	public TypeId GetTypeId()
	{
		return GetTypeId(GetHigh());
	}

	bool HasEntry()
	{
		return HasEntry(GetHigh());
	}

	public static bool operator <(ObjectGuid left, ObjectGuid right)
	{
		if (left._high < right._high)
			return true;
		else if (left._high > right._high)
			return false;

		return left._low < right._low;
	}

	public static bool operator >(ObjectGuid left, ObjectGuid right)
	{
		if (left._high > right._high)
			return true;
		else if (left._high < right._high)
			return false;

		return left._low > right._low;
	}

	public override string ToString()
	{
		var str = $"GUID Full: 0x{_high + _low}, Type: {GetHigh()}";

		if (HasEntry())
			str += (IsPet() ? " Pet number: " : " Entry: ") + GetEntry() + " ";

		str += " Low: " + GetCounter();

		return str;
	}

	public static ObjectGuid FromString(string guidString)
	{
		return ObjectGuidInfo.Parse(guidString);
	}

	public static bool operator ==(ObjectGuid first, ObjectGuid other)
	{
		return first.Equals(other);
	}

	public static bool operator !=(ObjectGuid first, ObjectGuid other)
	{
		return !(first == other);
	}

	public override bool Equals(object obj)
	{
		return obj != null && obj is ObjectGuid && Equals((ObjectGuid)obj);
	}

	public bool Equals(ObjectGuid other)
	{
		return other._high == _high && other._low == _low;
	}

	public override int GetHashCode()
	{
		return new
		{
			_high,
			_low
		}.GetHashCode();
	}

	//Static Methods 
	static TypeId GetTypeId(HighGuid high)
	{
		switch (high)
		{
			case HighGuid.Item:
				return TypeId.Item;
			case HighGuid.Creature:
			case HighGuid.Pet:
			case HighGuid.Vehicle:
				return TypeId.Unit;
			case HighGuid.Player:
				return TypeId.Player;
			case HighGuid.GameObject:
			case HighGuid.Transport:
				return TypeId.GameObject;
			case HighGuid.DynamicObject:
				return TypeId.DynamicObject;
			case HighGuid.Corpse:
				return TypeId.Corpse;
			case HighGuid.AreaTrigger:
				return TypeId.AreaTrigger;
			case HighGuid.SceneObject:
				return TypeId.SceneObject;
			case HighGuid.Conversation:
				return TypeId.Conversation;
			default:
				return TypeId.Object;
		}
	}

	static bool HasEntry(HighGuid high)
	{
		switch (high)
		{
			case HighGuid.GameObject:
			case HighGuid.Creature:
			case HighGuid.Pet:
			case HighGuid.Vehicle:
			default:
				return true;
		}
	}

	public static bool IsMapSpecific(HighGuid high)
	{
		switch (high)
		{
			case HighGuid.Conversation:
			case HighGuid.Creature:
			case HighGuid.Vehicle:
			case HighGuid.Pet:
			case HighGuid.GameObject:
			case HighGuid.DynamicObject:
			case HighGuid.AreaTrigger:
			case HighGuid.Corpse:
			case HighGuid.LootObject:
			case HighGuid.SceneObject:
			case HighGuid.Scenario:
			case HighGuid.AIGroup:
			case HighGuid.DynamicDoor:
			case HighGuid.Vignette:
			case HighGuid.CallForHelp:
			case HighGuid.AIResource:
			case HighGuid.AILock:
			case HighGuid.AILockTicket:
				return true;
			default:
				return false;
		}
	}

	public static bool IsRealmSpecific(HighGuid high)
	{
		switch (high)
		{
			case HighGuid.Player:
			case HighGuid.Item:
			case HighGuid.ChatChannel:
			case HighGuid.Transport:
			case HighGuid.Guild:
				return true;
			default:
				return false;
		}
	}

	public static bool IsGlobal(HighGuid high)
	{
		switch (high)
		{
			case HighGuid.Uniq:
			case HighGuid.Party:
			case HighGuid.WowAccount:
			case HighGuid.BNetAccount:
			case HighGuid.GMTask:
			case HighGuid.RaidGroup:
			case HighGuid.Spell:
			case HighGuid.Mail:
			case HighGuid.UserRouter:
			case HighGuid.PVPQueueGroup:
			case HighGuid.UserClient:
			case HighGuid.UniqUserClient:
			case HighGuid.BattlePet:
				return true;
			default:
				return false;
		}
	}
}
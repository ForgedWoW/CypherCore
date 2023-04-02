// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Entities.Objects;

public struct ObjectGuid : IEquatable<ObjectGuid>
{
    public static ObjectGuid Empty = new();
    public static ObjectGuid FromStringFailed = Create(HighGuid.Uniq, 4);
    public static ObjectGuid TradeItem = Create(HighGuid.Uniq, 10);

    public ObjectGuid(ulong high, ulong low)
    {
        LowValue = low;
        HighValue = high;
    }

    public ulong Counter
    {
        get
        {
            if (High == HighGuid.Transport)
                return (HighValue >> 38) & 0xFFFFF;
            else
                return LowValue & 0xFFFFFFFFFF;
        }
    }

    public uint Entry => (uint)((HighValue >> 6) & 0x7FFFFF);

    public HighGuid High => (HighGuid)(HighValue >> 58);

    public ulong HighValue { get; private set; }

    public bool IsAnyTypeCreature => IsCreature || IsPet || IsVehicle;

    public bool IsAnyTypeGameObject => IsGameObject || IsMOTransport;

    public bool IsAreaTrigger => High == HighGuid.AreaTrigger;

    public bool IsCast => High == HighGuid.Cast;

    public bool IsConversation => High == HighGuid.Conversation;

    public bool IsCorpse => High == HighGuid.Corpse;

    public bool IsCreature => High == HighGuid.Creature;

    public bool IsCreatureOrPet => IsCreature || IsPet;

    public bool IsCreatureOrVehicle => IsCreature || IsVehicle;

    public bool IsDynamicObject => High == HighGuid.DynamicObject;

    public bool IsEmpty => LowValue == 0 && HighValue == 0;

    public bool IsGameObject => High == HighGuid.GameObject;

    public bool IsGuild => High == HighGuid.Guild;

    public bool IsItem => High == HighGuid.Item;

    public bool IsMOTransport => High == HighGuid.Transport;

    public bool IsParty => High == HighGuid.Party;

    public bool IsPet => High == HighGuid.Pet;

    public bool IsPlayer => !IsEmpty && High == HighGuid.Player;

    public bool IsSceneObject => High == HighGuid.SceneObject;

    public bool IsUnit => IsAnyTypeCreature || IsPlayer;

    public bool IsVehicle => High == HighGuid.Vehicle;

    public ulong LowValue { get; private set; }

    public uint MapId => (uint)((HighValue >> 29) & 0x1FFF);

    public uint RealmId => (uint)((HighValue >> 42) & 0x1FFF);

    public uint ServerId => (uint)((LowValue >> 40) & 0x1FFF);

    public byte SubType => (byte)(HighValue & 0x3F);

    public TypeId TypeId => GetTypeId(High);

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

    public static ObjectGuid FromString(string guidString)
    {
        return ObjectGuidInfo.Parse(guidString);
    }

    public static ulong GetMaxCounter(HighGuid highGuid)
    {
        return highGuid == HighGuid.Transport ? 0xFFFFF : (ulong)0xFFFFFFFFFF;
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

    public static bool operator !=(ObjectGuid first, ObjectGuid other)
    {
        return !(first == other);
    }

    public static bool operator <(ObjectGuid left, ObjectGuid right)
    {
        if (left.HighValue < right.HighValue)
            return true;
        else if (left.HighValue > right.HighValue)
            return false;

        return left.LowValue < right.LowValue;
    }

    public static bool operator ==(ObjectGuid first, ObjectGuid other)
    {
        return first.Equals(other);
    }

    public static bool operator >(ObjectGuid left, ObjectGuid right)
    {
        if (left.HighValue > right.HighValue)
            return true;
        else if (left.HighValue < right.HighValue)
            return false;

        return left.LowValue > right.LowValue;
    }

    public void Clear()
    {
        HighValue = 0;
        LowValue = 0;
    }

    public override bool Equals(object obj)
    {
        return obj is ObjectGuid guid && Equals(guid);
    }

    public bool Equals(ObjectGuid other)
    {
        return other.HighValue == HighValue && other.LowValue == LowValue;
    }

    public override int GetHashCode()
    {
        return new
        {
            HighValue,
            LowValue
        }.GetHashCode();
    }

    public byte[] GetRawValue()
    {
        var temp = new byte[16];
        var hiBytes = BitConverter.GetBytes(HighValue);
        var lowBytes = BitConverter.GetBytes(LowValue);

        for (var i = 0; i < temp.Length / 2; ++i)
        {
            temp[i] = lowBytes[i];
            temp[8 + i] = hiBytes[i];
        }

        return temp;
    }

    public void SetRawValue(byte[] bytes)
    {
        LowValue = BitConverter.ToUInt64(bytes, 0);
        HighValue = BitConverter.ToUInt64(bytes, 8);
    }

    public void SetRawValue(ulong high, ulong low)
    {
        HighValue = high;
        LowValue = low;
    }
    public override string ToString()
    {
        var str = $"GUID Full: 0x{HighValue + LowValue}, Type: {High}";

        if (HasEntry())
            str += (IsPet ? " Pet number: " : " Entry: ") + Entry + " ";

        str += " Low: " + Counter;

        return str;
    }

    //Static Methods 
    private static TypeId GetTypeId(HighGuid high)
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

    private static bool HasEntry(HighGuid high)
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

    private bool HasEntry()
    {
        return HasEntry(High);
    }
}
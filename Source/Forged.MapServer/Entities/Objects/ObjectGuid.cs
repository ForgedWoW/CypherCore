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
        return type switch
        {
            HighGuid.Null          => ObjectGuidFactory.CreateNull(),
            HighGuid.Uniq          => ObjectGuidFactory.CreateUniq(dbId),
            HighGuid.Player        => ObjectGuidFactory.CreatePlayer(0, dbId),
            HighGuid.Item          => ObjectGuidFactory.CreateItem(0, dbId),
            HighGuid.StaticDoor    => ObjectGuidFactory.CreateTransport(type, dbId),
            HighGuid.Transport     => ObjectGuidFactory.CreateTransport(type, dbId),
            HighGuid.Party         => ObjectGuidFactory.CreateGlobal(type, 0, dbId),
            HighGuid.WowAccount    => ObjectGuidFactory.CreateGlobal(type, 0, dbId),
            HighGuid.BNetAccount   => ObjectGuidFactory.CreateGlobal(type, 0, dbId),
            HighGuid.GMTask        => ObjectGuidFactory.CreateGlobal(type, 0, dbId),
            HighGuid.RaidGroup     => ObjectGuidFactory.CreateGlobal(type, 0, dbId),
            HighGuid.Spell         => ObjectGuidFactory.CreateGlobal(type, 0, dbId),
            HighGuid.Mail          => ObjectGuidFactory.CreateGlobal(type, 0, dbId),
            HighGuid.UserRouter    => ObjectGuidFactory.CreateGlobal(type, 0, dbId),
            HighGuid.PVPQueueGroup => ObjectGuidFactory.CreateGlobal(type, 0, dbId),
            HighGuid.UserClient    => ObjectGuidFactory.CreateGlobal(type, 0, dbId),
            HighGuid.BattlePet     => ObjectGuidFactory.CreateGlobal(type, 0, dbId),
            HighGuid.CommerceObj   => ObjectGuidFactory.CreateGlobal(type, 0, dbId),
            HighGuid.Guild         => ObjectGuidFactory.CreateGuild(type, 0, dbId),
            _                      => Empty
        };
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
        return type switch
        {
            HighGuid.PetBattle        => ObjectGuidFactory.CreateClient(type, 0, arg1, counter),
            HighGuid.UniqUserClient   => ObjectGuidFactory.CreateClient(type, 0, arg1, counter),
            HighGuid.ClientSession    => ObjectGuidFactory.CreateClient(type, 0, arg1, counter),
            HighGuid.ClientConnection => ObjectGuidFactory.CreateClient(type, 0, arg1, counter),
            _                         => Empty
        };
    }

    public static ObjectGuid Create(HighGuid type, byte clubType, uint clubFinderId, ulong counter)
    {
        if (type != HighGuid.ClubFinder)
            return Empty;

        return ObjectGuidFactory.CreateClubFinder(0, clubType, clubFinderId, counter);
    }

    public static ObjectGuid Create(HighGuid type, uint mapId, uint entry, ulong counter)
    {
        return type switch
        {
            HighGuid.WorldTransaction => ObjectGuidFactory.CreateWorldObject(type, 0, 0, (ushort)mapId, 0, entry, counter),
            HighGuid.Conversation     => ObjectGuidFactory.CreateWorldObject(type, 0, 0, (ushort)mapId, 0, entry, counter),
            HighGuid.Creature         => ObjectGuidFactory.CreateWorldObject(type, 0, 0, (ushort)mapId, 0, entry, counter),
            HighGuid.Vehicle          => ObjectGuidFactory.CreateWorldObject(type, 0, 0, (ushort)mapId, 0, entry, counter),
            HighGuid.Pet              => ObjectGuidFactory.CreateWorldObject(type, 0, 0, (ushort)mapId, 0, entry, counter),
            HighGuid.GameObject       => ObjectGuidFactory.CreateWorldObject(type, 0, 0, (ushort)mapId, 0, entry, counter),
            HighGuid.DynamicObject    => ObjectGuidFactory.CreateWorldObject(type, 0, 0, (ushort)mapId, 0, entry, counter),
            HighGuid.AreaTrigger      => ObjectGuidFactory.CreateWorldObject(type, 0, 0, (ushort)mapId, 0, entry, counter),
            HighGuid.Corpse           => ObjectGuidFactory.CreateWorldObject(type, 0, 0, (ushort)mapId, 0, entry, counter),
            HighGuid.LootObject       => ObjectGuidFactory.CreateWorldObject(type, 0, 0, (ushort)mapId, 0, entry, counter),
            HighGuid.SceneObject      => ObjectGuidFactory.CreateWorldObject(type, 0, 0, (ushort)mapId, 0, entry, counter),
            HighGuid.Scenario         => ObjectGuidFactory.CreateWorldObject(type, 0, 0, (ushort)mapId, 0, entry, counter),
            HighGuid.AIGroup          => ObjectGuidFactory.CreateWorldObject(type, 0, 0, (ushort)mapId, 0, entry, counter),
            HighGuid.DynamicDoor      => ObjectGuidFactory.CreateWorldObject(type, 0, 0, (ushort)mapId, 0, entry, counter),
            HighGuid.Vignette         => ObjectGuidFactory.CreateWorldObject(type, 0, 0, (ushort)mapId, 0, entry, counter),
            HighGuid.CallForHelp      => ObjectGuidFactory.CreateWorldObject(type, 0, 0, (ushort)mapId, 0, entry, counter),
            HighGuid.AIResource       => ObjectGuidFactory.CreateWorldObject(type, 0, 0, (ushort)mapId, 0, entry, counter),
            HighGuid.AILock           => ObjectGuidFactory.CreateWorldObject(type, 0, 0, (ushort)mapId, 0, entry, counter),
            HighGuid.AILockTicket     => ObjectGuidFactory.CreateWorldObject(type, 0, 0, (ushort)mapId, 0, entry, counter),
            _                         => Empty
        };
    }

    public static ObjectGuid Create(HighGuid type, SpellCastSource subType, uint mapId, uint entry, ulong counter)
    {
        return type switch
        {
            HighGuid.Cast => ObjectGuidFactory.CreateWorldObject(type, (byte)subType, 0, (ushort)mapId, 0, entry, counter),
            _             => Empty
        };
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
        return high switch
        {
            HighGuid.Uniq           => true,
            HighGuid.Party          => true,
            HighGuid.WowAccount     => true,
            HighGuid.BNetAccount    => true,
            HighGuid.GMTask         => true,
            HighGuid.RaidGroup      => true,
            HighGuid.Spell          => true,
            HighGuid.Mail           => true,
            HighGuid.UserRouter     => true,
            HighGuid.PVPQueueGroup  => true,
            HighGuid.UserClient     => true,
            HighGuid.UniqUserClient => true,
            HighGuid.BattlePet      => true,
            _                       => false
        };
    }

    public static bool IsMapSpecific(HighGuid high)
    {
        return high switch
        {
            HighGuid.Conversation  => true,
            HighGuid.Creature      => true,
            HighGuid.Vehicle       => true,
            HighGuid.Pet           => true,
            HighGuid.GameObject    => true,
            HighGuid.DynamicObject => true,
            HighGuid.AreaTrigger   => true,
            HighGuid.Corpse        => true,
            HighGuid.LootObject    => true,
            HighGuid.SceneObject   => true,
            HighGuid.Scenario      => true,
            HighGuid.AIGroup       => true,
            HighGuid.DynamicDoor   => true,
            HighGuid.Vignette      => true,
            HighGuid.CallForHelp   => true,
            HighGuid.AIResource    => true,
            HighGuid.AILock        => true,
            HighGuid.AILockTicket  => true,
            _                      => false
        };
    }

    public static bool IsRealmSpecific(HighGuid high)
    {
        return high switch
        {
            HighGuid.Player      => true,
            HighGuid.Item        => true,
            HighGuid.ChatChannel => true,
            HighGuid.Transport   => true,
            HighGuid.Guild       => true,
            _                    => false
        };
    }

    public static bool operator !=(ObjectGuid first, ObjectGuid other)
    {
        return !(first == other);
    }

    public static bool operator <(ObjectGuid left, ObjectGuid right)
    {
        if (left.HighValue < right.HighValue)
            return true;

        if (left.HighValue > right.HighValue)
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

        if (left.HighValue < right.HighValue)
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
        return high switch
        {
            HighGuid.Item          => TypeId.Item,
            HighGuid.Creature      => TypeId.Unit,
            HighGuid.Pet           => TypeId.Unit,
            HighGuid.Vehicle       => TypeId.Unit,
            HighGuid.Player        => TypeId.Player,
            HighGuid.GameObject    => TypeId.GameObject,
            HighGuid.Transport     => TypeId.GameObject,
            HighGuid.DynamicObject => TypeId.DynamicObject,
            HighGuid.Corpse        => TypeId.Corpse,
            HighGuid.AreaTrigger   => TypeId.AreaTrigger,
            HighGuid.SceneObject   => TypeId.SceneObject,
            HighGuid.Conversation  => TypeId.Conversation,
            _                      => TypeId.Object
        };
    }

    private static bool HasEntry(HighGuid high)
    {
        return high switch
        {
            HighGuid.GameObject => true,
            HighGuid.Creature   => true,
            HighGuid.Pet        => true,
            HighGuid.Vehicle    => true,
            _                   => true
        };
    }

    private bool HasEntry()
    {
        return HasEntry(High);
    }
}
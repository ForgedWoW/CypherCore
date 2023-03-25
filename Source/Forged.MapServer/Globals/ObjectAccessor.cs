// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps;
using Framework.Constants;
using Transport = Forged.MapServer.Entities.Transport;

namespace Forged.MapServer.Globals;

public class ObjectAccessor : Singleton<ObjectAccessor>
{
    readonly object _lockObject = new();
    readonly Dictionary<ObjectGuid, Player> _players = new();

    ObjectAccessor() { }

    public WorldObject GetWorldObject(WorldObject p, ObjectGuid guid)
    {
        switch (guid.High)
        {
            case HighGuid.Player:
                return GetPlayer(p, guid);
            case HighGuid.Transport:
            case HighGuid.GameObject:
                return GetGameObject(p, guid);
            case HighGuid.Vehicle:
            case HighGuid.Creature:
                return GetCreature(p, guid);
            case HighGuid.Pet:
                return GetPet(p, guid);
            case HighGuid.DynamicObject:
                return GetDynamicObject(p, guid);
            case HighGuid.AreaTrigger:
                return GetAreaTrigger(p, guid);
            case HighGuid.Corpse:
                return GetCorpse(p, guid);
            case HighGuid.SceneObject:
                return GetSceneObject(p, guid);
            case HighGuid.Conversation:
                return GetConversation(p, guid);
            default:
                return null;
        }
    }

    public WorldObject GetObjectByTypeMask(WorldObject p, ObjectGuid guid, TypeMask typemask)
    {
        switch (guid.High)
        {
            case HighGuid.Item:
                if (typemask.HasAnyFlag(TypeMask.Item) && p.IsTypeId(TypeId.Player))
                    return ((Player)p).GetItemByGuid(guid);

                break;
            case HighGuid.Player:
                if (typemask.HasAnyFlag(TypeMask.Player))
                    return GetPlayer(p, guid);

                break;
            case HighGuid.Transport:
            case HighGuid.GameObject:
                if (typemask.HasAnyFlag(TypeMask.GameObject))
                    return GetGameObject(p, guid);

                break;
            case HighGuid.Creature:
            case HighGuid.Vehicle:
                if (typemask.HasAnyFlag(TypeMask.Unit))
                    return GetCreature(p, guid);

                break;
            case HighGuid.Pet:
                if (typemask.HasAnyFlag(TypeMask.Unit))
                    return GetPet(p, guid);

                break;
            case HighGuid.DynamicObject:
                if (typemask.HasAnyFlag(TypeMask.DynamicObject))
                    return GetDynamicObject(p, guid);

                break;
            case HighGuid.AreaTrigger:
                if (typemask.HasAnyFlag(TypeMask.AreaTrigger))
                    return GetAreaTrigger(p, guid);

                break;
            case HighGuid.SceneObject:
                if (typemask.HasAnyFlag(TypeMask.SceneObject))
                    return GetSceneObject(p, guid);

                break;
            case HighGuid.Conversation:
                if (typemask.HasAnyFlag(TypeMask.Conversation))
                    return GetConversation(p, guid);

                break;
            case HighGuid.Corpse:
                break;
        }

        return null;
    }

    public static Corpse GetCorpse(WorldObject u, ObjectGuid guid)
    {
        return u.Map.GetCorpse(guid);
    }

    public static GameObject GetGameObject(WorldObject u, ObjectGuid guid)
    {
        return u.Map.GetGameObject(guid);
    }

    public static Transport GetTransport(WorldObject u, ObjectGuid guid)
    {
        return u.Map.GetTransport(guid);
    }

    public static Conversation GetConversation(WorldObject u, ObjectGuid guid)
    {
        return u.Map.GetConversation(guid);
    }

    public Unit GetUnit(WorldObject u, ObjectGuid guid)
    {
        if (guid.IsPlayer)
            return GetPlayer(u, guid);

        if (guid.IsPet)
            return GetPet(u, guid);

        return GetCreature(u, guid);
    }

    public static Creature GetCreature(WorldObject u, ObjectGuid guid)
    {
        return u.Map.GetCreature(guid);
    }

    public static Pet GetPet(WorldObject u, ObjectGuid guid)
    {
        return u.Map.GetPet(guid);
    }

    public Player GetPlayer(Map m, ObjectGuid guid)
    {
        var player = _players.LookupByKey(guid);

        if (player)
            if (player.IsInWorld && player.Map == m)
                return player;

        return null;
    }

    public Player GetPlayer(WorldObject u, ObjectGuid guid)
    {
        return GetPlayer(u.Map, guid);
    }

    public static Creature GetCreatureOrPetOrVehicle(WorldObject u, ObjectGuid guid)
    {
        if (guid.IsPet)
            return GetPet(u, guid);

        if (guid.IsCreatureOrVehicle)
            return GetCreature(u, guid);

        return null;
    }

    // these functions return objects if found in whole world
    // ACCESS LIKE THAT IS NOT THREAD SAFE
    public Player FindPlayer(ObjectGuid guid)
    {
        var player = FindConnectedPlayer(guid);

        return player && player.IsInWorld ? player : null;
    }

    public Player FindPlayerByName(string name)
    {
        var player = PlayerNameMapHolder.Find(name);

        if (!player || !player.IsInWorld)
            return null;

        return player;
    }

    public Player FindPlayerByLowGUID(ulong lowguid)
    {
        var guid = ObjectGuid.Create(HighGuid.Player, lowguid);

        return FindPlayer(guid);
    }

    // this returns Player even if he is not in world, for example teleporting
    public Player FindConnectedPlayer(ObjectGuid guid)
    {
        lock (_lockObject)
        {
            return _players.LookupByKey(guid);
        }
    }

    public Player FindConnectedPlayerByName(string name)
    {
        return PlayerNameMapHolder.Find(name);
    }

    public void SaveAllPlayers()
    {
        lock (_lockObject)
        {
            foreach (var pl in GetPlayers())
                pl.SaveToDB();
        }
    }

    public ICollection<Player> GetPlayers()
    {
        lock (_lockObject)
        {
            return _players.Values;
        }
    }

    public void AddObject(Player obj)
    {
        lock (_lockObject)
        {
            PlayerNameMapHolder.Insert(obj);
            _players[obj.GUID] = obj;
        }
    }

    public void RemoveObject(Player obj)
    {
        lock (_lockObject)
        {
            PlayerNameMapHolder.Remove(obj);
            _players.Remove(obj.GUID);
        }
    }

    static DynamicObject GetDynamicObject(WorldObject u, ObjectGuid guid)
    {
        return u.Map.GetDynamicObject(guid);
    }

    static AreaTrigger GetAreaTrigger(WorldObject u, ObjectGuid guid)
    {
        return u.Map.GetAreaTrigger(guid);
    }

    static SceneObject GetSceneObject(WorldObject u, ObjectGuid guid)
    {
        return u.Map.GetSceneObject(guid);
    }
}

class PlayerNameMapHolder
{
    static readonly Dictionary<string, Player> _playerNameMap = new();

    public static void Insert(Player p)
    {
        _playerNameMap[p.GetName()] = p;
    }

    public static void Remove(Player p)
    {
        _playerNameMap.Remove(p.GetName());
    }

    public static Player Find(string name)
    {
        if (!ObjectManager.NormalizePlayerName(ref name))
            return null;

        return _playerNameMap.LookupByKey(name);
    }
}
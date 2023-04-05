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
    private readonly object _lockObject = new();
    private readonly Dictionary<ObjectGuid, Player> _players = new();

    private ObjectAccessor() { }

    public static Conversation GetConversation(WorldObject u, ObjectGuid guid)
    {
        return u.Location.Map.GetConversation(guid);
    }

    public static Corpse GetCorpse(WorldObject u, ObjectGuid guid)
    {
        return u.Location.Map.GetCorpse(guid);
    }

    public static Creature GetCreature(WorldObject u, ObjectGuid guid)
    {
        return u.Location.Map.GetCreature(guid);
    }

    public static Creature GetCreatureOrPetOrVehicle(WorldObject u, ObjectGuid guid)
    {
        if (guid.IsPet)
            return GetPet(u, guid);

        if (guid.IsCreatureOrVehicle)
            return GetCreature(u, guid);

        return null;
    }

    public static GameObject GetGameObject(WorldObject u, ObjectGuid guid)
    {
        return u.Location.Map.GetGameObject(guid);
    }

    public static Pet GetPet(WorldObject u, ObjectGuid guid)
    {
        return u.Location.Map.GetPet(guid);
    }

    public static Transport GetTransport(WorldObject u, ObjectGuid guid)
    {
        return u.Location.Map.GetTransport(guid);
    }

    public void AddObject(Player obj)
    {
        lock (_lockObject)
        {
            PlayerNameMapHolder.Insert(obj);
            _players[obj.GUID] = obj;
        }
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

    // these functions return objects if found in whole world
    // ACCESS LIKE THAT IS NOT THREAD SAFE
    public Player FindPlayer(ObjectGuid guid)
    {
        var player = FindConnectedPlayer(guid);

        return player && player.Location.IsInWorld ? player : null;
    }

    public Player FindPlayerByLowGUID(ulong lowguid)
    {
        var guid = ObjectGuid.Create(HighGuid.Player, lowguid);

        return FindPlayer(guid);
    }

    public Player FindPlayerByName(string name)
    {
        var player = PlayerNameMapHolder.Find(name);

        if (!player || !player.Location.IsInWorld)
            return null;

        return player;
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

    public Player GetPlayer(Map m, ObjectGuid guid)
    {
        var player = _players.LookupByKey(guid);

        if (player)
            if (player.Location.IsInWorld && player.Location.Map == m)
                return player;

        return null;
    }

    public Player GetPlayer(WorldObject u, ObjectGuid guid)
    {
        return GetPlayer(u.Location.Map, guid);
    }

    public ICollection<Player> GetPlayers()
    {
        lock (_lockObject)
        {
            return _players.Values;
        }
    }

    public Unit GetUnit(WorldObject u, ObjectGuid guid)
    {
        if (guid.IsPlayer)
            return GetPlayer(u, guid);

        if (guid.IsPet)
            return GetPet(u, guid);

        return GetCreature(u, guid);
    }

    public WorldObject GetWorldObject(WorldObject p, ObjectGuid guid)
    {
        return guid.High switch
        {
            HighGuid.Player        => GetPlayer(p, guid),
            HighGuid.Transport     => GetGameObject(p, guid),
            HighGuid.GameObject    => GetGameObject(p, guid),
            HighGuid.Vehicle       => GetCreature(p, guid),
            HighGuid.Creature      => GetCreature(p, guid),
            HighGuid.Pet           => GetPet(p, guid),
            HighGuid.DynamicObject => GetDynamicObject(p, guid),
            HighGuid.AreaTrigger   => GetAreaTrigger(p, guid),
            HighGuid.Corpse        => GetCorpse(p, guid),
            HighGuid.SceneObject   => GetSceneObject(p, guid),
            HighGuid.Conversation  => GetConversation(p, guid),
            _                      => null
        };
    }
    public void RemoveObject(Player obj)
    {
        lock (_lockObject)
        {
            PlayerNameMapHolder.Remove(obj);
            _players.Remove(obj.GUID);
        }
    }

    public void SaveAllPlayers()
    {
        lock (_lockObject)
        {
            foreach (var pl in GetPlayers())
                pl.SaveToDB();
        }
    }
    private static AreaTrigger GetAreaTrigger(WorldObject u, ObjectGuid guid)
    {
        return u.Location.Map.GetAreaTrigger(guid);
    }

    private static DynamicObject GetDynamicObject(WorldObject u, ObjectGuid guid)
    {
        return u.Location.Map.GetDynamicObject(guid);
    }
    private static SceneObject GetSceneObject(WorldObject u, ObjectGuid guid)
    {
        return u.Location.Map.GetSceneObject(guid);
    }
}
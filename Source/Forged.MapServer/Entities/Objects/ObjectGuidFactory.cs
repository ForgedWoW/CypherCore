// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.World;
using Framework.Constants;

namespace Forged.MapServer.Entities.Objects;

public class ObjectGuidFactory
{
    public static ObjectGuid CreateChatChannel(uint realmId, bool builtIn, bool trade, ushort zoneId, byte factionGroupMask, ulong counter)
    {
        return new ObjectGuid(((ulong)HighGuid.ChatChannel << 58) | ((ulong)(GetRealmIdForObjectGuid(realmId) & 0x1FFF) << 42) | ((ulong)(builtIn ? 1 : 0) << 25) | ((ulong)(trade ? 1 : 0) << 24) | ((ulong)(zoneId & 0x3FFF) << 10) | ((ulong)(factionGroupMask & 0x3F) << 4), counter);
    }

    public static ObjectGuid CreateClient(HighGuid type, uint realmId, uint arg1, ulong counter)
    {
        return new ObjectGuid(((ulong)type << 58) | ((ulong)(GetRealmIdForObjectGuid(realmId) & 0x1FFF) << 42) | ((ulong)(arg1 & 0xFFFFFFFF) << 10), counter);
    }

    public static ObjectGuid CreateClientActor(ushort ownerType, ushort ownerId, uint counter)
    {
        return new ObjectGuid(((ulong)HighGuid.ClientActor << 58) | ((ulong)(ownerType & 0x1FFF) << 42) | ((ulong)(ownerId & 0xFFFFFF) << 26), counter);
    }

    public static ObjectGuid CreateClubFinder(uint realmId, byte type, uint clubFinderId, ulong dbId)
    {
        return new ObjectGuid(((ulong)HighGuid.ClubFinder << 58) | (type == 1 ? ((ulong)(GetRealmIdForObjectGuid(realmId) & 0x1FFF) << 42) : 0ul) | ((ulong)(type & 0xFF) << 33) | clubFinderId & 0xFFFFFFFF, dbId);
    }

    public static ObjectGuid CreateGlobal(HighGuid type, ulong dbIdHigh, ulong dbId)
    {
        return new ObjectGuid(((ulong)type << 58) | dbIdHigh & 0x3FFFFFFFFFFFFFF, dbId);
    }

    public static ObjectGuid CreateGuild(HighGuid type, uint realmId, ulong dbId)
    {
        return new ObjectGuid(((ulong)type << 58) | ((ulong)GetRealmIdForObjectGuid(realmId) << 42), dbId);
    }

    public static ObjectGuid CreateItem(uint realmId, ulong dbId)
    {
        return new ObjectGuid(((ulong)(HighGuid.Item) << 58) | ((ulong)(GetRealmIdForObjectGuid(realmId)) << 42), dbId);
    }

    public static ObjectGuid CreateLFGList(byte arg1, ulong counter)
    {
        return new ObjectGuid(((ulong)HighGuid.LFGObject << 58) | ((ulong)(arg1 & 0xF) << 54), counter);
    }

    public static ObjectGuid CreateLFGObject(byte arg1, byte arg2, byte arg3, byte arg4, bool arg5, byte arg6, ulong counter)
    {
        return new ObjectGuid(((ulong)HighGuid.LFGObject << 58) | ((ulong)(arg1 & 0xF) << 54) | ((ulong)(arg2 & 0xF) << 50) | ((ulong)(arg3 & 0xF) << 46) | ((ulong)(arg4 & 0xFF) << 38) | ((ulong)(arg5 ? 1 : 0) << 37) | ((ulong)(arg6 & 0x3) << 35), counter);
    }

    public static ObjectGuid CreateMobileSession(uint realmId, ushort arg1, ulong counter)
    {
        return new ObjectGuid(((ulong)HighGuid.MobileSession << 58) | ((ulong)GetRealmIdForObjectGuid(realmId) << 42) | ((ulong)(arg1 & 0x1FF) << 33), counter);
    }

    public static ObjectGuid CreateNull()
    {
        return new ObjectGuid();
    }

    public static ObjectGuid CreatePlayer(uint realmId, ulong dbId)
    {
        return new ObjectGuid(((ulong)HighGuid.Player << 58) | ((ulong)(GetRealmIdForObjectGuid(realmId)) << 42), dbId);
    }

    public static ObjectGuid CreateToolsClient(uint mapId, uint serverId, ulong counter)
    {
        return new ObjectGuid(((ulong)HighGuid.ToolsClient << 58) | mapId, ((ulong)(serverId & 0xFFFFFF) << 40) | (counter & 0xFFFFFFFFFF));
    }

    public static ObjectGuid CreateTransport(HighGuid type, ulong counter)
    {
        return new ObjectGuid(((ulong)type << 58) | (counter << 38), 0ul);
    }

    public static ObjectGuid CreateUniq(ulong id)
    {
        return new ObjectGuid((ulong)HighGuid.Uniq << 58, id);
    }
    public static ObjectGuid CreateWebObj(uint realmId, byte arg1, byte arg2, ulong counter)
    {
        return new ObjectGuid(((ulong)HighGuid.WebObj << 58) | ((ulong)(GetRealmIdForObjectGuid(realmId) & 0x1FFF) << 42) | ((ulong)(arg1 & 0x1F) << 37) | ((ulong)(arg2 & 0x3) << 35), counter);
    }

    public static ObjectGuid CreateWorldLayer(uint arg1, ushort arg2, byte arg3, uint arg4)
    {
        return new ObjectGuid(((ulong)HighGuid.WorldLayer << 58) | ((ulong)(arg1 & 0xFFFFFFFF) << 10) | arg2 & 0x1FFu, ((ulong)(arg3 & 0xFF) << 24) | arg4 & 0x7FFFFF);
    }

    public static ObjectGuid CreateWorldObject(HighGuid type, byte subType, uint realmId, ushort mapId, uint serverId, uint entry, ulong counter)
    {
        return new ObjectGuid(((ulong)type << 58) | ((ulong)(GetRealmIdForObjectGuid(realmId) & 0x1FFF) << 42) | ((ulong)(mapId & 0x1FFF) << 29) | ((ulong)(entry & 0x7FFFFF) << 6) | ((ulong)(subType) & 0x3F), ((ulong)(serverId & 0xFFFFFF) << 40) | (counter & 0xFFFFFFFFFF));
    }
    private static uint GetRealmIdForObjectGuid(uint realmId)
    {
        return realmId != 0 ? realmId : WorldManager.Realm.Id.Index;
    }
}
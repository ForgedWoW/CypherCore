// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;

namespace Game.Entities;

class ObjectGuidInfo
{
	static readonly Dictionary<HighGuid, string> Names = new();
	static readonly Dictionary<HighGuid, Func<HighGuid, ObjectGuid, string>> ClientFormatFunction = new();
	static readonly Dictionary<HighGuid, Func<HighGuid, string, ObjectGuid>> ClientParseFunction = new();

	static ObjectGuidInfo()
	{
		SET_GUID_INFO(HighGuid.Null, FormatNull, ParseNull);
		SET_GUID_INFO(HighGuid.Uniq, FormatUniq, ParseUniq);
		SET_GUID_INFO(HighGuid.Player, FormatPlayer, ParsePlayer);
		SET_GUID_INFO(HighGuid.Item, FormatItem, ParseItem);
		SET_GUID_INFO(HighGuid.WorldTransaction, FormatWorldObject, ParseWorldObject);
		SET_GUID_INFO(HighGuid.StaticDoor, FormatTransport, ParseTransport);
		SET_GUID_INFO(HighGuid.Transport, FormatTransport, ParseTransport);
		SET_GUID_INFO(HighGuid.Conversation, FormatWorldObject, ParseWorldObject);
		SET_GUID_INFO(HighGuid.Creature, FormatWorldObject, ParseWorldObject);
		SET_GUID_INFO(HighGuid.Vehicle, FormatWorldObject, ParseWorldObject);
		SET_GUID_INFO(HighGuid.Pet, FormatWorldObject, ParseWorldObject);
		SET_GUID_INFO(HighGuid.GameObject, FormatWorldObject, ParseWorldObject);
		SET_GUID_INFO(HighGuid.DynamicObject, FormatWorldObject, ParseWorldObject);
		SET_GUID_INFO(HighGuid.AreaTrigger, FormatWorldObject, ParseWorldObject);
		SET_GUID_INFO(HighGuid.Corpse, FormatWorldObject, ParseWorldObject);
		SET_GUID_INFO(HighGuid.LootObject, FormatWorldObject, ParseWorldObject);
		SET_GUID_INFO(HighGuid.SceneObject, FormatWorldObject, ParseWorldObject);
		SET_GUID_INFO(HighGuid.Scenario, FormatWorldObject, ParseWorldObject);
		SET_GUID_INFO(HighGuid.AIGroup, FormatWorldObject, ParseWorldObject);
		SET_GUID_INFO(HighGuid.DynamicDoor, FormatWorldObject, ParseWorldObject);
		SET_GUID_INFO(HighGuid.ClientActor, FormatClientActor, ParseClientActor);
		SET_GUID_INFO(HighGuid.Vignette, FormatWorldObject, ParseWorldObject);
		SET_GUID_INFO(HighGuid.CallForHelp, FormatWorldObject, ParseWorldObject);
		SET_GUID_INFO(HighGuid.AIResource, FormatWorldObject, ParseWorldObject);
		SET_GUID_INFO(HighGuid.AILock, FormatWorldObject, ParseWorldObject);
		SET_GUID_INFO(HighGuid.AILockTicket, FormatWorldObject, ParseWorldObject);
		SET_GUID_INFO(HighGuid.ChatChannel, FormatChatChannel, ParseChatChannel);
		SET_GUID_INFO(HighGuid.Party, FormatGlobal, ParseGlobal);
		SET_GUID_INFO(HighGuid.Guild, FormatGuild, ParseGuild);
		SET_GUID_INFO(HighGuid.WowAccount, FormatGlobal, ParseGlobal);
		SET_GUID_INFO(HighGuid.BNetAccount, FormatGlobal, ParseGlobal);
		SET_GUID_INFO(HighGuid.GMTask, FormatGlobal, ParseGlobal);
		SET_GUID_INFO(HighGuid.MobileSession, FormatMobileSession, ParseMobileSession);
		SET_GUID_INFO(HighGuid.RaidGroup, FormatGlobal, ParseGlobal);
		SET_GUID_INFO(HighGuid.Spell, FormatGlobal, ParseGlobal);
		SET_GUID_INFO(HighGuid.Mail, FormatGlobal, ParseGlobal);
		SET_GUID_INFO(HighGuid.WebObj, FormatWebObj, ParseWebObj);
		SET_GUID_INFO(HighGuid.LFGObject, FormatLFGObject, ParseLFGObject);
		SET_GUID_INFO(HighGuid.LFGList, FormatLFGList, ParseLFGList);
		SET_GUID_INFO(HighGuid.UserRouter, FormatGlobal, ParseGlobal);
		SET_GUID_INFO(HighGuid.PVPQueueGroup, FormatGlobal, ParseGlobal);
		SET_GUID_INFO(HighGuid.UserClient, FormatGlobal, ParseGlobal);
		SET_GUID_INFO(HighGuid.PetBattle, FormatClient, ParseClient);
		SET_GUID_INFO(HighGuid.UniqUserClient, FormatClient, ParseClient);
		SET_GUID_INFO(HighGuid.BattlePet, FormatGlobal, ParseGlobal);
		SET_GUID_INFO(HighGuid.CommerceObj, FormatGlobal, ParseGlobal);
		SET_GUID_INFO(HighGuid.ClientSession, FormatClient, ParseClient);
		SET_GUID_INFO(HighGuid.Cast, FormatWorldObject, ParseWorldObject);
		SET_GUID_INFO(HighGuid.ClientConnection, FormatClient, ParseClient);
		SET_GUID_INFO(HighGuid.ClubFinder, FormatClubFinder, ParseClubFinder);
	}

	public static string Format(ObjectGuid guid)
	{
		if (guid.GetHigh() >= HighGuid.Count)
			return "Uniq-WOWGUID_TO_STRING_FAILED";

		if (ClientFormatFunction[guid.GetHigh()] == null)
			return "Uniq-WOWGUID_TO_STRING_FAILED";

		return ClientFormatFunction[guid.GetHigh()](guid.GetHigh(), guid);
	}

	public static ObjectGuid Parse(string guidString)
	{
		var typeEnd = guidString.IndexOf('-');

		if (typeEnd == -1)
			return ObjectGuid.FromStringFailed;

		if (!Enum.TryParse<HighGuid>(guidString.Substring(0, typeEnd), out var type))
			return ObjectGuid.FromStringFailed;

		if (type >= HighGuid.Count)
			return ObjectGuid.FromStringFailed;

		return ClientParseFunction[type](type, guidString.Substring(typeEnd + 1));
	}

	static void SET_GUID_INFO(HighGuid type, Func<HighGuid, ObjectGuid, string> format, Func<HighGuid, string, ObjectGuid> parse)
	{
		Names[type] = type.ToString();
		ClientFormatFunction[type] = format;
		ClientParseFunction[type] = parse;
	}

	static string FormatNull(HighGuid typeName, ObjectGuid guid)
	{
		return "0000000000000000";
	}

	static ObjectGuid ParseNull(HighGuid type, string guidString)
	{
		return ObjectGuid.Empty;
	}

	static string FormatUniq(HighGuid typeName, ObjectGuid guid)
	{
		string[] uniqNames =
		{
			null, "WOWGUID_UNIQUE_PROBED_DELETE", "WOWGUID_UNIQUE_JAM_TEMP", "WOWGUID_TO_STRING_FAILED", "WOWGUID_FROM_STRING_FAILED", "WOWGUID_UNIQUE_SERVER_SELF", "WOWGUID_UNIQUE_MAGIC_SELF", "WOWGUID_UNIQUE_MAGIC_PET", "WOWGUID_UNIQUE_INVALID_TRANSPORT", "WOWGUID_UNIQUE_AMMO_ID", "WOWGUID_SPELL_TARGET_TRADE_ITEM", "WOWGUID_SCRIPT_TARGET_INVALID", "WOWGUID_SCRIPT_TARGET_NONE", null, "WOWGUID_FAKE_MODERATOR", null, null, "WOWGUID_UNIQUE_ACCOUNT_OBJ_INITIALIZATION"
		};

		var id = guid.GetCounter();

		if ((int)id >= uniqNames.Length)
			id = 3;

		return $"{typeName}-{uniqNames[id]}";
	}

	static ObjectGuid ParseUniq(HighGuid type, string guidString)
	{
		string[] uniqNames =
		{
			null, "WOWGUID_UNIQUE_PROBED_DELETE", "WOWGUID_UNIQUE_JAM_TEMP", "WOWGUID_TO_STRING_FAILED", "WOWGUID_FROM_STRING_FAILED", "WOWGUID_UNIQUE_SERVER_SELF", "WOWGUID_UNIQUE_MAGIC_SELF", "WOWGUID_UNIQUE_MAGIC_PET", "WOWGUID_UNIQUE_INVALID_TRANSPORT", "WOWGUID_UNIQUE_AMMO_ID", "WOWGUID_SPELL_TARGET_TRADE_ITEM", "WOWGUID_SCRIPT_TARGET_INVALID", "WOWGUID_SCRIPT_TARGET_NONE", null, "WOWGUID_FAKE_MODERATOR", null, null, "WOWGUID_UNIQUE_ACCOUNT_OBJ_INITIALIZATION"
		};

		for (var id = 0; id < uniqNames.Length; ++id)
		{
			if (uniqNames[id] == null)
				continue;

			if (guidString.Equals(uniqNames[id]))
				return ObjectGuidFactory.CreateUniq((ulong)id);
		}

		return ObjectGuid.FromStringFailed;
	}

	static string FormatPlayer(HighGuid typeName, ObjectGuid guid)
	{
		return $"{typeName}-{guid.GetRealmId()}-0x{guid.GetLowValue():X16}";
	}

	static ObjectGuid ParsePlayer(HighGuid type, string guidString)
	{
		var split = guidString.Split('-');

		if (split.Length != 2)
			return ObjectGuid.FromStringFailed;

		if (!uint.TryParse(split[0], out var realmId) || !ulong.TryParse(split[1], out var dbId))
			return ObjectGuid.FromStringFailed;

		return ObjectGuidFactory.CreatePlayer(realmId, dbId);
	}

	static string FormatItem(HighGuid typeName, ObjectGuid guid)
	{
		return $"{typeName}-{guid.GetRealmId()}-{(uint)(guid.GetHighValue() >> 18) & 0xFFFFFF}-0x{guid.GetLowValue():X16}";
	}

	static ObjectGuid ParseItem(HighGuid type, string guidString)
	{
		var split = guidString.Split('-');

		if (split.Length != 3)
			return ObjectGuid.FromStringFailed;

		if (!uint.TryParse(split[0], out var realmId) || !uint.TryParse(split[1], out var arg1) || !ulong.TryParse(split[2], out var dbId))
			return ObjectGuid.FromStringFailed;

		return ObjectGuidFactory.CreateItem(realmId, dbId);
	}

	static string FormatWorldObject(HighGuid typeName, ObjectGuid guid)
	{
		return $"{typeName}-{guid.GetSubType()}-{guid.GetRealmId()}-{guid.GetMapId()}-{(uint)(guid.GetLowValue() >> 40) & 0xFFFFFF}-{guid.GetEntry()}-0x{guid.GetCounter():X10}";
	}

	static ObjectGuid ParseWorldObject(HighGuid type, string guidString)
	{
		var split = guidString.Split('-');

		if (split.Length != 6)
			return ObjectGuid.FromStringFailed;

		if (!byte.TryParse(split[0], out var subType) ||
			!uint.TryParse(split[1], out var realmId) ||
			!ushort.TryParse(split[2], out var mapId) ||
			!uint.TryParse(split[3], out var serverId) ||
			!uint.TryParse(split[4], out var id) ||
			!ulong.TryParse(split[5], out var counter))
			return ObjectGuid.FromStringFailed;

		return ObjectGuidFactory.CreateWorldObject(type, subType, realmId, mapId, serverId, id, counter);
	}

	static string FormatTransport(HighGuid typeName, ObjectGuid guid)
	{
		return $"{typeName}-{(guid.GetHighValue() >> 38) & 0xFFFFF}-0x{guid.GetLowValue():X16}";
	}

	static ObjectGuid ParseTransport(HighGuid type, string guidString)
	{
		var split = guidString.Split('-');

		if (split.Length != 2)
			return ObjectGuid.FromStringFailed;

		if (!uint.TryParse(split[0], out var id) || !ulong.TryParse(split[1], out var counter))
			return ObjectGuid.FromStringFailed;

		return ObjectGuidFactory.CreateTransport(type, counter);
	}

	static string FormatClientActor(HighGuid typeName, ObjectGuid guid)
	{
		return $"{typeName}-{guid.GetRealmId()}-{(guid.GetHighValue() >> 26) & 0xFFFFFF}-{guid.GetLowValue()}";
	}

	static ObjectGuid ParseClientActor(HighGuid type, string guidString)
	{
		var split = guidString.Split('-');

		if (split.Length != 3)
			return ObjectGuid.FromStringFailed;

		if (!ushort.TryParse(split[0], out var ownerType) || !ushort.TryParse(split[1], out var ownerId) || !uint.TryParse(split[2], out var counter))
			return ObjectGuid.FromStringFailed;

		return ObjectGuidFactory.CreateClientActor(ownerType, ownerId, counter);
	}

	static string FormatChatChannel(HighGuid typeName, ObjectGuid guid)
	{
		var builtIn = (uint)(guid.GetHighValue() >> 25) & 0x1;
		var trade = (uint)(guid.GetHighValue() >> 24) & 0x1;
		var zoneId = (uint)(guid.GetHighValue() >> 10) & 0x3FFF;
		var factionGroupMask = (uint)(guid.GetHighValue() >> 4) & 0x3F;

		return $"{typeName}-{guid.GetRealmId()}-{builtIn}-{trade}-{zoneId}-{factionGroupMask}-0x{guid.GetLowValue():X8}";
	}

	static ObjectGuid ParseChatChannel(HighGuid type, string guidString)
	{
		var split = guidString.Split('-');

		if (split.Length != 6)
			return ObjectGuid.FromStringFailed;

		if (!uint.TryParse(split[0], out var realmId) ||
			!uint.TryParse(split[1], out var builtIn) ||
			!uint.TryParse(split[2], out var trade) ||
			!ushort.TryParse(split[3], out var zoneId) ||
			!byte.TryParse(split[4], out var factionGroupMask) ||
			!ulong.TryParse(split[5], out var id))
			return ObjectGuid.FromStringFailed;

		return ObjectGuidFactory.CreateChatChannel(realmId, builtIn != 0, trade != 0, zoneId, factionGroupMask, id);
	}

	static string FormatGlobal(HighGuid typeName, ObjectGuid guid)
	{
		return $"{typeName}-{guid.GetHighValue() & 0x3FFFFFFFFFFFFFF}-0x{guid.GetLowValue():X12}";
	}

	static ObjectGuid ParseGlobal(HighGuid type, string guidString)
	{
		var split = guidString.Split('-');

		if (split.Length != 2)
			return ObjectGuid.FromStringFailed;

		if (!ulong.TryParse(split[0], out var dbIdHigh) || !ulong.TryParse(split[1], out var dbIdLow))
			return ObjectGuid.FromStringFailed;

		return ObjectGuidFactory.CreateGlobal(type, dbIdHigh, dbIdLow);
	}

	static string FormatGuild(HighGuid typeName, ObjectGuid guid)
	{
		return $"{typeName}-{guid.GetRealmId()}-0x{guid.GetLowValue():X12}";
	}

	static ObjectGuid ParseGuild(HighGuid type, string guidString)
	{
		var split = guidString.Split('-');

		if (split.Length != 2)
			return ObjectGuid.FromStringFailed;

		if (!uint.TryParse(split[0], out var realmId) || !ulong.TryParse(split[1], out var dbId))
			return ObjectGuid.FromStringFailed;

		return ObjectGuidFactory.CreateGuild(type, realmId, dbId);
	}

	static string FormatMobileSession(HighGuid typeName, ObjectGuid guid)
	{
		return $"{typeName}-{guid.GetRealmId()}-{(guid.GetHighValue() >> 33) & 0x1FF}-0x{guid.GetLowValue():X8}";
	}

	static ObjectGuid ParseMobileSession(HighGuid type, string guidString)
	{
		var split = guidString.Split('-');

		if (split.Length != 3)
			return ObjectGuid.FromStringFailed;

		if (!uint.TryParse(split[0], out var realmId) || !ushort.TryParse(split[1], out var arg1) || !ulong.TryParse(split[2], out var counter))
			return ObjectGuid.FromStringFailed;

		return ObjectGuidFactory.CreateMobileSession(realmId, arg1, counter);
	}

	static string FormatWebObj(HighGuid typeName, ObjectGuid guid)
	{
		return $"{typeName}-{guid.GetRealmId()}-{(guid.GetHighValue() >> 37) & 0x1F}-{(guid.GetHighValue() >> 35) & 0x3}-0x{guid.GetLowValue():X12}";
	}

	static ObjectGuid ParseWebObj(HighGuid type, string guidString)
	{
		var split = guidString.Split('-');

		if (split.Length != 4)
			return ObjectGuid.FromStringFailed;

		if (!uint.TryParse(split[0], out var realmId) || !byte.TryParse(split[1], out var arg1) || !byte.TryParse(split[2], out var arg2) || !ulong.TryParse(split[3], out var counter))
			return ObjectGuid.FromStringFailed;

		return ObjectGuidFactory.CreateWebObj(realmId, arg1, arg2, counter);
	}

	static string FormatLFGObject(HighGuid typeName, ObjectGuid guid)
	{
		return $"{typeName}-{(guid.GetHighValue() >> 54) & 0xF}-{(guid.GetHighValue() >> 50) & 0xF}-{(guid.GetHighValue() >> 46) & 0xF}-" +
				$"{(guid.GetHighValue() >> 38) & 0xFF}-{(guid.GetHighValue() >> 37) & 0x1}-{(guid.GetHighValue() >> 35) & 0x3}-0x{guid.GetLowValue():X6}";
	}

	static ObjectGuid ParseLFGObject(HighGuid type, string guidString)
	{
		var split = guidString.Split('-');

		if (split.Length != 7)
			return ObjectGuid.FromStringFailed;

		if (!byte.TryParse(split[0], out var arg1) ||
			!byte.TryParse(split[1], out var arg2) ||
			!byte.TryParse(split[2], out var arg3) ||
			!byte.TryParse(split[3], out var arg4) ||
			!byte.TryParse(split[4], out var arg5) ||
			!byte.TryParse(split[5], out var arg6) ||
			!ulong.TryParse(split[6], out var counter))
			return ObjectGuid.FromStringFailed;

		return ObjectGuidFactory.CreateLFGObject(arg1, arg2, arg3, arg4, arg5 != 0, arg6, counter);
	}

	static string FormatLFGList(HighGuid typeName, ObjectGuid guid)
	{
		return $"{typeName}-{(guid.GetHighValue() >> 54) & 0xF}-0x{guid.GetLowValue():X6}";
	}

	static ObjectGuid ParseLFGList(HighGuid type, string guidString)
	{
		var split = guidString.Split('-');

		if (split.Length != 2)
			return ObjectGuid.FromStringFailed;

		if (!byte.TryParse(split[0], out var arg1) || !ulong.TryParse(split[1], out var counter))
			return ObjectGuid.FromStringFailed;

		return ObjectGuidFactory.CreateLFGList(arg1, counter);
	}

	static string FormatClient(HighGuid typeName, ObjectGuid guid)
	{
		return $"{typeName}-{guid.GetRealmId()}-{(guid.GetHighValue() >> 10) & 0xFFFFFFFF}-0x{guid.GetLowValue():X12}";
	}

	static ObjectGuid ParseClient(HighGuid type, string guidString)
	{
		var split = guidString.Split('-');

		if (split.Length != 3)
			return ObjectGuid.FromStringFailed;

		if (!uint.TryParse(split[0], out var realmId) || !uint.TryParse(split[1], out var arg1) || !ulong.TryParse(split[2], out var counter))
			return ObjectGuid.FromStringFailed;

		return ObjectGuidFactory.CreateClient(type, realmId, arg1, counter);
	}

	static string FormatClubFinder(HighGuid typeName, ObjectGuid guid)
	{
		var type = (uint)(guid.GetHighValue() >> 33) & 0xFF;
		var clubFinderId = (uint)(guid.GetHighValue() & 0xFFFFFFFF);

		if (type == 1) // guild
			return $"{typeName}-{type}-{clubFinderId}-{guid.GetRealmId()}-{guid.GetLowValue()}";

		return $"{typeName}-{type}-{clubFinderId}-0x{guid.GetLowValue():X16}";
	}

	static ObjectGuid ParseClubFinder(HighGuid type, string guidString)
	{
		var split = guidString.Split('-');

		if (split.Length < 1)
			return ObjectGuid.FromStringFailed;

		if (!byte.TryParse(split[0], out var typeNum))
			return ObjectGuid.FromStringFailed;

		uint clubFinderId = 0;
		uint realmId = 0;
		ulong dbId = 0;

		switch (typeNum)
		{
			case 0: // club
				if (split.Length < 3)
					return ObjectGuid.FromStringFailed;

				if (!uint.TryParse(split[0], out clubFinderId) || !ulong.TryParse(split[1], out dbId))
					return ObjectGuid.FromStringFailed;

				break;
			case 1: // guild
				if (split.Length < 4)
					return ObjectGuid.FromStringFailed;

				if (!uint.TryParse(split[0], out clubFinderId) || !uint.TryParse(split[1], out realmId) || !ulong.TryParse(split[2], out dbId))
					return ObjectGuid.FromStringFailed;

				break;
			default:
				return ObjectGuid.FromStringFailed;
		}

		return ObjectGuidFactory.CreateClubFinder(realmId, typeNum, clubFinderId, dbId);
	}

	string FormatToolsClient(HighGuid typeName, ObjectGuid guid)
	{
		return $"{typeName}-{guid.GetMapId()}-{(uint)(guid.GetLowValue() >> 40) & 0xFFFFFF}-{guid.GetCounter():X10}";
	}

	ObjectGuid ParseToolsClient(HighGuid type, string guidString)
	{
		var split = guidString.Split('-');

		if (split.Length != 3)
			return ObjectGuid.FromStringFailed;

		if (!uint.TryParse(split[0], out var mapId) || !uint.TryParse(split[1], out var serverId) || !ulong.TryParse(split[2], out var counter))
			return ObjectGuid.FromStringFailed;

		return ObjectGuidFactory.CreateToolsClient(mapId, serverId, counter);
	}

	string FormatWorldLayer(HighGuid typeName, ObjectGuid guid)
	{
		return $"{typeName}-{(uint)((guid.GetHighValue() >> 10) & 0xFFFFFFFF)}-{(uint)(guid.GetHighValue() & 0x1FF)}-{(uint)((guid.GetLowValue() >> 24) & 0xFF)}-{(uint)(guid.GetLowValue() & 0x7FFFFF)}";
	}

	ObjectGuid ParseWorldLayer(HighGuid type, string guidString)
	{
		var split = guidString.Split('-');

		if (split.Length != 4)
			return ObjectGuid.FromStringFailed;

		if (!uint.TryParse(split[0], out var arg1) || !ushort.TryParse(split[1], out var arg2) || !byte.TryParse(split[2], out var arg3) || !uint.TryParse(split[0], out var arg4))
			return ObjectGuid.FromStringFailed;

		return ObjectGuidFactory.CreateWorldLayer(arg1, arg2, arg3, arg4);
	}
}
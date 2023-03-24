// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Bgs.Protocol.GameUtilities.V1;
using Framework.Constants;
using Framework.Serialization;
using Framework.Web;
using Google.Protobuf;

namespace Game.Common.Services;

public class RealmRequestService
{
	[Service(OriginalHash.GameUtilitiesService, 1)]
	BattlenetRpcErrorCode HandleProcessClientRequest(ClientRequest request, ClientResponse response)
	{
		Bgs.Protocol.Attribute command = null;
		Dictionary<string, Bgs.Protocol.Variant> Params = new();

		string removeSuffix(string str)
		{
			var pos = str.IndexOf('_');

			if (pos != -1)
				return str.Substring(0, pos);

			return str;
		}

		for (var i = 0; i < request.Attribute.Count; ++i)
		{
			var attr = request.Attribute[i];

			if (attr.Name.Contains("Command_"))
			{
				command = attr;
				Params[removeSuffix(attr.Name)] = attr.Value;
			}
			else
			{
				Params[attr.Name] = attr.Value;
			}
		}

		if (command == null)
		{
			Log.outError(LogFilter.SessionRpc, "{0} sent ClientRequest with no command.", GetPlayerInfo());

			return BattlenetRpcErrorCode.RpcMalformedRequest;
		}

		return removeSuffix(command.Name) switch
		{
			"Command_RealmListRequest_v1" => HandleRealmListRequest(Params, response),
			"Command_RealmJoinRequest_v1" => HandleRealmJoinRequest(Params, response),
			_                             => BattlenetRpcErrorCode.RpcNotImplemented
		};
	}

	[Service(OriginalHash.GameUtilitiesService, 10)]
	BattlenetRpcErrorCode HandleGetAllValuesForAttribute(GetAllValuesForAttributeRequest request, GetAllValuesForAttributeResponse response)
	{
		if (!request.AttributeKey.Contains("Command_RealmListRequest_v1"))
		{
			Global.RealmMgr.WriteSubRegions(response);

			return BattlenetRpcErrorCode.Ok;
		}

		return BattlenetRpcErrorCode.RpcNotImplemented;
	}

	BattlenetRpcErrorCode HandleRealmListRequest(Dictionary<string, Bgs.Protocol.Variant> Params, ClientResponse response)
	{
		var subRegionId = "";
		var subRegion = Params.LookupByKey("Command_RealmListRequest_v1");

		if (subRegion != null)
			subRegionId = subRegion.StringValue;

		var compressed = Global.RealmMgr.GetRealmList(Global.WorldMgr.Realm.Build, subRegionId);

		if (compressed.Empty())
			return BattlenetRpcErrorCode.UtilServerFailedToSerializeResponse;

		Bgs.Protocol.Attribute attribute = new();
		attribute.Name = "Param_RealmList";
		attribute.Value = new Bgs.Protocol.Variant();
		attribute.Value.BlobValue = ByteString.CopyFrom(compressed);
		response.Attribute.Add(attribute);

		var realmCharacterCounts = new RealmCharacterCountList();

		foreach (var characterCount in RealmCharacterCounts)
		{
			RealmCharacterCountEntry countEntry = new();
			countEntry.WowRealmAddress = (int)characterCount.Key;
			countEntry.Count = characterCount.Value;
			realmCharacterCounts.Counts.Add(countEntry);
		}

		compressed = Json.Deflate("JSONRealmCharacterCountList", realmCharacterCounts);

		attribute = new Bgs.Protocol.Attribute();
		attribute.Name = "Param_CharacterCountList";
		attribute.Value = new Bgs.Protocol.Variant();
		attribute.Value.BlobValue = ByteString.CopyFrom(compressed);
		response.Attribute.Add(attribute);

		return BattlenetRpcErrorCode.Ok;
	}

	BattlenetRpcErrorCode HandleRealmJoinRequest(Dictionary<string, Bgs.Protocol.Variant> Params, ClientResponse response)
	{
		var realmAddress = Params.LookupByKey("Param_RealmAddress");

		if (realmAddress != null)
			return Global.RealmMgr.JoinRealm((uint)realmAddress.UintValue,
											Global.WorldMgr.Realm.Build,
											System.Net.IPAddress.Parse(RemoteAddress),
											RealmListSecret,
											SessionDbcLocale,
											OS,
											AccountName,
											response);

		return BattlenetRpcErrorCode.Ok;
	}
}

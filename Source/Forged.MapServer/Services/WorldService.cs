// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Net;
using Bgs.Protocol;
using Bgs.Protocol.GameUtilities.V1;
using Forged.MapServer.Server;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Serialization;
using Framework.Web;
using Game.Common.Handlers;
using Google.Protobuf;
using Serilog;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.Services;

public class WorldService : IWorldSocketHandler
{
    private readonly RealmManager _realmManager;
    private readonly WorldSession _session;

    public WorldService(WorldSession session, RealmManager realmManager)
    {
        _session = session;
        _realmManager = realmManager;
    }

    [Service(OriginalHash.GameUtilitiesService, 10)]
    private BattlenetRpcErrorCode HandleGetAllValuesForAttribute(GetAllValuesForAttributeRequest request, GetAllValuesForAttributeResponse response)
    {
        if (request.AttributeKey.Contains("Command_RealmListRequest_v1"))
            return BattlenetRpcErrorCode.RpcNotImplemented;

        _realmManager.WriteSubRegions(response);

        return BattlenetRpcErrorCode.Ok;
    }

    [Service(OriginalHash.GameUtilitiesService, 1)]
    private BattlenetRpcErrorCode HandleProcessClientRequest(ClientRequest request, ClientResponse response)
    {
        Attribute command = null;
        Dictionary<string, Variant> requestParameters = new();

        string RemoveSuffix(string str)
        {
            var pos = str.IndexOf('_');

            return pos != -1 ? str[..pos] : str;
        }

        for (var i = 0; i < request.Attribute.Count; ++i)
        {
            var attr = request.Attribute[i];

            if (attr.Name.Contains("Command_"))
            {
                command = attr;
                requestParameters[RemoveSuffix(attr.Name)] = attr.Value;
            }
            else
                requestParameters[attr.Name] = attr.Value;
        }

        if (command != null)
            return RemoveSuffix(command.Name) switch
            {
                "Command_RealmListRequest_v1" => HandleRealmListRequest(requestParameters, response),
                "Command_RealmJoinRequest_v1" => HandleRealmJoinRequest(requestParameters, response),
                _ => BattlenetRpcErrorCode.RpcNotImplemented
            };

        Log.Logger.Error("{0} sent ClientRequest with no command.", _session.GetPlayerInfo());

        return BattlenetRpcErrorCode.RpcMalformedRequest;
    }

    private BattlenetRpcErrorCode HandleRealmJoinRequest(Dictionary<string, Variant> requestParameters, ClientResponse response)
    {
        if (requestParameters.TryGetValue("Param_RealmAddress", out var realmAddress))
            return _realmManager.JoinRealm((uint)realmAddress.UintValue,
                                             WorldManager.Realm.Build,
                                             IPAddress.Parse(_session.RemoteAddress),
                                             _session.RealmListSecret,
                                             _session.SessionDbcLocale,
                                             _session.OS,
                                             _session.AccountName,
                                             response);

        return BattlenetRpcErrorCode.Ok;
    }

    private BattlenetRpcErrorCode HandleRealmListRequest(Dictionary<string, Variant> requestParameters, ClientResponse response)
    {
        var subRegionId = "";

        if (requestParameters.TryGetValue("Command_RealmListRequest_v1", out var subRegion))
            subRegionId = subRegion.StringValue;

        var compressed = _realmManager.GetRealmList(WorldManager.Realm.Build, subRegionId);

        if (compressed.Empty())
            return BattlenetRpcErrorCode.UtilServerFailedToSerializeResponse;

        Attribute attribute = new()
        {
            Name = "Param_RealmList",
            Value = new Variant
            {
                BlobValue = ByteString.CopyFrom(compressed)
            }
        };

        response.Attribute.Add(attribute);

        var realmCharacterCounts = new RealmCharacterCountList();

        foreach (var characterCount in _session.RealmCharacterCounts)
        {
            RealmCharacterCountEntry countEntry = new()
            {
                WowRealmAddress = (int)characterCount.Key,
                Count = characterCount.Value
            };

            realmCharacterCounts.Counts.Add(countEntry);
        }

        compressed = Json.Deflate("JSONRealmCharacterCountList", realmCharacterCounts);

        attribute = new Attribute
        {
            Name = "Param_CharacterCountList",
            Value = new Variant
            {
                BlobValue = ByteString.CopyFrom(compressed)
            }
        };

        response.Attribute.Add(attribute);

        return BattlenetRpcErrorCode.Ok;
    }
}
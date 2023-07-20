// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Social;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;

namespace Forged.MapServer.Entities.Players;

public class SocialManager
{
    public const int FRIEND_LIMIT_MAX = 50;
    public const int IGNORE_LIMIT = 50;
    private readonly IConfiguration _configuration;
    private readonly ObjectAccessor _objectAccessor;
    private readonly CharacterDatabase _characterDatabase;
    private readonly Dictionary<ObjectGuid, PlayerSocial> _socialMap = new();

    public SocialManager(ObjectAccessor objectAccessor, IConfiguration configuration, CharacterDatabase characterDatabase)
    {
        _objectAccessor = objectAccessor;
        _configuration = configuration;
        _characterDatabase = characterDatabase;
    }

    public void GetFriendInfo(Player player, ObjectGuid friendGuid, FriendInfo friendInfo)
    {
        if (player == null)
            return;

        friendInfo.Status = FriendStatus.Offline;
        friendInfo.Area = 0;
        friendInfo.Level = 0;
        friendInfo.Class = 0;

        var target = _objectAccessor.FindPlayer(friendGuid);

        if (target == null)
            return;

        if (player.Social.PlayerSocialMap.TryGetValue(friendGuid, out var playerFriendInfo))
            friendInfo.Note = playerFriendInfo.Note;

        // PLAYER see his team only and PLAYER can't see MODERATOR, GAME MASTER, ADMINISTRATOR characters
        // MODERATOR, GAME MASTER, ADMINISTRATOR can see all

        if (!player.Session.HasPermission(RBACPermissions.WhoSeeAllSecLevels) &&
            target.Session.Security > (AccountTypes)_configuration.GetDefaultValue("GM:InWhoList:Level", (int)AccountTypes.Administrator))
            return;

        // player can see member of other team only if CONFIG_ALLOW_TWO_SIDE_WHO_LIST
        if (target.Team != player.Team && !player.Session.HasPermission(RBACPermissions.TwoSideWhoList))
            return;

        if (target.IsVisibleGloballyFor(player))
        {
            if (target.IsDnd)
                friendInfo.Status = FriendStatus.Dnd;
            else if (target.IsAfk)
                friendInfo.Status = FriendStatus.Afk;
            else
            {
                friendInfo.Status = FriendStatus.Online;

                if (target.Session.RecruiterId == player.Session.AccountId || target.Session.AccountId == player.Session.RecruiterId)
                    friendInfo.Status |= FriendStatus.Raf;
            }

            friendInfo.Area = target.Location.Zone;
            friendInfo.Level = target.Level;
            friendInfo.Class = target.Class;
        }
    }

    public PlayerSocial LoadFromDB(SQLResult result, ObjectGuid guid)
    {
        PlayerSocial social = new(_characterDatabase, this);
        social.SetPlayerGUID(guid);

        if (!result.IsEmpty())
            do
            {
                var friendGuid = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(0));
                var friendAccountGuid = ObjectGuid.Create(HighGuid.WowAccount, result.Read<uint>(1));
                var flags = (SocialFlag)result.Read<byte>(2);

                social.PlayerSocialMap[friendGuid] = new FriendInfo(friendAccountGuid, flags, result.Read<string>(3));

                if (flags.HasFlag(SocialFlag.Ignored))
                    social.IgnoredAccounts.Add(friendAccountGuid);
            } while (result.NextRow());

        _socialMap[guid] = social;

        return social;
    }

    public void RemovePlayerSocial(ObjectGuid guid)
    {
        _socialMap.Remove(guid);
    }

    public void SendFriendStatus(Player player, FriendsResult result, ObjectGuid friendGuid, bool broadcast = false)
    {
        FriendInfo fi = new();
        GetFriendInfo(player, friendGuid, fi);

        FriendStatusPkt friendStatus = new();
        friendStatus.Initialize(friendGuid, result, fi);

        if (broadcast)
            BroadcastToFriendListers(player, friendStatus);
        else
            player.SendPacket(friendStatus);
    }

    private void BroadcastToFriendListers(Player player, ServerPacket packet)
    {
        if (player == null)
            return;

        var gmSecLevel = (AccountTypes)_configuration.GetDefaultValue("GM:InWhoList:Level", (int)AccountTypes.Administrator);

        foreach (var pair in _socialMap)
        {
            var info = pair.Value.PlayerSocialMap.LookupByKey(player.GUID);

            if (info != null && info.Flags.HasAnyFlag(SocialFlag.Friend))
            {
                var target = _objectAccessor.FindPlayer(pair.Key);

                if (target == null || !target.Location.IsInWorld)
                    continue;

                var session = target.Session;

                if (!session.HasPermission(RBACPermissions.WhoSeeAllSecLevels) && player.Session.Security > gmSecLevel)
                    continue;

                if (target.Team != player.Team && !session.HasPermission(RBACPermissions.TwoSideWhoList))
                    continue;

                if (player.IsVisibleGloballyFor(target))
                    session.SendPacket(packet);
            }
        }
    }
}
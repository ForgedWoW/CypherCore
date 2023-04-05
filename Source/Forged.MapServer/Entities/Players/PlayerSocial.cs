// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Social;
using Framework.Database;

namespace Forged.MapServer.Entities.Players;

public class PlayerSocial
{
    public List<ObjectGuid> IgnoredAccounts = new();
    public Dictionary<ObjectGuid, FriendInfo> PlayerSocialMap = new();
    private ObjectGuid _mPlayerGUID;

    public bool AddToSocialList(ObjectGuid friendGuid, ObjectGuid accountGuid, SocialFlag flag)
    {
        // check client limits
        if (GetNumberOfSocialsWithFlag(flag) >= (((flag & SocialFlag.Friend) != 0) ? SocialManager.FRIEND_LIMIT_MAX : SocialManager.IGNORE_LIMIT))
            return false;

        var friendInfo = PlayerSocialMap.LookupByKey(friendGuid);

        if (friendInfo != null)
        {
            friendInfo.Flags |= flag;
            friendInfo.WowAccountGuid = accountGuid;

            var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_CHARACTER_SOCIAL_FLAGS);
            stmt.AddValue(0, (byte)friendInfo.Flags);
            stmt.AddValue(1, GetPlayerGUID().Counter);
            stmt.AddValue(2, friendGuid.Counter);
            CharacterDatabase.Execute(stmt);
        }
        else
        {
            FriendInfo fi = new();
            fi.Flags |= flag;
            fi.WowAccountGuid = accountGuid;
            PlayerSocialMap[friendGuid] = fi;

            var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CHARACTER_SOCIAL);
            stmt.AddValue(0, GetPlayerGUID().Counter);
            stmt.AddValue(1, friendGuid.Counter);
            stmt.AddValue(2, (byte)flag);
            CharacterDatabase.Execute(stmt);
        }

        if (flag.HasFlag(SocialFlag.Ignored))
            IgnoredAccounts.Add(accountGuid);

        return true;
    }

    public bool HasFriend(ObjectGuid friendGuid)
    {
        return _HasContact(friendGuid, SocialFlag.Friend);
    }

    public bool HasIgnore(ObjectGuid ignoreGuid, ObjectGuid ignoreAccountGuid)
    {
        return _HasContact(ignoreGuid, SocialFlag.Ignored) || IgnoredAccounts.Contains(ignoreAccountGuid);
    }

    public void RemoveFromSocialList(ObjectGuid friendGuid, SocialFlag flag)
    {
        var friendInfo = PlayerSocialMap.LookupByKey(friendGuid);

        if (friendInfo == null) // not exist
            return;

        friendInfo.Flags &= ~flag;

        if (friendInfo.Flags == 0)
        {
            var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_SOCIAL);
            stmt.AddValue(0, GetPlayerGUID().Counter);
            stmt.AddValue(1, friendGuid.Counter);
            CharacterDatabase.Execute(stmt);

            var accountGuid = friendInfo.WowAccountGuid;

            PlayerSocialMap.Remove(friendGuid);

            if (flag.HasFlag(SocialFlag.Ignored))
            {
                var otherIgnoreForAccount = PlayerSocialMap.Any(social => social.Value.Flags.HasFlag(SocialFlag.Ignored) && social.Value.WowAccountGuid == accountGuid);

                if (!otherIgnoreForAccount)
                    IgnoredAccounts.Remove(accountGuid);
            }
        }
        else
        {
            var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_CHARACTER_SOCIAL_FLAGS);
            stmt.AddValue(0, (byte)friendInfo.Flags);
            stmt.AddValue(1, GetPlayerGUID().Counter);
            stmt.AddValue(2, friendGuid.Counter);
            CharacterDatabase.Execute(stmt);
        }
    }

    public void SendSocialList(Player player, SocialFlag flags)
    {
        if (!player)
            return;

        uint friendsCount = 0;
        uint ignoredCount = 0;

        ContactList contactList = new()
        {
            Flags = flags
        };

        foreach (var v in PlayerSocialMap)
        {
            var contactFlags = v.Value.Flags;

            if (!contactFlags.HasAnyFlag(flags))
                continue;

            // Check client limit for friends list
            if (contactFlags.HasFlag(SocialFlag.Friend))
                if (++friendsCount > SocialManager.FRIEND_LIMIT_MAX)
                    continue;

            // Check client limit for ignore list
            if (contactFlags.HasFlag(SocialFlag.Ignored))
                if (++ignoredCount > SocialManager.IGNORE_LIMIT)
                    continue;

            SocialManager.GetFriendInfo(player, v.Key, v.Value);

            contactList.Contacts.Add(new ContactInfo(v.Key, v.Value));
        }

        player.SendPacket(contactList);
    }

    public void SetFriendNote(ObjectGuid friendGuid, string note)
    {
        if (!PlayerSocialMap.ContainsKey(friendGuid)) // not exist
            return;

        var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_CHARACTER_SOCIAL_NOTE);
        stmt.AddValue(0, note);
        stmt.AddValue(1, GetPlayerGUID().Counter);
        stmt.AddValue(2, friendGuid.Counter);
        CharacterDatabase.Execute(stmt);

        PlayerSocialMap[friendGuid].Note = note;
    }
    public void SetPlayerGUID(ObjectGuid guid)
    {
        _mPlayerGUID = guid;
    }

    private bool _HasContact(ObjectGuid guid, SocialFlag flags)
    {
        var friendInfo = PlayerSocialMap.LookupByKey(guid);

        if (friendInfo != null)
            return friendInfo.Flags.HasAnyFlag(flags);

        return false;
    }

    private uint GetNumberOfSocialsWithFlag(SocialFlag flag)
    {
        uint counter = 0;

        foreach (var pair in PlayerSocialMap)
            if (pair.Value.Flags.HasAnyFlag(flag))
                ++counter;

        return counter;
    }
    private ObjectGuid GetPlayerGUID()
    {
        return _mPlayerGUID;
    }
}
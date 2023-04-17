// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Query;

public class PlayerGuidLookupData
{
    public ObjectGuid AccountID;
    public ObjectGuid BnetAccountID;
    public PlayerClass ClassID = PlayerClass.None;
    public DeclinedName DeclinedNames = new();
    public ObjectGuid GuidActual;
    public ulong GuildClubMemberID;
    public bool IsDeleted;
    public byte Level;
    public string Name = "";
    public Race RaceID = Race.None;

    public Gender Sex = Gender.None;

    public byte Unused915;

    // same as bgs.protocol.club.v1.MemberId.unique_id
    public uint VirtualRealmAddress;

    public bool Initialize(ObjectGuid guid, Player player = null)
    {
        var characterInfo = Global.CharacterCacheStorage.GetCharacterCacheByGuid(guid);

        if (characterInfo == null)
            return false;

        if (player)
        {
            AccountID = player.Session.AccountGUID;
            BnetAccountID = player.Session.BattlenetAccountGUID;
            Name = player.GetName();
            RaceID = player.Race;
            Sex = player.NativeGender;
            ClassID = player.Class;
            Level = (byte)player.Level;

            var names = player.DeclinedNames;

            if (names != null)
                DeclinedNames = names;
        }
        else
        {
            var accountId = Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(guid);
            var bnetAccountId = Global.BNetAccountMgr.GetIdByGameAccount(accountId);

            AccountID = ObjectGuid.Create(HighGuid.WowAccount, accountId);
            BnetAccountID = ObjectGuid.Create(HighGuid.BNetAccount, bnetAccountId);
            Name = characterInfo.Name;
            RaceID = characterInfo.RaceId;
            Sex = characterInfo.Sex;
            ClassID = characterInfo.ClassId;
            Level = characterInfo.Level;
        }

        IsDeleted = characterInfo.IsDeleted;
        GuidActual = guid;
        VirtualRealmAddress = WorldManager.Realm.Id.VirtualRealmAddress;

        return true;
    }

    public void Write(WorldPacket data)
    {
        data.WriteBit(IsDeleted);
        data.WriteBits(Name.GetByteCount(), 6);

        for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
            data.WriteBits(DeclinedNames.Name[i].GetByteCount(), 7);

        data.FlushBits();

        for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
            data.WriteString(DeclinedNames.Name[i]);

        data.WritePackedGuid(AccountID);
        data.WritePackedGuid(BnetAccountID);
        data.WritePackedGuid(GuidActual);
        data.WriteUInt64(GuildClubMemberID);
        data.WriteUInt32(VirtualRealmAddress);
        data.WriteUInt8((byte)RaceID);
        data.WriteUInt8((byte)Sex);
        data.WriteUInt8((byte)ClassID);
        data.WriteUInt8(Level);
        data.WriteUInt8(Unused915);
        data.WriteString(Name);
    }
}
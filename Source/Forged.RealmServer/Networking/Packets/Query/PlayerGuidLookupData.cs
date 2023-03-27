// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class PlayerGuidLookupData
{
	public bool IsDeleted;
	public ObjectGuid AccountID;
	public ObjectGuid BnetAccountID;
	public ObjectGuid GuidActual;
	public string Name = "";
	public ulong GuildClubMemberID; // same as bgs.protocol.club.v1.MemberId.unique_id
	public uint VirtualRealmAddress;
	public Race RaceID = Race.None;
	public Gender Sex = Gender.None;
	public PlayerClass ClassID = PlayerClass.None;
	public byte Level;
	public byte Unused915;
	public DeclinedName DeclinedNames = new();

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
		VirtualRealmAddress = _worldManager.VirtualRealmAddress;

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
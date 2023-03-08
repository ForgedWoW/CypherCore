﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Framework.Database;
using Framework.IO;
using Game.Entities;

namespace Game.Networking.Packets;

public class EnumCharacters : ClientPacket
{
	public EnumCharacters(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

public class EnumCharactersResult : ServerPacket
{
	public bool Success;
	public bool IsDeletedCharacters;           // used for character undelete list
	public bool IsNewPlayerRestrictionSkipped; // allows client to skip new player restrictions
	public bool IsNewPlayerRestricted;         // forbids using level boost and class trials
	public bool IsNewPlayer;                   // forbids hero classes and allied races
	public bool IsTrialAccountRestricted;
	public bool IsAlliedRacesCreationAllowed;

	public int MaxCharacterLevel = 1;
	public uint? DisabledClassesMask = new();

	public List<CharacterInfo> Characters = new();  // all characters on the list
	public List<RaceUnlock> RaceUnlockData = new(); //
	public List<UnlockedConditionalAppearance> UnlockedConditionalAppearances = new();
	public List<RaceLimitDisableInfo> RaceLimitDisables = new();
	public EnumCharactersResult() : base(ServerOpcodes.EnumCharactersResult) { }

	public override void Write()
	{
		_worldPacket.WriteBit(Success);
		_worldPacket.WriteBit(IsDeletedCharacters);
		_worldPacket.WriteBit(IsNewPlayerRestrictionSkipped);
		_worldPacket.WriteBit(IsNewPlayerRestricted);
		_worldPacket.WriteBit(IsNewPlayer);
		_worldPacket.WriteBit(IsTrialAccountRestricted);
		_worldPacket.WriteBit(DisabledClassesMask.HasValue);
		_worldPacket.WriteBit(IsAlliedRacesCreationAllowed);
		_worldPacket.WriteInt32(Characters.Count);
		_worldPacket.WriteInt32(MaxCharacterLevel);
		_worldPacket.WriteInt32(RaceUnlockData.Count);
		_worldPacket.WriteInt32(UnlockedConditionalAppearances.Count);
		_worldPacket.WriteInt32(RaceLimitDisables.Count);

		if (DisabledClassesMask.HasValue)
			_worldPacket.WriteUInt32(DisabledClassesMask.Value);

		foreach (var unlockedConditionalAppearance in UnlockedConditionalAppearances)
			unlockedConditionalAppearance.Write(_worldPacket);

		foreach (var raceLimitDisableInfo in RaceLimitDisables)
			raceLimitDisableInfo.Write(_worldPacket);

		foreach (var charInfo in Characters)
			charInfo.Write(_worldPacket);

		foreach (var raceUnlock in RaceUnlockData)
			raceUnlock.Write(_worldPacket);
	}

	public class CharacterInfo
	{
		public ObjectGuid Guid;
		public ulong GuildClubMemberID; // same as bgs.protocol.club.v1.MemberId.unique_id, guessed basing on SMSG_QUERY_PLAYER_NAME_RESPONSE (that one is known)
		public string Name;
		public byte ListPosition; // Order of the characters in list
		public byte RaceId;
		public Class ClassId;
		public byte SexId;
		public Array<ChrCustomizationChoice> Customizations = new(72);
		public byte ExperienceLevel;
		public uint ZoneId;
		public uint MapId;
		public Vector3 PreloadPos;
		public ObjectGuid GuildGuid;
		public CharacterFlags Flags;           // Character flag @see enum CharacterFlags
		public CharacterCustomizeFlags Flags2; // Character customization flags @see enum CharacterCustomizeFlags
		public uint Flags3;                    // Character flags 3 @todo research
		public uint Flags4;
		public bool FirstLogin;
		public byte unkWod61x;
		public long LastPlayedTime;
		public short SpecID;
		public int Unknown703;
		public int LastLoginVersion;
		public uint OverrideSelectScreenFileDataID;
		public uint PetCreatureDisplayId;
		public uint PetExperienceLevel;
		public uint PetCreatureFamilyId;
		public bool BoostInProgress;               // @todo
		public uint[] ProfessionIds = new uint[2]; // @todo
		public VisualItemInfo[] VisualItems = new VisualItemInfo[InventorySlots.ReagentBagEnd];
		public List<string> MailSenders = new();
		public List<uint> MailSenderTypes = new();

		public CharacterInfo(SQLFields fields)
		{
			Guid = ObjectGuid.Create(HighGuid.Player, fields.Read<ulong>(0));
			Name = fields.Read<string>(1);
			RaceId = fields.Read<byte>(2);
			ClassId = (Class)fields.Read<byte>(3);
			SexId = fields.Read<byte>(4);
			ExperienceLevel = fields.Read<byte>(5);
			ZoneId = fields.Read<uint>(6);
			MapId = fields.Read<uint>(7);
			PreloadPos = new Vector3(fields.Read<float>(8), fields.Read<float>(9), fields.Read<float>(10));

			var guildId = fields.Read<ulong>(11);

			if (guildId != 0)
				GuildGuid = ObjectGuid.Create(HighGuid.Guild, guildId);

			var playerFlags = (PlayerFlags)fields.Read<uint>(12);
			var atLoginFlags = (AtLoginFlags)fields.Read<ushort>(13);

			if (atLoginFlags.HasAnyFlag(AtLoginFlags.Resurrect))
				playerFlags &= ~PlayerFlags.Ghost;

			if (playerFlags.HasAnyFlag(PlayerFlags.Ghost))
				Flags |= CharacterFlags.Ghost;

			if (atLoginFlags.HasAnyFlag(AtLoginFlags.Rename))
				Flags |= CharacterFlags.Rename;

			if (fields.Read<uint>(18) != 0)
				Flags |= CharacterFlags.LockedByBilling;

			if (WorldConfig.GetBoolValue(WorldCfg.DeclinedNamesUsed) && !string.IsNullOrEmpty(fields.Read<string>(23)))
				Flags |= CharacterFlags.Declined;

			if (atLoginFlags.HasAnyFlag(AtLoginFlags.Customize))
				Flags2 = CharacterCustomizeFlags.Customize;
			else if (atLoginFlags.HasAnyFlag(AtLoginFlags.ChangeFaction))
				Flags2 = CharacterCustomizeFlags.Faction;
			else if (atLoginFlags.HasAnyFlag(AtLoginFlags.ChangeRace))
				Flags2 = CharacterCustomizeFlags.Race;

			Flags3 = 0;
			Flags4 = 0;
			FirstLogin = atLoginFlags.HasAnyFlag(AtLoginFlags.FirstLogin);

			// show pet at selection character in character list only for non-ghost character
			if (!playerFlags.HasAnyFlag(PlayerFlags.Ghost) && (ClassId == Class.Warlock || ClassId == Class.Hunter || ClassId == Class.Deathknight))
			{
				var creatureInfo = Global.ObjectMgr.GetCreatureTemplate(fields.Read<uint>(14));

				if (creatureInfo != null)
				{
					PetCreatureDisplayId = fields.Read<uint>(15);
					PetExperienceLevel = fields.Read<ushort>(16);
					PetCreatureFamilyId = (uint)creatureInfo.Family;
				}
			}

			BoostInProgress = false;
			ProfessionIds[0] = 0;
			ProfessionIds[1] = 0;

			StringArguments equipment = new(fields.Read<string>(17));
			ListPosition = fields.Read<byte>(19);
			LastPlayedTime = fields.Read<long>(20);

			var spec = Global.DB2Mgr.GetChrSpecializationByIndex(ClassId, fields.Read<byte>(21));

			if (spec != null)
				SpecID = (short)spec.Id;

			LastLoginVersion = fields.Read<int>(22);

			for (byte slot = 0; slot < InventorySlots.ReagentBagEnd; ++slot)
			{
				VisualItems[slot].InvType = (byte)equipment.NextUInt32();
				VisualItems[slot].DisplayId = equipment.NextUInt32();
				VisualItems[slot].DisplayEnchantId = equipment.NextUInt32();
				VisualItems[slot].Subclass = (byte)equipment.NextUInt32();
				VisualItems[slot].SecondaryItemModifiedAppearanceID = equipment.NextUInt32();
			}
		}

		public void Write(WorldPacket data)
		{
			data.WritePackedGuid(Guid);
			data.WriteUInt64(GuildClubMemberID);
			data.WriteUInt8(ListPosition);
			data.WriteUInt8(RaceId);
			data.WriteUInt8((byte)ClassId);
			data.WriteUInt8(SexId);
			data.WriteInt32(Customizations.Count);

			data.WriteUInt8(ExperienceLevel);
			data.WriteUInt32(ZoneId);
			data.WriteUInt32(MapId);
			data.WriteVector3(PreloadPos);
			data.WritePackedGuid(GuildGuid);
			data.WriteUInt32((uint)Flags);
			data.WriteUInt32((uint)Flags2);
			data.WriteUInt32(Flags3);
			data.WriteUInt32(PetCreatureDisplayId);
			data.WriteUInt32(PetExperienceLevel);
			data.WriteUInt32(PetCreatureFamilyId);

			data.WriteUInt32(ProfessionIds[0]);
			data.WriteUInt32(ProfessionIds[1]);

			foreach (var visualItem in VisualItems)
				visualItem.Write(data);

			data.WriteInt64(LastPlayedTime);
			data.WriteInt16(SpecID);
			data.WriteInt32(Unknown703);
			data.WriteInt32(LastLoginVersion);
			data.WriteUInt32(Flags4);
			data.WriteInt32(MailSenders.Count);
			data.WriteInt32(MailSenderTypes.Count);
			data.WriteUInt32(OverrideSelectScreenFileDataID);

			foreach (var customization in Customizations)
			{
				data.WriteUInt32(customization.ChrCustomizationOptionID);
				data.WriteUInt32(customization.ChrCustomizationChoiceID);
			}

			foreach (var mailSenderType in MailSenderTypes)
				data.WriteUInt32(mailSenderType);

			data.WriteBits(Name.GetByteCount(), 6);
			data.WriteBit(FirstLogin);
			data.WriteBit(BoostInProgress);
			data.WriteBits(unkWod61x, 5);

			foreach (var str in MailSenders)
				data.WriteBits(str.GetByteCount() + 1, 6);

			data.FlushBits();

			foreach (var str in MailSenders)
				if (!str.IsEmpty())
					data.WriteCString(str);

			data.WriteString(Name);
		}

		public struct VisualItemInfo
		{
			public void Write(WorldPacket data)
			{
				data.WriteUInt32(DisplayId);
				data.WriteUInt32(DisplayEnchantId);
				data.WriteUInt32(SecondaryItemModifiedAppearanceID);
				data.WriteUInt8(InvType);
				data.WriteUInt8(Subclass);
			}

			public uint DisplayId;
			public uint DisplayEnchantId;
			public uint SecondaryItemModifiedAppearanceID; // also -1 is some special value
			public byte InvType;
			public byte Subclass;
		}

		public struct PetInfo
		{
			public uint CreatureDisplayId; // PetCreatureDisplayID
			public uint Level;             // PetExperienceLevel
			public uint CreatureFamily;    // PetCreatureFamilyID
		}
	}

	public struct RaceUnlock
	{
		public void Write(WorldPacket data)
		{
			data.WriteInt32(RaceID);
			data.WriteBit(HasExpansion);
			data.WriteBit(HasAchievement);
			data.WriteBit(HasHeritageArmor);
			data.FlushBits();
		}

		public int RaceID;
		public bool HasExpansion;
		public bool HasAchievement;
		public bool HasHeritageArmor;
	}

	public struct UnlockedConditionalAppearance
	{
		public void Write(WorldPacket data)
		{
			data.WriteInt32(AchievementID);
			data.WriteInt32(Unused);
		}

		public int AchievementID;
		public int Unused;
	}

	public struct RaceLimitDisableInfo
	{
		enum blah
		{
			Server,
			Level
		}

		public int RaceID;
		public int BlockReason;

		public void Write(WorldPacket data)
		{
			data.WriteInt32(RaceID);
			data.WriteInt32(BlockReason);
		}
	}
}

class CheckCharacterNameAvailability : ClientPacket
{
	public uint SequenceIndex;
	public string Name;

	public CheckCharacterNameAvailability(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		SequenceIndex = _worldPacket.ReadUInt32();
		Name = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(6));
	}
}

class CheckCharacterNameAvailabilityResult : ServerPacket
{
	public uint SequenceIndex;
	public ResponseCodes Result;

	public CheckCharacterNameAvailabilityResult(uint sequenceIndex, ResponseCodes result) : base(ServerOpcodes.CheckCharacterNameAvailabilityResult)
	{
		SequenceIndex = sequenceIndex;
		Result = result;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32(SequenceIndex);
		_worldPacket.WriteUInt32((uint)Result);
	}
}

public class CreateCharacter : ClientPacket
{
	public CharacterCreateInfo CreateInfo;
	public CreateCharacter(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		CreateInfo = new CharacterCreateInfo();
		var nameLength = _worldPacket.ReadBits<uint>(6);
		var hasTemplateSet = _worldPacket.HasBit();
		CreateInfo.IsTrialBoost = _worldPacket.HasBit();
		CreateInfo.UseNPE = _worldPacket.HasBit();

		CreateInfo.RaceId = (Race)_worldPacket.ReadUInt8();
		CreateInfo.ClassId = (Class)_worldPacket.ReadUInt8();
		CreateInfo.Sex = (Gender)_worldPacket.ReadUInt8();
		var customizationCount = _worldPacket.ReadUInt32();

		CreateInfo.Name = _worldPacket.ReadString(nameLength);

		if (CreateInfo.TemplateSet.HasValue)
			CreateInfo.TemplateSet = _worldPacket.ReadUInt32();

		for (var i = 0; i < customizationCount; ++i)
			CreateInfo.Customizations[i] = new ChrCustomizationChoice()
			{
				ChrCustomizationOptionID = _worldPacket.ReadUInt32(),
				ChrCustomizationChoiceID = _worldPacket.ReadUInt32()
			};

		CreateInfo.Customizations.Sort();
	}
}

public class CreateChar : ServerPacket
{
	public ResponseCodes Code;
	public ObjectGuid Guid;
	public CreateChar() : base(ServerOpcodes.CreateChar) { }

	public override void Write()
	{
		_worldPacket.WriteUInt8((byte)Code);
		_worldPacket.WritePackedGuid(Guid);
	}
}

public class CharDelete : ClientPacket
{
	public ObjectGuid Guid; // Guid of the character to delete
	public CharDelete(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Guid = _worldPacket.ReadPackedGuid();
	}
}

public class DeleteChar : ServerPacket
{
	public ResponseCodes Code;
	public DeleteChar() : base(ServerOpcodes.DeleteChar) { }

	public override void Write()
	{
		_worldPacket.WriteUInt8((byte)Code);
	}
}

public class CharacterRenameRequest : ClientPacket
{
	public CharacterRenameInfo RenameInfo;
	public CharacterRenameRequest(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		RenameInfo = new CharacterRenameInfo();
		RenameInfo.Guid = _worldPacket.ReadPackedGuid();
		RenameInfo.NewName = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(6));
	}
}

public class CharacterRenameResult : ServerPacket
{
	public string Name;
	public ResponseCodes Result;
	public ObjectGuid? Guid;
	public CharacterRenameResult() : base(ServerOpcodes.CharacterRenameResult) { }

	public override void Write()
	{
		_worldPacket.WriteUInt8((byte)Result);
		_worldPacket.WriteBit(Guid.HasValue);
		_worldPacket.WriteBits(Name.GetByteCount(), 6);
		_worldPacket.FlushBits();

		if (Guid.HasValue)
			_worldPacket.WritePackedGuid(Guid.Value);

		_worldPacket.WriteString(Name);
	}
}

public class CharCustomize : ClientPacket
{
	public CharCustomizeInfo CustomizeInfo;
	public CharCustomize(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		CustomizeInfo = new CharCustomizeInfo();
		CustomizeInfo.CharGUID = _worldPacket.ReadPackedGuid();
		CustomizeInfo.SexID = (Gender)_worldPacket.ReadUInt8();
		var customizationCount = _worldPacket.ReadUInt32();

		for (var i = 0; i < customizationCount; ++i)
			CustomizeInfo.Customizations[i] = new ChrCustomizationChoice()
			{
				ChrCustomizationOptionID = _worldPacket.ReadUInt32(),
				ChrCustomizationChoiceID = _worldPacket.ReadUInt32()
			};

		CustomizeInfo.Customizations.Sort();

		CustomizeInfo.CharName = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(6));
	}
}

// @todo: CharCustomizeResult

public class CharRaceOrFactionChange : ClientPacket
{
	public CharRaceOrFactionChangeInfo RaceOrFactionChangeInfo;
	public CharRaceOrFactionChange(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		RaceOrFactionChangeInfo = new CharRaceOrFactionChangeInfo();

		RaceOrFactionChangeInfo.FactionChange = _worldPacket.HasBit();

		var nameLength = _worldPacket.ReadBits<uint>(6);

		RaceOrFactionChangeInfo.Guid = _worldPacket.ReadPackedGuid();
		RaceOrFactionChangeInfo.SexID = (Gender)_worldPacket.ReadUInt8();
		RaceOrFactionChangeInfo.RaceID = (Race)_worldPacket.ReadUInt8();
		RaceOrFactionChangeInfo.InitialRaceID = (Race)_worldPacket.ReadUInt8();
		var customizationCount = _worldPacket.ReadUInt32();
		RaceOrFactionChangeInfo.Name = _worldPacket.ReadString(nameLength);

		for (var i = 0; i < customizationCount; ++i)
			RaceOrFactionChangeInfo.Customizations[i] = new ChrCustomizationChoice()
			{
				ChrCustomizationOptionID = _worldPacket.ReadUInt32(),
				ChrCustomizationChoiceID = _worldPacket.ReadUInt32()
			};

		RaceOrFactionChangeInfo.Customizations.Sort();
	}
}

public class CharFactionChangeResult : ServerPacket
{
	public ResponseCodes Result = 0;
	public ObjectGuid Guid;
	public CharFactionChangeDisplayInfo Display;
	public CharFactionChangeResult() : base(ServerOpcodes.CharFactionChangeResult) { }

	public override void Write()
	{
		_worldPacket.WriteUInt8((byte)Result);
		_worldPacket.WritePackedGuid(Guid);
		_worldPacket.WriteBit(Display != null);
		_worldPacket.FlushBits();

		if (Display != null)
		{
			_worldPacket.WriteBits(Display.Name.GetByteCount(), 6);
			_worldPacket.WriteUInt8(Display.SexID);
			_worldPacket.WriteUInt8(Display.RaceID);
			_worldPacket.WriteInt32(Display.Customizations.Count);
			_worldPacket.WriteString(Display.Name);

			foreach (var customization in Display.Customizations)
			{
				_worldPacket.WriteUInt32(customization.ChrCustomizationOptionID);
				_worldPacket.WriteUInt32(customization.ChrCustomizationChoiceID);
			}
		}
	}

	public class CharFactionChangeDisplayInfo
	{
		public string Name;
		public byte SexID;
		public byte RaceID;
		public Array<ChrCustomizationChoice> Customizations = new(72);
	}
}

public class GenerateRandomCharacterName : ClientPacket
{
	public byte Sex;
	public byte Race;
	public GenerateRandomCharacterName(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Race = _worldPacket.ReadUInt8();
		Sex = _worldPacket.ReadUInt8();
	}
}

public class GenerateRandomCharacterNameResult : ServerPacket
{
	public string Name;
	public bool Success;
	public GenerateRandomCharacterNameResult() : base(ServerOpcodes.GenerateRandomCharacterNameResult) { }

	public override void Write()
	{
		_worldPacket.WriteBit(Success);
		_worldPacket.WriteBits(Name.GetByteCount(), 6);

		_worldPacket.WriteString(Name);
	}
}

public class ReorderCharacters : ClientPacket
{
	public ReorderInfo[] Entries = new ReorderInfo[200];
	public ReorderCharacters(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var count = _worldPacket.ReadBits<uint>(9);

		for (var i = 0; i < count && i < WorldConfig.GetIntValue(WorldCfg.CharactersPerRealm); ++i)
		{
			ReorderInfo reorderInfo;
			reorderInfo.PlayerGUID = _worldPacket.ReadPackedGuid();
			reorderInfo.NewPosition = _worldPacket.ReadUInt8();
			Entries[i] = reorderInfo;
		}
	}

	public struct ReorderInfo
	{
		public ObjectGuid PlayerGUID;
		public byte NewPosition;
	}
}

public class UndeleteCharacter : ClientPacket
{
	public CharacterUndeleteInfo UndeleteInfo;
	public UndeleteCharacter(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		UndeleteInfo = new CharacterUndeleteInfo();
		_worldPacket.WriteInt32(UndeleteInfo.ClientToken);
		_worldPacket.WritePackedGuid(UndeleteInfo.CharacterGuid);
	}
}

public class UndeleteCharacterResponse : ServerPacket
{
	public CharacterUndeleteInfo UndeleteInfo;
	public CharacterUndeleteResult Result;
	public UndeleteCharacterResponse() : base(ServerOpcodes.UndeleteCharacterResponse) { }

	public override void Write()
	{
		Cypher.Assert(UndeleteInfo != null);
		_worldPacket.WriteInt32(UndeleteInfo.ClientToken);
		_worldPacket.WriteUInt32((uint)Result);
		_worldPacket.WritePackedGuid(UndeleteInfo.CharacterGuid);
	}
}

public class GetUndeleteCharacterCooldownStatus : ClientPacket
{
	public GetUndeleteCharacterCooldownStatus(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

public class UndeleteCooldownStatusResponse : ServerPacket
{
	public bool OnCooldown;      //
	public uint MaxCooldown;     // Max. cooldown until next free character restoration. Displayed in undelete confirm message. (in sec)
	public uint CurrentCooldown; // Current cooldown until next free character restoration. (in sec)
	public UndeleteCooldownStatusResponse() : base(ServerOpcodes.UndeleteCooldownStatusResponse) { }

	public override void Write()
	{
		_worldPacket.WriteBit(OnCooldown);
		_worldPacket.WriteUInt32(MaxCooldown);
		_worldPacket.WriteUInt32(CurrentCooldown);
	}
}

public class PlayerLogin : ClientPacket
{
	public ObjectGuid Guid; // Guid of the player that is logging in
	public float FarClip;   // Visibility distance (for terrain)
	public PlayerLogin(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Guid = _worldPacket.ReadPackedGuid();
		FarClip = _worldPacket.ReadFloat();
	}
}

public class LoginVerifyWorld : ServerPacket
{
	public int MapID = -1;
	public Position Pos;
	public uint Reason = 0;
	public LoginVerifyWorld() : base(ServerOpcodes.LoginVerifyWorld, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(MapID);
		_worldPacket.WriteFloat(Pos.X);
		_worldPacket.WriteFloat(Pos.Y);
		_worldPacket.WriteFloat(Pos.Z);
		_worldPacket.WriteFloat(Pos.Orientation);
		_worldPacket.WriteUInt32(Reason);
	}
}

public class CharacterLoginFailed : ServerPacket
{
	readonly LoginFailureReason Code;

	public CharacterLoginFailed(LoginFailureReason code) : base(ServerOpcodes.CharacterLoginFailed)
	{
		Code = code;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt8((byte)Code);
	}
}

public class LogoutRequest : ClientPacket
{
	public bool IdleLogout;
	public LogoutRequest(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		IdleLogout = _worldPacket.HasBit();
	}
}

public class LogoutResponse : ServerPacket
{
	public int LogoutResult;
	public bool Instant = false;
	public LogoutResponse() : base(ServerOpcodes.LogoutResponse, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(LogoutResult);
		_worldPacket.WriteBit(Instant);
		_worldPacket.FlushBits();
	}
}

public class LogoutComplete : ServerPacket
{
	public LogoutComplete() : base(ServerOpcodes.LogoutComplete) { }

	public override void Write() { }
}

public class LogoutCancel : ClientPacket
{
	public LogoutCancel(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

public class LogoutCancelAck : ServerPacket
{
	public LogoutCancelAck() : base(ServerOpcodes.LogoutCancelAck, ConnectionType.Instance) { }

	public override void Write() { }
}

public class LoadingScreenNotify : ClientPacket
{
	public int MapID = -1;
	public bool Showing;
	public LoadingScreenNotify(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		MapID = _worldPacket.ReadInt32();
		Showing = _worldPacket.HasBit();
	}
}

public class InitialSetup : ServerPacket
{
	public byte ServerExpansionTier;
	public byte ServerExpansionLevel;
	public InitialSetup() : base(ServerOpcodes.InitialSetup, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt8(ServerExpansionLevel);
		_worldPacket.WriteUInt8(ServerExpansionTier);
	}
}

public class SetActionBarToggles : ClientPacket
{
	public byte Mask;
	public SetActionBarToggles(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Mask = _worldPacket.ReadUInt8();
	}
}

public class RequestPlayedTime : ClientPacket
{
	public bool TriggerScriptEvent;
	public RequestPlayedTime(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		TriggerScriptEvent = _worldPacket.HasBit();
	}
}

public class PlayedTime : ServerPacket
{
	public uint TotalTime;
	public uint LevelTime;
	public bool TriggerEvent;
	public PlayedTime() : base(ServerOpcodes.PlayedTime, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(TotalTime);
		_worldPacket.WriteUInt32(LevelTime);
		_worldPacket.WriteBit(TriggerEvent);
		_worldPacket.FlushBits();
	}
}

public class SetTitle : ClientPacket
{
	public int TitleID;

	public SetTitle(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		TitleID = _worldPacket.ReadInt32();
	}
}

public class AlterApperance : ClientPacket
{
	public byte NewSex;
	public Array<ChrCustomizationChoice> Customizations = new(72);
	public int CustomizedRace;
	public AlterApperance(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var customizationCount = _worldPacket.ReadUInt32();
		NewSex = _worldPacket.ReadUInt8();
		CustomizedRace = _worldPacket.ReadInt32();

		for (var i = 0; i < customizationCount; ++i)
			Customizations[i] = new ChrCustomizationChoice()
			{
				ChrCustomizationOptionID = _worldPacket.ReadUInt32(),
				ChrCustomizationChoiceID = _worldPacket.ReadUInt32()
			};

		Customizations.Sort();
	}
}

public class BarberShopResult : ServerPacket
{
	public enum ResultEnum
	{
		Success = 0,
		NoMoney = 1,
		NotOnChair = 2,
		NoMoney2 = 3
	}

	public ResultEnum Result;

	public BarberShopResult(ResultEnum result) : base(ServerOpcodes.BarberShopResult)
	{
		Result = result;
	}

	public override void Write()
	{
		_worldPacket.WriteInt32((int)Result);
	}
}

class LogXPGain : ServerPacket
{
	public ObjectGuid Victim;
	public int Original;
	public PlayerLogXPReason Reason;
	public int Amount;
	public float GroupBonus;
	public LogXPGain() : base(ServerOpcodes.LogXpGain) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Victim);
		_worldPacket.WriteInt32(Original);
		_worldPacket.WriteUInt8((byte)Reason);
		_worldPacket.WriteInt32(Amount);
		_worldPacket.WriteFloat(GroupBonus);
	}
}

class TitleEarned : ServerPacket
{
	public uint Index;
	public TitleEarned(ServerOpcodes opcode) : base(opcode) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(Index);
	}
}

class SetFactionAtWar : ClientPacket
{
	public byte FactionIndex;
	public SetFactionAtWar(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		FactionIndex = _worldPacket.ReadUInt8();
	}
}

class SetFactionNotAtWar : ClientPacket
{
	public byte FactionIndex;
	public SetFactionNotAtWar(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		FactionIndex = _worldPacket.ReadUInt8();
	}
}

class SetFactionInactive : ClientPacket
{
	public uint Index;
	public bool State;
	public SetFactionInactive(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Index = _worldPacket.ReadUInt32();
		State = _worldPacket.HasBit();
	}
}

class SetWatchedFaction : ClientPacket
{
	public uint FactionIndex;
	public SetWatchedFaction(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		FactionIndex = _worldPacket.ReadUInt32();
	}
}

class SetFactionVisible : ServerPacket
{
	public uint FactionIndex;
	public SetFactionVisible(bool visible) : base(visible ? ServerOpcodes.SetFactionVisible : ServerOpcodes.SetFactionNotVisible, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(FactionIndex);
	}
}

class CharCustomizeSuccess : ServerPacket
{
	readonly string CharName = "";
	readonly byte SexID;
	readonly Array<ChrCustomizationChoice> Customizations = new(72);

	readonly ObjectGuid CharGUID;

	public CharCustomizeSuccess(CharCustomizeInfo customizeInfo) : base(ServerOpcodes.CharCustomizeSuccess)
	{
		CharGUID = customizeInfo.CharGUID;
		SexID = (byte)customizeInfo.SexID;
		CharName = customizeInfo.CharName;
		Customizations = customizeInfo.Customizations;
	}

	public override void Write()
	{
		_worldPacket.WritePackedGuid(CharGUID);
		_worldPacket.WriteUInt8(SexID);
		_worldPacket.WriteInt32(Customizations.Count);

		foreach (var customization in Customizations)
		{
			_worldPacket.WriteUInt32(customization.ChrCustomizationOptionID);
			_worldPacket.WriteUInt32(customization.ChrCustomizationChoiceID);
		}

		_worldPacket.WriteBits(CharName.GetByteCount(), 6);
		_worldPacket.FlushBits();
		_worldPacket.WriteString(CharName);
	}
}

class CharCustomizeFailure : ServerPacket
{
	public byte Result;
	public ObjectGuid CharGUID;
	public CharCustomizeFailure() : base(ServerOpcodes.CharCustomizeFailure) { }

	public override void Write()
	{
		_worldPacket.WriteUInt8(Result);
		_worldPacket.WritePackedGuid(CharGUID);
	}
}

class SetPlayerDeclinedNames : ClientPacket
{
	public ObjectGuid Player;
	public DeclinedName DeclinedNames;

	public SetPlayerDeclinedNames(WorldPacket packet) : base(packet)
	{
		DeclinedNames = new DeclinedName();
	}

	public override void Read()
	{
		Player = _worldPacket.ReadPackedGuid();

		var stringLengths = new byte[SharedConst.MaxDeclinedNameCases];

		for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
			stringLengths[i] = _worldPacket.ReadBits<byte>(7);

		for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
			DeclinedNames.Name[i] = _worldPacket.ReadString(stringLengths[i]);
	}
}

class SetPlayerDeclinedNamesResult : ServerPacket
{
	public ObjectGuid Player;
	public DeclinedNameResult ResultCode;
	public SetPlayerDeclinedNamesResult() : base(ServerOpcodes.SetPlayerDeclinedNamesResult) { }

	public override void Write()
	{
		_worldPacket.WriteInt32((int)ResultCode);
		_worldPacket.WritePackedGuid(Player);
	}
}

//Structs
public class CharacterCreateInfo
{
	// User specified variables
	public Race RaceId = Race.None;
	public Class ClassId = Class.None;
	public Gender Sex = Gender.None;
	public Array<ChrCustomizationChoice> Customizations = new(72);
	public uint? TemplateSet;
	public bool IsTrialBoost;
	public bool UseNPE;
	public string Name;

	// Server side data
	public byte CharCount = 0;
}

public class CharacterRenameInfo
{
	public string NewName;
	public ObjectGuid Guid;
}

public class CharCustomizeInfo
{
	public ObjectGuid CharGUID;
	public Gender SexID = Gender.None;
	public string CharName;
	public Array<ChrCustomizationChoice> Customizations = new(72);
}

public class CharRaceOrFactionChangeInfo
{
	public Race RaceID = Race.None;
	public Race InitialRaceID = Race.None;
	public Gender SexID = Gender.None;
	public ObjectGuid Guid;
	public bool FactionChange;
	public string Name;
	public Array<ChrCustomizationChoice> Customizations = new(72);
}

public class CharacterUndeleteInfo
{
	// User specified variables
	public ObjectGuid CharacterGuid; // Guid of the character to restore
	public int ClientToken = 0;      // @todo: research

	// Server side data
	public string Name;
}
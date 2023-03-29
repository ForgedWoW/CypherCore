// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Framework.Constants;
using Framework.Database;
using Framework.IO;

namespace Forged.MapServer.Networking.Packets.Character;

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
        public PlayerClass ClassId;
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
            ClassId = (PlayerClass)fields.Read<byte>(3);
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

            if (GetDefaultValue("DeclinedNames", false) && !string.IsNullOrEmpty(fields.Read<string>(23)))
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
            if (!playerFlags.HasAnyFlag(PlayerFlags.Ghost) && (ClassId == PlayerClass.Warlock || ClassId == PlayerClass.Hunter || ClassId == PlayerClass.Deathknight))
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
        private enum blah
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
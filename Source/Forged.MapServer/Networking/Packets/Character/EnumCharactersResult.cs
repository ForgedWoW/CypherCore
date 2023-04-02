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
    public List<CharacterInfo> Characters = new();
    public uint? DisabledClassesMask = new();
    public bool IsAlliedRacesCreationAllowed;
    public bool IsDeletedCharacters;
    public bool IsNewPlayer;
    public bool IsNewPlayerRestricted;
    // used for character undelete list
    public bool IsNewPlayerRestrictionSkipped;

    // allows client to skip new player restrictions
    // forbids using level boost and class trials
    // forbids hero classes and allied races
    public bool IsTrialAccountRestricted;

    public int MaxCharacterLevel = 1;
    public List<RaceLimitDisableInfo> RaceLimitDisables = new();
    // all characters on the list
    public List<RaceUnlock> RaceUnlockData = new();

    public bool Success;
    //
    public List<UnlockedConditionalAppearance> UnlockedConditionalAppearances = new();
    public EnumCharactersResult() : base(ServerOpcodes.EnumCharactersResult) { }

    public override void Write()
    {
        WorldPacket.WriteBit(Success);
        WorldPacket.WriteBit(IsDeletedCharacters);
        WorldPacket.WriteBit(IsNewPlayerRestrictionSkipped);
        WorldPacket.WriteBit(IsNewPlayerRestricted);
        WorldPacket.WriteBit(IsNewPlayer);
        WorldPacket.WriteBit(IsTrialAccountRestricted);
        WorldPacket.WriteBit(DisabledClassesMask.HasValue);
        WorldPacket.WriteBit(IsAlliedRacesCreationAllowed);
        WorldPacket.WriteInt32(Characters.Count);
        WorldPacket.WriteInt32(MaxCharacterLevel);
        WorldPacket.WriteInt32(RaceUnlockData.Count);
        WorldPacket.WriteInt32(UnlockedConditionalAppearances.Count);
        WorldPacket.WriteInt32(RaceLimitDisables.Count);

        if (DisabledClassesMask.HasValue)
            WorldPacket.WriteUInt32(DisabledClassesMask.Value);

        foreach (var unlockedConditionalAppearance in UnlockedConditionalAppearances)
            unlockedConditionalAppearance.Write(WorldPacket);

        foreach (var raceLimitDisableInfo in RaceLimitDisables)
            raceLimitDisableInfo.Write(WorldPacket);

        foreach (var charInfo in Characters)
            charInfo.Write(WorldPacket);

        foreach (var raceUnlock in RaceUnlockData)
            raceUnlock.Write(WorldPacket);
    }

    public struct RaceLimitDisableInfo
    {
        public int BlockReason;

        public int RaceID;

        private enum blah
        {
            Server,
            Level
        }
        public void Write(WorldPacket data)
        {
            data.WriteInt32(RaceID);
            data.WriteInt32(BlockReason);
        }
    }

    public struct RaceUnlock
    {
        public bool HasAchievement;

        public bool HasExpansion;

        public bool HasHeritageArmor;

        public int RaceID;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(RaceID);
            data.WriteBit(HasExpansion);
            data.WriteBit(HasAchievement);
            data.WriteBit(HasHeritageArmor);
            data.FlushBits();
        }
    }

    public struct UnlockedConditionalAppearance
    {
        public int AchievementID;

        public int Unused;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(AchievementID);
            data.WriteInt32(Unused);
        }
    }

    public class CharacterInfo
    {
        public bool BoostInProgress;
        public PlayerClass ClassId;
        public Array<ChrCustomizationChoice> Customizations = new(72);
        public byte ExperienceLevel;
        public bool FirstLogin;
        public CharacterFlags Flags;
        // Character flag @see enum CharacterFlags
        public CharacterCustomizeFlags Flags2;

        // Character customization flags @see enum CharacterCustomizeFlags
        public uint Flags3;

        // Character flags 3 @todo research
        public uint Flags4;

        public ObjectGuid Guid;
        public ulong GuildClubMemberID; // same as bgs.protocol.club.v1.MemberId.unique_id, guessed basing on SMSG_QUERY_PLAYER_NAME_RESPONSE (that one is known)
        public ObjectGuid GuildGuid;
        public int LastLoginVersion;
        public long LastPlayedTime;
        public byte ListPosition;
        public List<string> MailSenders = new();
        public List<uint> MailSenderTypes = new();
        public uint MapId;
        public string Name;
        public uint OverrideSelectScreenFileDataID;

        public uint PetCreatureDisplayId;

        public uint PetCreatureFamilyId;

        public uint PetExperienceLevel;

        public Vector3 PreloadPos;

        // @todo
        public uint[] ProfessionIds = new uint[2];

        // Order of the characters in list
        public byte RaceId;
        public byte SexId;
        public short SpecID;
        public int Unknown703;
        public byte unkWod61x;
        // @todo
        public VisualItemInfo[] VisualItems = new VisualItemInfo[InventorySlots.ReagentBagEnd];

        public uint ZoneId;
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

        public struct PetInfo
        {
            public uint CreatureDisplayId; // PetCreatureDisplayID
            public uint CreatureFamily;
            public uint Level;             // PetExperienceLevel
                                           // PetCreatureFamilyID
        }

        public struct VisualItemInfo
        {
            public uint DisplayEnchantId;

            public uint DisplayId;

            public byte InvType;

            public uint SecondaryItemModifiedAppearanceID;

            // also -1 is some special value
            public byte Subclass;

            public void Write(WorldPacket data)
            {
                data.WriteUInt32(DisplayId);
                data.WriteUInt32(DisplayEnchantId);
                data.WriteUInt32(SecondaryItemModifiedAppearanceID);
                data.WriteUInt8(InvType);
                data.WriteUInt8(Subclass);
            }
        }
    }
}
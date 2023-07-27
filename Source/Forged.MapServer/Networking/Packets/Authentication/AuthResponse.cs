// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Globals;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Authentication;

internal class AuthResponse : ServerPacket
{
    public BattlenetRpcErrorCode Result;
    public AuthSuccessInfo SuccessInfo; // contains the packet data in case that it has account information (It is never set when WaitInfo is set), otherwise its contents are undefined.

    public AuthWaitInfo? WaitInfo; // contains the queue wait information in case the account is in the login queue.

    // the result of the authentication process, possible values are @ref BattlenetRpcErrorCode
    public AuthResponse() : base(ServerOpcodes.AuthResponse) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32((uint)Result);
        WorldPacket.WriteBit(SuccessInfo != null);
        WorldPacket.WriteBit(WaitInfo.HasValue);
        WorldPacket.FlushBits();

        if (SuccessInfo != null)
        {
            WorldPacket.WriteUInt32(SuccessInfo.VirtualRealmAddress);
            WorldPacket.WriteInt32(SuccessInfo.VirtualRealms.Count);
            WorldPacket.WriteUInt32(SuccessInfo.TimeRested);
            WorldPacket.WriteUInt8(SuccessInfo.ActiveExpansionLevel);
            WorldPacket.WriteUInt8(SuccessInfo.AccountExpansionLevel);
            WorldPacket.WriteUInt32(SuccessInfo.TimeSecondsUntilPCKick);
            WorldPacket.WriteInt32(SuccessInfo.AvailableClasses.Count);
            WorldPacket.WriteInt32(SuccessInfo.Templates.Count);
            WorldPacket.WriteUInt32(SuccessInfo.CurrencyID);
            WorldPacket.WriteInt64(SuccessInfo.Time);

            foreach (var raceClassAvailability in SuccessInfo.AvailableClasses)
            {
                WorldPacket.WriteUInt8(raceClassAvailability.RaceID);
                WorldPacket.WriteInt32(raceClassAvailability.Classes.Count);

                foreach (var classAvailability in raceClassAvailability.Classes)
                {
                    WorldPacket.WriteUInt8(classAvailability.ClassID);
                    WorldPacket.WriteUInt8(classAvailability.ActiveExpansionLevel);
                    WorldPacket.WriteUInt8(classAvailability.AccountExpansionLevel);
                    WorldPacket.WriteUInt8(classAvailability.MinActiveExpansionLevel);
                }
            }

            WorldPacket.WriteBit(SuccessInfo.IsExpansionTrial);
            WorldPacket.WriteBit(SuccessInfo.ForceCharacterTemplate);
            WorldPacket.WriteBit(SuccessInfo.NumPlayersHorde.HasValue);
            WorldPacket.WriteBit(SuccessInfo.NumPlayersAlliance.HasValue);
            WorldPacket.WriteBit(SuccessInfo.ExpansionTrialExpiration.HasValue);
            WorldPacket.WriteBit(SuccessInfo.NewBuildKeys.HasValue);
            WorldPacket.FlushBits();

            {
                WorldPacket.WriteUInt32(SuccessInfo.GameTimeInfo.BillingPlan);
                WorldPacket.WriteUInt32(SuccessInfo.GameTimeInfo.TimeRemain);
                WorldPacket.WriteUInt32(SuccessInfo.GameTimeInfo.Unknown735);
                // 3x same bit is not a mistake - preserves legacy client behavior of BillingPlanFlags::SESSION_IGR
                WorldPacket.WriteBit(SuccessInfo.GameTimeInfo.InGameRoom); // inGameRoom check in function checking which lua event to fire when remaining time is near end - BILLING_NAG_DIALOG vs IGR_BILLING_NAG_DIALOG
                WorldPacket.WriteBit(SuccessInfo.GameTimeInfo.InGameRoom); // inGameRoom lua return from Script_GetBillingPlan
                WorldPacket.WriteBit(SuccessInfo.GameTimeInfo.InGameRoom); // not used anywhere in the client
                WorldPacket.FlushBits();
            }

            if (SuccessInfo.NumPlayersHorde.HasValue)
                WorldPacket.WriteUInt16(SuccessInfo.NumPlayersHorde.Value);

            if (SuccessInfo.NumPlayersAlliance.HasValue)
                WorldPacket.WriteUInt16(SuccessInfo.NumPlayersAlliance.Value);

            if (SuccessInfo.ExpansionTrialExpiration.HasValue)
                WorldPacket.WriteInt64(SuccessInfo.ExpansionTrialExpiration.Value);

            if (SuccessInfo.NewBuildKeys.HasValue)
            {
                for (int i = 0; i < 16; ++i)
                {
                    WorldPacket.WriteUInt8(SuccessInfo.NewBuildKeys.Value.NewBuildKey[i]);
                    WorldPacket.WriteUInt8(SuccessInfo.NewBuildKeys.Value.SomeKey[i]);
                }
            }
            
            foreach (var virtualRealm in SuccessInfo.VirtualRealms)
                virtualRealm.Write(WorldPacket);

            foreach (var templat in SuccessInfo.Templates)
            {
                WorldPacket.WriteUInt32(templat.TemplateSetId);
                WorldPacket.WriteInt32(templat.Classes.Count);

                foreach (var templateClass in templat.Classes)
                {
                    WorldPacket.WriteUInt8(templateClass.ClassID);
                    WorldPacket.WriteUInt8((byte)templateClass.FactionGroup);
                }

                WorldPacket.WriteBits(templat.Name.GetByteCount(), 7);
                WorldPacket.WriteBits(templat.Description.GetByteCount(), 10);
                WorldPacket.FlushBits();

                WorldPacket.WriteString(templat.Name);
                WorldPacket.WriteString(templat.Description);
            }
        }

        WaitInfo?.Write(WorldPacket);
    }

    public class AuthSuccessInfo
    {
        public byte AccountExpansionLevel;
        public byte ActiveExpansionLevel; // the current server expansion, the possible values are in @ref Expansions
        public List<RaceClassAvailability> AvailableClasses;

        public uint CurrencyID;

        public long? ExpansionTrialExpiration;

        public bool ForceCharacterTemplate;

        public GameTime GameTimeInfo;

        public bool IsExpansionTrial;

        public ushort? NumPlayersAlliance;

        // the minimum AccountExpansion required to select the classes
        // forces the client to always use a character template when creating a new character. @see Templates. @todo implement
        public ushort? NumPlayersHorde;

        public List<CharacterTemplate> Templates = new();

        // this is probably used for the ingame shop. @todo implement
        public long Time;

        // the current expansion of this account, the possible values are in @ref Expansions
        public uint TimeRested; // affects the return value of the GetBillingTimeRested() client API call, it is the number of seconds you have left until the experience points and loot you receive from creatures and quests is reduced. It is only used in the Asia region in retail, it's not implemented in TC and will probably never be.

        public uint TimeSecondsUntilPCKick;

        public uint VirtualRealmAddress; // a special identifier made from the Index, BattleGroup and Region. @todo implement

        public NewBuild? NewBuildKeys;
        // @todo research
        public List<VirtualRealmInfo> VirtualRealms = new(); // list of realms connected to this one (inclusive) @todo implement
        // list of pre-made character templates. @todo implement

        // number of horde players in this realm. @todo implement
        // number of alliance players in this realm. @todo implement
        // expansion trial expiration unix timestamp

        public struct GameTime
        {
            public uint BillingPlan;
            public bool InGameRoom;
            public uint TimeRemain;
            public uint Unknown735;
        }

        public struct NewBuild
        {
            public List<byte> NewBuildKey;
            public List<byte> SomeKey;
        }
    }
}
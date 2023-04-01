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
    public AuthSuccessInfo SuccessInfo;  // contains the packet data in case that it has account information (It is never set when WaitInfo is set), otherwise its contents are undefined.
    public AuthWaitInfo? WaitInfo;       // contains the queue wait information in case the account is in the login queue.
    public BattlenetRpcErrorCode Result; // the result of the authentication process, possible values are @ref BattlenetRpcErrorCode
    public AuthResponse() : base(ServerOpcodes.AuthResponse) { }

    public override void Write()
    {
        _worldPacket.WriteUInt32((uint)Result);
        _worldPacket.WriteBit(SuccessInfo != null);
        _worldPacket.WriteBit(WaitInfo.HasValue);
        _worldPacket.FlushBits();

        if (SuccessInfo != null)
        {
            _worldPacket.WriteUInt32(SuccessInfo.VirtualRealmAddress);
            _worldPacket.WriteInt32(SuccessInfo.VirtualRealms.Count);
            _worldPacket.WriteUInt32(SuccessInfo.TimeRested);
            _worldPacket.WriteUInt8(SuccessInfo.ActiveExpansionLevel);
            _worldPacket.WriteUInt8(SuccessInfo.AccountExpansionLevel);
            _worldPacket.WriteUInt32(SuccessInfo.TimeSecondsUntilPCKick);
            _worldPacket.WriteInt32(SuccessInfo.AvailableClasses.Count);
            _worldPacket.WriteInt32(SuccessInfo.Templates.Count);
            _worldPacket.WriteUInt32(SuccessInfo.CurrencyID);
            _worldPacket.WriteInt64(SuccessInfo.Time);

            foreach (var raceClassAvailability in SuccessInfo.AvailableClasses)
            {
                _worldPacket.WriteUInt8(raceClassAvailability.RaceID);
                _worldPacket.WriteInt32(raceClassAvailability.Classes.Count);

                foreach (var classAvailability in raceClassAvailability.Classes)
                {
                    _worldPacket.WriteUInt8(classAvailability.ClassID);
                    _worldPacket.WriteUInt8(classAvailability.ActiveExpansionLevel);
                    _worldPacket.WriteUInt8(classAvailability.AccountExpansionLevel);
                    _worldPacket.WriteUInt8(classAvailability.MinActiveExpansionLevel);
                }
            }

            _worldPacket.WriteBit(SuccessInfo.IsExpansionTrial);
            _worldPacket.WriteBit(SuccessInfo.ForceCharacterTemplate);
            _worldPacket.WriteBit(SuccessInfo.NumPlayersHorde.HasValue);
            _worldPacket.WriteBit(SuccessInfo.NumPlayersAlliance.HasValue);
            _worldPacket.WriteBit(SuccessInfo.ExpansionTrialExpiration.HasValue);
            _worldPacket.FlushBits();

            {
                _worldPacket.WriteUInt32(SuccessInfo.GameTimeInfo.BillingPlan);
                _worldPacket.WriteUInt32(SuccessInfo.GameTimeInfo.TimeRemain);
                _worldPacket.WriteUInt32(SuccessInfo.GameTimeInfo.Unknown735);
                // 3x same bit is not a mistake - preserves legacy client behavior of BillingPlanFlags::SESSION_IGR
                _worldPacket.WriteBit(SuccessInfo.GameTimeInfo.InGameRoom); // inGameRoom check in function checking which lua event to fire when remaining time is near end - BILLING_NAG_DIALOG vs IGR_BILLING_NAG_DIALOG
                _worldPacket.WriteBit(SuccessInfo.GameTimeInfo.InGameRoom); // inGameRoom lua return from Script_GetBillingPlan
                _worldPacket.WriteBit(SuccessInfo.GameTimeInfo.InGameRoom); // not used anywhere in the client
                _worldPacket.FlushBits();
            }

            if (SuccessInfo.NumPlayersHorde.HasValue)
                _worldPacket.WriteUInt16(SuccessInfo.NumPlayersHorde.Value);

            if (SuccessInfo.NumPlayersAlliance.HasValue)
                _worldPacket.WriteUInt16(SuccessInfo.NumPlayersAlliance.Value);

            if (SuccessInfo.ExpansionTrialExpiration.HasValue)
                _worldPacket.WriteInt64(SuccessInfo.ExpansionTrialExpiration.Value);

            foreach (var virtualRealm in SuccessInfo.VirtualRealms)
                virtualRealm.Write(_worldPacket);

            foreach (var templat in SuccessInfo.Templates)
            {
                _worldPacket.WriteUInt32(templat.TemplateSetId);
                _worldPacket.WriteInt32(templat.Classes.Count);

                foreach (var templateClass in templat.Classes)
                {
                    _worldPacket.WriteUInt8(templateClass.ClassID);
                    _worldPacket.WriteUInt8((byte)templateClass.FactionGroup);
                }

                _worldPacket.WriteBits(templat.Name.GetByteCount(), 7);
                _worldPacket.WriteBits(templat.Description.GetByteCount(), 10);
                _worldPacket.FlushBits();

                _worldPacket.WriteString(templat.Name);
                _worldPacket.WriteString(templat.Description);
            }
        }

        WaitInfo?.Write(_worldPacket);
    }

    public class AuthSuccessInfo
    {
        public byte ActiveExpansionLevel;  // the current server expansion, the possible values are in @ref Expansions
        public byte AccountExpansionLevel; // the current expansion of this account, the possible values are in @ref Expansions
        public uint TimeRested;            // affects the return value of the GetBillingTimeRested() client API call, it is the number of seconds you have left until the experience points and loot you receive from creatures and quests is reduced. It is only used in the Asia region in retail, it's not implemented in TC and will probably never be.

        public uint VirtualRealmAddress;    // a special identifier made from the Index, BattleGroup and Region. @todo implement
        public uint TimeSecondsUntilPCKick; // @todo research
        public uint CurrencyID;             // this is probably used for the ingame shop. @todo implement
        public long Time;

        public GameTime GameTimeInfo;

        public List<VirtualRealmInfo> VirtualRealms = new(); // list of realms connected to this one (inclusive) @todo implement
        public List<CharacterTemplate> Templates = new();    // list of pre-made character templates. @todo implement

        public List<RaceClassAvailability> AvailableClasses; // the minimum AccountExpansion required to select the classes

        public bool IsExpansionTrial;
        public bool ForceCharacterTemplate;    // forces the client to always use a character template when creating a new character. @see Templates. @todo implement
        public ushort? NumPlayersHorde;        // number of horde players in this realm. @todo implement
        public ushort? NumPlayersAlliance;     // number of alliance players in this realm. @todo implement
        public long? ExpansionTrialExpiration; // expansion trial expiration unix timestamp

        public struct GameTime
        {
            public uint BillingPlan;
            public uint TimeRemain;
            public uint Unknown735;
            public bool InGameRoom;
        }
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Ticket;

public class GMTicketCaseStatus : ServerPacket
{
    public List<GMTicketCase> Cases = new();
    public GMTicketCaseStatus() : base(ServerOpcodes.GmTicketCaseStatus) { }

    public override void Write()
    {
        _worldPacket.WriteInt32(Cases.Count);

        foreach (var c in Cases)
        {
            _worldPacket.WriteInt32(c.CaseID);
            _worldPacket.WriteInt64(c.CaseOpened);
            _worldPacket.WriteInt32(c.CaseStatus);
            _worldPacket.WriteUInt16(c.CfgRealmID);
            _worldPacket.WriteUInt64(c.CharacterID);
            _worldPacket.WriteInt32(c.WaitTimeOverrideMinutes);

            _worldPacket.WriteBits(c.Url.GetByteCount(), 11);
            _worldPacket.WriteBits(c.WaitTimeOverrideMessage.GetByteCount(), 10);

            _worldPacket.WriteString(c.Url);
            _worldPacket.WriteString(c.WaitTimeOverrideMessage);
        }
    }

    public struct GMTicketCase
    {
        public int CaseID;
        public long CaseOpened;
        public int CaseStatus;
        public ushort CfgRealmID;
        public ulong CharacterID;
        public int WaitTimeOverrideMinutes;
        public string Url;
        public string WaitTimeOverrideMessage;
    }
}
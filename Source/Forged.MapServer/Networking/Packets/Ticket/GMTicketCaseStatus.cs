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
        WorldPacket.WriteInt32(Cases.Count);

        foreach (var c in Cases)
        {
            WorldPacket.WriteInt32(c.CaseID);
            WorldPacket.WriteInt64(c.CaseOpened);
            WorldPacket.WriteInt32(c.CaseStatus);
            WorldPacket.WriteUInt16(c.CfgRealmID);
            WorldPacket.WriteUInt64(c.CharacterID);
            WorldPacket.WriteInt32(c.WaitTimeOverrideMinutes);

            WorldPacket.WriteBits(c.Url.GetByteCount(), 11);
            WorldPacket.WriteBits(c.WaitTimeOverrideMessage.GetByteCount(), 10);

            WorldPacket.WriteString(c.Url);
            WorldPacket.WriteString(c.WaitTimeOverrideMessage);
        }
    }

    public struct GMTicketCase
    {
        public int CaseID;
        public long CaseOpened;
        public int CaseStatus;
        public ushort CfgRealmID;
        public ulong CharacterID;
        public string Url;
        public string WaitTimeOverrideMessage;
        public int WaitTimeOverrideMinutes;
    }
}
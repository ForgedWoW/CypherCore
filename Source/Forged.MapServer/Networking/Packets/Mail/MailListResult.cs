// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Mail;

public class MailListResult : ServerPacket
{
    public List<MailListEntry> Mails = new();
    public int TotalNumRecords;
    public MailListResult() : base(ServerOpcodes.MailListResult) { }

    public override void Write()
    {
        WorldPacket.WriteInt32(Mails.Count);
        WorldPacket.WriteInt32(TotalNumRecords);

        Mails.ForEach(p => p.Write(WorldPacket));
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Common.Networking.Packets.Mail;

public class MailListResult : ServerPacket
{
	public int TotalNumRecords;
	public List<MailListEntry> Mails = new();
	public MailListResult() : base(ServerOpcodes.MailListResult) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Mails.Count);
		_worldPacket.WriteInt32(TotalNumRecords);

		Mails.ForEach(p => p.Write(_worldPacket));
	}
}

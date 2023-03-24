// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Common.Networking.Packets.Calendar;

public class CalendarCommunityInvite : ServerPacket
{
	public List<CalendarEventInitialInviteInfo> Invites = new();
	public CalendarCommunityInvite() : base(ServerOpcodes.CalendarCommunityInvite) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Invites.Count);

		foreach (var invite in Invites)
		{
			_worldPacket.WritePackedGuid(invite.InviteGuid);
			_worldPacket.WriteUInt8(invite.Level);
		}
	}
}

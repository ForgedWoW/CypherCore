// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Mail;

public class NotifyReceivedMail : ServerPacket
{
	public float Delay = 0.0f;
	public NotifyReceivedMail() : base(ServerOpcodes.NotifyReceivedMail) { }

	public override void Write()
	{
		_worldPacket.WriteFloat(Delay);
	}
}
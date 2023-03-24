// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Game.Common.Networking.Packets.Chat;

public class PrintNotification : ServerPacket
{
	public string NotifyText;

	public PrintNotification(string notifyText) : base(ServerOpcodes.PrintNotification)
	{
		NotifyText = notifyText;
	}

	public override void Write()
	{
		_worldPacket.WriteBits(NotifyText.GetByteCount(), 12);
		_worldPacket.WriteString(NotifyText);
	}
}

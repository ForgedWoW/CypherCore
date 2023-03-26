// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Duel;

public class DuelCountdown : ServerPacket
{
    private readonly uint Countdown;

	public DuelCountdown(uint countdown) : base(ServerOpcodes.DuelCountdown)
	{
		Countdown = countdown;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32(Countdown);
	}
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

public class PauseMirrorTimer : ServerPacket
{
	public bool Paused = true;
	public MirrorTimerType Timer;

	public PauseMirrorTimer(MirrorTimerType timer, bool paused) : base(ServerOpcodes.PauseMirrorTimer)
	{
		Timer = timer;
		Paused = paused;
	}

	public override void Write()
	{
		_worldPacket.WriteInt32((int)Timer);
		_worldPacket.WriteBit(Paused);
		_worldPacket.FlushBits();
	}
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class StartMirrorTimer : ServerPacket
{
	public int Scale;
	public int MaxValue;
	public MirrorTimerType Timer;
	public int SpellID;
	public int Value;
	public bool Paused;

	public StartMirrorTimer(MirrorTimerType timer, int value, int maxValue, int scale, int spellID, bool paused) : base(ServerOpcodes.StartMirrorTimer)
	{
		Timer = timer;
		Value = value;
		MaxValue = maxValue;
		Scale = scale;
		SpellID = spellID;
		Paused = paused;
	}

	public override void Write()
	{
		_worldPacket.WriteInt32((int)Timer);
		_worldPacket.WriteInt32(Value);
		_worldPacket.WriteInt32(MaxValue);
		_worldPacket.WriteInt32(Scale);
		_worldPacket.WriteInt32(SpellID);
		_worldPacket.WriteBit(Paused);
		_worldPacket.FlushBits();
	}
}
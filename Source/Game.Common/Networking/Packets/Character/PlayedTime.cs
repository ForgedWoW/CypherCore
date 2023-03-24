// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.Character;

public class PlayedTime : ServerPacket
{
	public uint TotalTime;
	public uint LevelTime;
	public bool TriggerEvent;
	public PlayedTime() : base(ServerOpcodes.PlayedTime, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(TotalTime);
		_worldPacket.WriteUInt32(LevelTime);
		_worldPacket.WriteBit(TriggerEvent);
		_worldPacket.FlushBits();
	}
}

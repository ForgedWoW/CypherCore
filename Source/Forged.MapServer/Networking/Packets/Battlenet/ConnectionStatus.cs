// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Battlenet;

internal class ConnectionStatus : ServerPacket
{
	public byte State;
	public bool SuppressNotification = true;
	public ConnectionStatus() : base(ServerOpcodes.BattleNetConnectionStatus) { }

	public override void Write()
	{
		_worldPacket.WriteBits(State, 2);
		_worldPacket.WriteBit(SuppressNotification);
		_worldPacket.FlushBits();
	}
}
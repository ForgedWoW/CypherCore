// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Game.Common.Networking.Packets.Duel;

public class DuelWinner : ServerPacket
{
	public string BeatenName;
	public string WinnerName;
	public uint BeatenVirtualRealmAddress;
	public uint WinnerVirtualRealmAddress;
	public bool Fled;
	public DuelWinner() : base(ServerOpcodes.DuelWinner, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteBits(BeatenName.GetByteCount(), 6);
		_worldPacket.WriteBits(WinnerName.GetByteCount(), 6);
		_worldPacket.WriteBit(Fled);
		_worldPacket.WriteUInt32(BeatenVirtualRealmAddress);
		_worldPacket.WriteUInt32(WinnerVirtualRealmAddress);
		_worldPacket.WriteString(BeatenName);
		_worldPacket.WriteString(WinnerName);
	}
}

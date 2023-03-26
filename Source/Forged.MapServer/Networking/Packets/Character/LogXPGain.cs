// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Character;

internal class LogXPGain : ServerPacket
{
	public ObjectGuid Victim;
	public int Original;
	public PlayerLogXPReason Reason;
	public int Amount;
	public float GroupBonus;
	public LogXPGain() : base(ServerOpcodes.LogXpGain) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Victim);
		_worldPacket.WriteInt32(Original);
		_worldPacket.WriteUInt8((byte)Reason);
		_worldPacket.WriteInt32(Amount);
		_worldPacket.WriteFloat(GroupBonus);
	}
}
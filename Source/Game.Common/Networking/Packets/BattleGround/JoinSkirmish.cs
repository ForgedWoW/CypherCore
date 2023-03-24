// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.BattleGround;

public class JoinSkirmish : ClientPacket
{
	public byte Roles = 0;
	public BracketType Bracket = 0;
	public bool JoinAsGroup = false;
	public bool UnkBool = false;

	public JoinSkirmish(WorldPacket worldPacket) : base(worldPacket) { }

	public override void Read()
	{
		JoinAsGroup = _worldPacket.ReadBit() != 0;
		UnkBool = _worldPacket.ReadBit() != 0;
		Roles = _worldPacket.ReadBit();
		Bracket = (BracketType)_worldPacket.ReadBit();
	}
}

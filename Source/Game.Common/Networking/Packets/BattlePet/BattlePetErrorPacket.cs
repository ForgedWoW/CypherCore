// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class BattlePetErrorPacket : ServerPacket
{
	public BattlePetError Result;
	public uint CreatureID;
	public BattlePetErrorPacket() : base(ServerOpcodes.BattlePetError) { }

	public override void Write()
	{
		_worldPacket.WriteBits(Result, 4);
		_worldPacket.WriteUInt32(CreatureID);
	}
}
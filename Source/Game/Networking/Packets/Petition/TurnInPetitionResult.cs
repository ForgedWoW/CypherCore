// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class TurnInPetitionResult : ServerPacket
{
	public PetitionTurns Result = 0; // PetitionError
	public TurnInPetitionResult() : base(ServerOpcodes.TurnInPetitionResult) { }

	public override void Write()
	{
		_worldPacket.WriteBits(Result, 4);
		_worldPacket.FlushBits();
	}
}
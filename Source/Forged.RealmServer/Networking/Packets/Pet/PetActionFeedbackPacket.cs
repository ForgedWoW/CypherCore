// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

class PetActionFeedbackPacket : ServerPacket
{
	public uint SpellID;
	public PetActionFeedback Response;
	public PetActionFeedbackPacket() : base(ServerOpcodes.PetStableResult) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WriteUInt8((byte)Response);
	}
}
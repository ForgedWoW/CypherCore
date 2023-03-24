// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.Character;

public class CharCustomizeFailure : ServerPacket
{
	public byte Result;
	public ObjectGuid CharGUID;
	public CharCustomizeFailure() : base(ServerOpcodes.CharCustomizeFailure) { }

	public override void Write()
	{
		_worldPacket.WriteUInt8(Result);
		_worldPacket.WritePackedGuid(CharGUID);
	}
}

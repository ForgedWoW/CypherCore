﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class UndeleteCharacterResponse : ServerPacket
{
	public CharacterUndeleteInfo UndeleteInfo;
	public CharacterUndeleteResult Result;
	public UndeleteCharacterResponse() : base(ServerOpcodes.UndeleteCharacterResponse) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(UndeleteInfo.ClientToken);
		_worldPacket.WriteUInt32((uint)Result);
		_worldPacket.WritePackedGuid(UndeleteInfo.CharacterGuid);
	}
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

public class GenerateRandomCharacterNameResult : ServerPacket
{
	public string Name;
	public bool Success;
	public GenerateRandomCharacterNameResult() : base(ServerOpcodes.GenerateRandomCharacterNameResult) { }

	public override void Write()
	{
		_worldPacket.WriteBit(Success);
		_worldPacket.WriteBits(Name.GetByteCount(), 6);

		_worldPacket.WriteString(Name);
	}
}
﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Character;

public class SetWatchedFaction : ClientPacket
{
	public uint FactionIndex;
	public SetWatchedFaction(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		FactionIndex = _worldPacket.ReadUInt32();
	}
}

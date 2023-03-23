﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Objects;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Guild;

public class GuildSetMemberNote : ClientPacket
{
	public ObjectGuid NoteeGUID;
	public bool IsPublic; // 0 == Officer, 1 == Public
	public string Note;
	public GuildSetMemberNote(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		NoteeGUID = _worldPacket.ReadPackedGuid();

		var noteLen = _worldPacket.ReadBits<uint>(8);
		IsPublic = _worldPacket.HasBit();

		Note = _worldPacket.ReadString(noteLen);
	}
}

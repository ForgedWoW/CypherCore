﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Networking;

namespace Game.Entities;

public class Research
{
	public short ResearchProjectID;

	public void WriteCreate(WorldPacket data, Player owner, Player receiver)
	{
		data.WriteInt16(ResearchProjectID);
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
	{
		data.WriteInt16(ResearchProjectID);
	}
}
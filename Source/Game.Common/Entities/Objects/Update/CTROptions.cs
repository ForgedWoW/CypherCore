﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Networking;

namespace Game.Entities;

public class CTROptions
{
	public uint ContentTuningConditionMask;
	public uint Field_4;
	public uint ExpansionLevelMask;

	public void WriteCreate(WorldPacket data, Player owner, Player receiver)
	{
		data.WriteUInt32(ContentTuningConditionMask);
		data.WriteUInt32(Field_4);
		data.WriteUInt32(ExpansionLevelMask);
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
	{
		data.WriteUInt32(ContentTuningConditionMask);
		data.WriteUInt32(Field_4);
		data.WriteUInt32(ExpansionLevelMask);
	}
}
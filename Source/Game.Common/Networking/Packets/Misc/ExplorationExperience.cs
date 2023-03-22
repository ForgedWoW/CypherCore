﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class ExplorationExperience : ServerPacket
{
	public uint Experience;
	public uint AreaID;

	public ExplorationExperience(uint experience, uint areaID) : base(ServerOpcodes.ExplorationExperience)
	{
		Experience = experience;
		AreaID = areaID;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32(AreaID);
		_worldPacket.WriteUInt32(Experience);
	}
}
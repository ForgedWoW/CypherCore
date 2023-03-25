﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

public class TargetLocation
{
	public ObjectGuid Transport;
	public Position Location;

	public void Read(WorldPacket data)
	{
		Transport = data.ReadPackedGuid();
		Location = new Position();
		Location.X = data.ReadFloat();
		Location.Y = data.ReadFloat();
		Location.Z = data.ReadFloat();
	}

	public void Write(WorldPacket data)
	{
		data.WritePackedGuid(Transport);
		data.WriteFloat(Location.X);
		data.WriteFloat(Location.Y);
		data.WriteFloat(Location.Z);
	}
}
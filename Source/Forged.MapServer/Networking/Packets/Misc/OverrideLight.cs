﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

class OverrideLight : ServerPacket
{
	public uint AreaLightID;
	public uint TransitionMilliseconds;
	public uint OverrideLightID;
	public OverrideLight() : base(ServerOpcodes.OverrideLight) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(AreaLightID);
		_worldPacket.WriteUInt32(OverrideLightID);
		_worldPacket.WriteUInt32(TransitionMilliseconds);
	}
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Game.Common.Networking;

namespace Game.Common.Entities.AreaTriggers;

public struct AreaTriggerMovementScriptInfo
{
	public uint SpellScriptID;
	public Vector3 Center;

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(SpellScriptID);
		data.WriteVector3(Center);
	}
}

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Misc;

public class StandStateUpdate : ServerPacket
{
	readonly uint AnimKitID;
	readonly UnitStandStateType State;

	public StandStateUpdate(UnitStandStateType state, uint animKitId) : base(ServerOpcodes.StandStateUpdate)
	{
		State = state;
		AnimKitID = animKitId;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32(AnimKitID);
		_worldPacket.WriteUInt8((byte)State);
	}
}

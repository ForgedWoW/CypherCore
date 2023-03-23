// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking.Packets.LFG;

namespace Game.Common.Networking.Packets.LFG;

public class LfgBootPlayer : ServerPacket
{
	public LfgBootInfo Info = new();
	public LfgBootPlayer() : base(ServerOpcodes.LfgBootPlayer, ConnectionType.Instance) { }

	public override void Write()
	{
		Info.Write(_worldPacket);
	}
}

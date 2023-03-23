// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Spell;

public class AddRunePower : ServerPacket
{
	public uint AddedRunesMask;
	public AddRunePower() : base(ServerOpcodes.AddRunePower, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(AddedRunesMask);
	}
}

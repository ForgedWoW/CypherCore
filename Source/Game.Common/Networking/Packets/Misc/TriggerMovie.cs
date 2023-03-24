// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.Misc;

public class TriggerMovie : ServerPacket
{
	public uint MovieID;
	public TriggerMovie() : base(ServerOpcodes.TriggerMovie) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(MovieID);
	}
}

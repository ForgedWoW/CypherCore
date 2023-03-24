// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Objects;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.Duel;

public class DuelResponse : ClientPacket
{
	public ObjectGuid ArbiterGUID;
	public bool Accepted;
	public bool Forfeited;
	public DuelResponse(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ArbiterGUID = _worldPacket.ReadPackedGuid();
		Accepted = _worldPacket.HasBit();
		Forfeited = _worldPacket.HasBit();
	}
}

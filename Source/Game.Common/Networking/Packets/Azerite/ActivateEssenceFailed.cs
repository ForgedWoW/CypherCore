// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Azerite;

public class ActivateEssenceFailed : ServerPacket
{
	public AzeriteEssenceActivateResult Reason;
	public uint Arg;
	public uint AzeriteEssenceID;
	public byte? Slot;
	public ActivateEssenceFailed() : base(ServerOpcodes.ActivateEssenceFailed) { }

	public override void Write()
	{
		_worldPacket.WriteBits((int)Reason, 4);
		_worldPacket.WriteBit(Slot.HasValue);
		_worldPacket.WriteUInt32(Arg);
		_worldPacket.WriteUInt32(AzeriteEssenceID);

		if (Slot.HasValue)
			_worldPacket.WriteUInt8(Slot.Value);
	}
}

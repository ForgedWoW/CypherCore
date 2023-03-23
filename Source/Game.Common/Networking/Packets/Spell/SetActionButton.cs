// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Spell;

public class SetActionButton : ClientPacket
{
	public ulong Action; // two packed values (action and type)
	public byte Index;
	public SetActionButton(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Action = _worldPacket.ReadUInt64();
		Index = _worldPacket.ReadUInt8();
	}

	public uint GetButtonAction()
	{
		return (uint)(Action & 0x00FFFFFFFFFFFFFF);
	}

	public uint GetButtonType()
	{
		return (uint)((Action & 0xFF00000000000000) >> 56);
	}
}

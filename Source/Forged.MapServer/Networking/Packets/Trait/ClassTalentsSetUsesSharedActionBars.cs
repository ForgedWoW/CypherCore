// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Trait;

class ClassTalentsSetUsesSharedActionBars : ClientPacket
{
	public int ConfigID;
	public bool UsesShared;
	public bool IsLastSelectedSavedConfig;

	public ClassTalentsSetUsesSharedActionBars(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ConfigID = _worldPacket.ReadInt32();
		UsesShared = _worldPacket.HasBit();
		IsLastSelectedSavedConfig = _worldPacket.HasBit();
	}
}
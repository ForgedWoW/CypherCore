// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Character;

public class OverrideScreenFlash : ClientPacket
{
	public bool ScreenFlashEnabled;

	public OverrideScreenFlash(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ScreenFlashEnabled = _worldPacket.ReadBit() == 1;
	}
}
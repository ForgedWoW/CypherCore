// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Networking.Packets;

public class LoadingScreenNotify : ClientPacket
{
	public int MapID = -1;
	public bool Showing;
	public LoadingScreenNotify(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		MapID = _worldPacket.ReadInt32();
		Showing = _worldPacket.HasBit();
	}
}
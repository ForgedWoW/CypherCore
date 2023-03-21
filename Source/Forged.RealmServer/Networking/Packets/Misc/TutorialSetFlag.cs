// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

public class TutorialSetFlag : ClientPacket
{
	public TutorialAction Action;
	public uint TutorialBit;
	public TutorialSetFlag(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Action = (TutorialAction)_worldPacket.ReadBits<byte>(2);

		if (Action == TutorialAction.Update)
			TutorialBit = _worldPacket.ReadUInt32();
	}
}
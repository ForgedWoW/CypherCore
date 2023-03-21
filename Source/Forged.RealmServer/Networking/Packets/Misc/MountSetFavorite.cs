// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Networking.Packets;

class MountSetFavorite : ClientPacket
{
	public uint MountSpellID;
	public bool IsFavorite;
	public MountSetFavorite(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		MountSpellID = _worldPacket.ReadUInt32();
		IsFavorite = _worldPacket.HasBit();
	}
}
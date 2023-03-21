// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

class PlaySpellVisualKit : ServerPacket
{
	public ObjectGuid Unit;
	public uint KitRecID;
	public uint KitType;
	public uint Duration;
	public bool MountedVisual;
	public PlaySpellVisualKit() : base(ServerOpcodes.PlaySpellVisualKit) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Unit);
		_worldPacket.WriteUInt32(KitRecID);
		_worldPacket.WriteUInt32(KitType);
		_worldPacket.WriteUInt32(Duration);
		_worldPacket.WriteBit(MountedVisual);
		_worldPacket.FlushBits();
	}
}
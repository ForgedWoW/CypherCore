// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class MirrorImageCreatureData : ServerPacket
{
	public ObjectGuid UnitGUID;
	public int DisplayID;
	public int SpellVisualKitID;
	public MirrorImageCreatureData() : base(ServerOpcodes.MirrorImageCreatureData) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(UnitGUID);
		_worldPacket.WriteInt32(DisplayID);
		_worldPacket.WriteInt32(SpellVisualKitID);
	}
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class CancelSpellVisual : ServerPacket
{
	public ObjectGuid Source;
	public uint SpellVisualID;
	public CancelSpellVisual() : base(ServerOpcodes.CancelSpellVisual) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Source);
		_worldPacket.WriteUInt32(SpellVisualID);
	}
}
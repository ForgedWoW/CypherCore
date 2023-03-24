// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.Chat;

public class STextEmote : ServerPacket
{
	public ObjectGuid SourceGUID;
	public ObjectGuid SourceAccountGUID;
	public ObjectGuid TargetGUID;
	public int SoundIndex = -1;
	public int EmoteID;
	public STextEmote() : base(ServerOpcodes.TextEmote, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(SourceGUID);
		_worldPacket.WritePackedGuid(SourceAccountGUID);
		_worldPacket.WriteInt32(EmoteID);
		_worldPacket.WriteInt32(SoundIndex);
		_worldPacket.WritePackedGuid(TargetGUID);
	}
}

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Entities.Objects.Update;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Spell;

public class MirrorImageComponentedData : ServerPacket
{
	public ObjectGuid UnitGUID;
	public int DisplayID;
	public int SpellVisualKitID;
	public byte RaceID;
	public byte Gender;
	public byte ClassID;
	public List<ChrCustomizationChoice> Customizations = new();
	public ObjectGuid GuildGUID;

	public List<int> ItemDisplayID = new();
	public MirrorImageComponentedData() : base(ServerOpcodes.MirrorImageComponentedData) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(UnitGUID);
		_worldPacket.WriteInt32(DisplayID);
		_worldPacket.WriteInt32(SpellVisualKitID);
		_worldPacket.WriteUInt8(RaceID);
		_worldPacket.WriteUInt8(Gender);
		_worldPacket.WriteUInt8(ClassID);
		_worldPacket.WriteInt32(Customizations.Count);
		_worldPacket.WritePackedGuid(GuildGUID);
		_worldPacket.WriteInt32(ItemDisplayID.Count);

		foreach (var customization in Customizations)
		{
			_worldPacket.WriteUInt32(customization.ChrCustomizationOptionID);
			_worldPacket.WriteUInt32(customization.ChrCustomizationChoiceID);
		}

		foreach (var itemDisplayId in ItemDisplayID)
			_worldPacket.WriteInt32(itemDisplayId);
	}
}

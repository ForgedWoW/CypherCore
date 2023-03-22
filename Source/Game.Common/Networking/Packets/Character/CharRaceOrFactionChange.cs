﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class CharRaceOrFactionChange : ClientPacket
{
	public CharRaceOrFactionChangeInfo RaceOrFactionChangeInfo;
	public CharRaceOrFactionChange(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		RaceOrFactionChangeInfo = new CharRaceOrFactionChangeInfo();

		RaceOrFactionChangeInfo.FactionChange = _worldPacket.HasBit();

		var nameLength = _worldPacket.ReadBits<uint>(6);

		RaceOrFactionChangeInfo.Guid = _worldPacket.ReadPackedGuid();
		RaceOrFactionChangeInfo.SexID = (Gender)_worldPacket.ReadUInt8();
		RaceOrFactionChangeInfo.RaceID = (Race)_worldPacket.ReadUInt8();
		RaceOrFactionChangeInfo.InitialRaceID = (Race)_worldPacket.ReadUInt8();
		var customizationCount = _worldPacket.ReadUInt32();
		RaceOrFactionChangeInfo.Name = _worldPacket.ReadString(nameLength);

		for (var i = 0; i < customizationCount; ++i)
			RaceOrFactionChangeInfo.Customizations[i] = new ChrCustomizationChoice()
			{
				ChrCustomizationOptionID = _worldPacket.ReadUInt32(),
				ChrCustomizationChoiceID = _worldPacket.ReadUInt32()
			};

		RaceOrFactionChangeInfo.Customizations.Sort();
	}
}
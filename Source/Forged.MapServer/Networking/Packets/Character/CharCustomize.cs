// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects.Update;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Character;

public class CharCustomize : ClientPacket
{
	public CharCustomizeInfo CustomizeInfo;
	public CharCustomize(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		CustomizeInfo = new CharCustomizeInfo
		{
			CharGUID = _worldPacket.ReadPackedGuid(),
			SexID = (Gender)_worldPacket.ReadUInt8()
		};

		var customizationCount = _worldPacket.ReadUInt32();

		for (var i = 0; i < customizationCount; ++i)
			CustomizeInfo.Customizations[i] = new ChrCustomizationChoice()
			{
				ChrCustomizationOptionID = _worldPacket.ReadUInt32(),
				ChrCustomizationChoiceID = _worldPacket.ReadUInt32()
			};

		CustomizeInfo.Customizations.Sort();

		CustomizeInfo.CharName = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(6));
	}
}
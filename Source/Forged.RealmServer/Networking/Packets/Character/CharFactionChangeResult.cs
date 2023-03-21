// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class CharFactionChangeResult : ServerPacket
{
	public ResponseCodes Result = 0;
	public ObjectGuid Guid;
	public CharFactionChangeDisplayInfo Display;
	public CharFactionChangeResult() : base(ServerOpcodes.CharFactionChangeResult) { }

	public override void Write()
	{
		_worldPacket.WriteUInt8((byte)Result);
		_worldPacket.WritePackedGuid(Guid);
		_worldPacket.WriteBit(Display != null);
		_worldPacket.FlushBits();

		if (Display != null)
		{
			_worldPacket.WriteBits(Display.Name.GetByteCount(), 6);
			_worldPacket.WriteUInt8(Display.SexID);
			_worldPacket.WriteUInt8(Display.RaceID);
			_worldPacket.WriteInt32(Display.Customizations.Count);
			_worldPacket.WriteString(Display.Name);

			foreach (var customization in Display.Customizations)
			{
				_worldPacket.WriteUInt32(customization.ChrCustomizationOptionID);
				_worldPacket.WriteUInt32(customization.ChrCustomizationChoiceID);
			}
		}
	}

	public class CharFactionChangeDisplayInfo
	{
		public string Name;
		public byte SexID;
		public byte RaceID;
		public Array<ChrCustomizationChoice> Customizations = new(72);
	}
}
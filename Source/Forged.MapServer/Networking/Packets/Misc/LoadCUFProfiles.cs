// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Players;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

internal class LoadCUFProfiles : ServerPacket
{
	public List<CufProfile> CUFProfiles = new();
	public LoadCUFProfiles() : base(ServerOpcodes.LoadCufProfiles, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(CUFProfiles.Count);

		foreach (var cufProfile in CUFProfiles)
		{
			_worldPacket.WriteBits(cufProfile.ProfileName.GetByteCount(), 7);

			// Bool Options
			for (byte option = 0; option < (int)CUFBoolOptions.BoolOptionsCount; option++)
				_worldPacket.WriteBit(cufProfile.BoolOptions[option]);

			// Other Options
			_worldPacket.WriteUInt16(cufProfile.FrameHeight);
			_worldPacket.WriteUInt16(cufProfile.FrameWidth);

			_worldPacket.WriteUInt8(cufProfile.SortBy);
			_worldPacket.WriteUInt8(cufProfile.HealthText);

			_worldPacket.WriteUInt8(cufProfile.TopPoint);
			_worldPacket.WriteUInt8(cufProfile.BottomPoint);
			_worldPacket.WriteUInt8(cufProfile.LeftPoint);

			_worldPacket.WriteUInt16(cufProfile.TopOffset);
			_worldPacket.WriteUInt16(cufProfile.BottomOffset);
			_worldPacket.WriteUInt16(cufProfile.LeftOffset);

			_worldPacket.WriteString(cufProfile.ProfileName);
		}
	}
}
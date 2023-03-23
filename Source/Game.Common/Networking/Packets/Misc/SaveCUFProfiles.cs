// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Players;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Misc;

public class SaveCUFProfiles : ClientPacket
{
	public List<CufProfile> CUFProfiles = new();
	public SaveCUFProfiles(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var count = _worldPacket.ReadUInt32();

		for (byte i = 0; i < count && i < PlayerConst.MaxCUFProfiles; i++)
		{
			CufProfile cufProfile = new();

			var strLen = _worldPacket.ReadBits<byte>(7);

			// Bool Options
			for (byte option = 0; option < (int)CUFBoolOptions.BoolOptionsCount; option++)
				cufProfile.BoolOptions.Set(option, _worldPacket.HasBit());

			// Other Options
			cufProfile.FrameHeight = _worldPacket.ReadUInt16();
			cufProfile.FrameWidth = _worldPacket.ReadUInt16();

			cufProfile.SortBy = _worldPacket.ReadUInt8();
			cufProfile.HealthText = _worldPacket.ReadUInt8();

			cufProfile.TopPoint = _worldPacket.ReadUInt8();
			cufProfile.BottomPoint = _worldPacket.ReadUInt8();
			cufProfile.LeftPoint = _worldPacket.ReadUInt8();

			cufProfile.TopOffset = _worldPacket.ReadUInt16();
			cufProfile.BottomOffset = _worldPacket.ReadUInt16();
			cufProfile.LeftOffset = _worldPacket.ReadUInt16();

			cufProfile.ProfileName = _worldPacket.ReadString(strLen);

			CUFProfiles.Add(cufProfile);
		}
	}
}

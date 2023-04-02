// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Players;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

internal class SaveCUFProfiles : ClientPacket
{
    public List<CufProfile> CUFProfiles = new();
    public SaveCUFProfiles(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        var count = WorldPacket.ReadUInt32();

        for (byte i = 0; i < count && i < PlayerConst.MaxCUFProfiles; i++)
        {
            CufProfile cufProfile = new();

            var strLen = WorldPacket.ReadBits<byte>(7);

            // Bool Options
            for (byte option = 0; option < (int)CUFBoolOptions.BoolOptionsCount; option++)
                cufProfile.BoolOptions.Set(option, WorldPacket.HasBit());

            // Other Options
            cufProfile.FrameHeight = WorldPacket.ReadUInt16();
            cufProfile.FrameWidth = WorldPacket.ReadUInt16();

            cufProfile.SortBy = WorldPacket.ReadUInt8();
            cufProfile.HealthText = WorldPacket.ReadUInt8();

            cufProfile.TopPoint = WorldPacket.ReadUInt8();
            cufProfile.BottomPoint = WorldPacket.ReadUInt8();
            cufProfile.LeftPoint = WorldPacket.ReadUInt8();

            cufProfile.TopOffset = WorldPacket.ReadUInt16();
            cufProfile.BottomOffset = WorldPacket.ReadUInt16();
            cufProfile.LeftOffset = WorldPacket.ReadUInt16();

            cufProfile.ProfileName = WorldPacket.ReadString(strLen);

            CUFProfiles.Add(cufProfile);
        }
    }
}
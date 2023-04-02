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
        WorldPacket.WriteInt32(CUFProfiles.Count);

        foreach (var cufProfile in CUFProfiles)
        {
            WorldPacket.WriteBits(cufProfile.ProfileName.GetByteCount(), 7);

            // Bool Options
            for (byte option = 0; option < (int)CUFBoolOptions.BoolOptionsCount; option++)
                WorldPacket.WriteBit(cufProfile.BoolOptions[option]);

            // Other Options
            WorldPacket.WriteUInt16(cufProfile.FrameHeight);
            WorldPacket.WriteUInt16(cufProfile.FrameWidth);

            WorldPacket.WriteUInt8(cufProfile.SortBy);
            WorldPacket.WriteUInt8(cufProfile.HealthText);

            WorldPacket.WriteUInt8(cufProfile.TopPoint);
            WorldPacket.WriteUInt8(cufProfile.BottomPoint);
            WorldPacket.WriteUInt8(cufProfile.LeftPoint);

            WorldPacket.WriteUInt16(cufProfile.TopOffset);
            WorldPacket.WriteUInt16(cufProfile.BottomOffset);
            WorldPacket.WriteUInt16(cufProfile.LeftOffset);

            WorldPacket.WriteString(cufProfile.ProfileName);
        }
    }
}
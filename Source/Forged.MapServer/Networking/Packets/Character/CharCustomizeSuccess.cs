// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Character;

internal class CharCustomizeSuccess : ServerPacket
{
    private readonly ObjectGuid CharGUID;
    private readonly string CharName = "";
    private readonly Array<ChrCustomizationChoice> Customizations = new(72);
    private readonly byte SexID;

    public CharCustomizeSuccess(CharCustomizeInfo customizeInfo) : base(ServerOpcodes.CharCustomizeSuccess)
    {
        CharGUID = customizeInfo.CharGUID;
        SexID = (byte)customizeInfo.SexID;
        CharName = customizeInfo.CharName;
        Customizations = customizeInfo.Customizations;
    }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(CharGUID);
        WorldPacket.WriteUInt8(SexID);
        WorldPacket.WriteInt32(Customizations.Count);

        foreach (var customization in Customizations)
        {
            WorldPacket.WriteUInt32(customization.ChrCustomizationOptionID);
            WorldPacket.WriteUInt32(customization.ChrCustomizationChoiceID);
        }

        WorldPacket.WriteBits(CharName.GetByteCount(), 6);
        WorldPacket.FlushBits();
        WorldPacket.WriteString(CharName);
    }
}
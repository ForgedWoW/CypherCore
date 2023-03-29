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
    private readonly string CharName = "";
    private readonly byte SexID;
    private readonly Array<ChrCustomizationChoice> Customizations = new(72);

    private readonly ObjectGuid CharGUID;

    public CharCustomizeSuccess(CharCustomizeInfo customizeInfo) : base(ServerOpcodes.CharCustomizeSuccess)
    {
        CharGUID = customizeInfo.CharGUID;
        SexID = (byte)customizeInfo.SexID;
        CharName = customizeInfo.CharName;
        Customizations = customizeInfo.Customizations;
    }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(CharGUID);
        _worldPacket.WriteUInt8(SexID);
        _worldPacket.WriteInt32(Customizations.Count);

        foreach (var customization in Customizations)
        {
            _worldPacket.WriteUInt32(customization.ChrCustomizationOptionID);
            _worldPacket.WriteUInt32(customization.ChrCustomizationChoiceID);
        }

        _worldPacket.WriteBits(CharName.GetByteCount(), 6);
        _worldPacket.FlushBits();
        _worldPacket.WriteString(CharName);
    }
}
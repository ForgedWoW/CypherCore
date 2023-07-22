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
    private readonly ObjectGuid _charGUID;
    private readonly string _charName;
    private readonly Array<ChrCustomizationChoice> _customizations;
    private readonly byte _sexID;

    public CharCustomizeSuccess(CharCustomizeInfo customizeInfo) : base(ServerOpcodes.CharCustomizeSuccess)
    {
        _charGUID = customizeInfo.CharGUID;
        _sexID = (byte)customizeInfo.SexID;
        _charName = customizeInfo.CharName;
        _customizations = customizeInfo.Customizations;
    }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(_charGUID);
        WorldPacket.WriteUInt8(_sexID);
        WorldPacket.WriteInt32(_customizations.Count);

        foreach (var customization in _customizations)
        {
            WorldPacket.WriteUInt32(customization.ChrCustomizationOptionID);
            WorldPacket.WriteUInt32(customization.ChrCustomizationChoiceID);
        }

        WorldPacket.WriteBits(_charName.GetByteCount(), 6);
        WorldPacket.FlushBits();
        WorldPacket.WriteString(_charName);
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Character;

public class CharFactionChangeResult : ServerPacket
{
    public CharFactionChangeDisplayInfo Display;
    public ObjectGuid Guid;
    public ResponseCodes Result = 0;
    public CharFactionChangeResult() : base(ServerOpcodes.CharFactionChangeResult) { }

    public override void Write()
    {
        WorldPacket.WriteUInt8((byte)Result);
        WorldPacket.WritePackedGuid(Guid);
        WorldPacket.WriteBit(Display != null);
        WorldPacket.FlushBits();

        if (Display != null)
        {
            WorldPacket.WriteBits(Display.Name.GetByteCount(), 6);
            WorldPacket.WriteUInt8(Display.SexID);
            WorldPacket.WriteUInt8(Display.RaceID);
            WorldPacket.WriteInt32(Display.Customizations.Count);
            WorldPacket.WriteString(Display.Name);

            foreach (var customization in Display.Customizations)
            {
                WorldPacket.WriteUInt32(customization.ChrCustomizationOptionID);
                WorldPacket.WriteUInt32(customization.ChrCustomizationChoiceID);
            }
        }
    }

    public class CharFactionChangeDisplayInfo
    {
        public Array<ChrCustomizationChoice> Customizations = new(72);
        public string Name;
        public byte RaceID;
        public byte SexID;
    }
}
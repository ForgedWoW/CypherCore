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
        public Array<ChrCustomizationChoice> Customizations = new(72);
        public string Name;
        public byte RaceID;
        public byte SexID;
    }
}
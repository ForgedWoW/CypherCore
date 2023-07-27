// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.CombatLog;

public struct CombatWorldTextViewerInfo
{
    public ObjectGuid ViewerGUID;
    public byte? ColorType;
    public byte? ScaleType;
    
    public void Write(WorldPacket data)
    {
        data.WritePackedGuid(ViewerGUID);
        data.WritePackedGuid(ViewerGUID);
        data.WriteBit(ColorType.HasValue);
        data.WriteBit(ScaleType.HasValue);
        data.FlushBits();

        if (ColorType.HasValue)
            data.WriteUInt8(ColorType.Value);

        if (ScaleType.HasValue)
            data.WriteUInt8(ScaleType.Value);
    }
}
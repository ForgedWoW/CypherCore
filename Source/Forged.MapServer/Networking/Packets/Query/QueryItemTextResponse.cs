// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Query;

internal class QueryItemTextResponse : ServerPacket
{
    public ObjectGuid Id;
    public string Text;
    public bool Valid;
    public QueryItemTextResponse() : base(ServerOpcodes.QueryItemTextResponse) { }

    public override void Write()
    {
        _worldPacket.WriteBit(Valid);
        _worldPacket.WriteBits(Text.GetByteCount(), 13);
        _worldPacket.FlushBits();

        _worldPacket.WriteString(Text);
        _worldPacket.WritePackedGuid(Id);
    }
}
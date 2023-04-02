// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Query;

public class QueryPageTextResponse : ServerPacket
{
    public bool Allow;
    public List<PageTextInfo> Pages = new();
    public uint PageTextID;
    public QueryPageTextResponse() : base(ServerOpcodes.QueryPageTextResponse) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(PageTextID);
        WorldPacket.WriteBit(Allow);
        WorldPacket.FlushBits();

        if (Allow)
        {
            WorldPacket.WriteInt32(Pages.Count);

            foreach (var pageText in Pages)
                pageText.Write(WorldPacket);
        }
    }

    public struct PageTextInfo
    {
        public byte Flags;

        public uint Id;

        public uint NextPageID;

        public int PlayerConditionID;

        public string Text;

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(Id);
            data.WriteUInt32(NextPageID);
            data.WriteInt32(PlayerConditionID);
            data.WriteUInt8(Flags);
            data.WriteBits(Text.GetByteCount(), 12);
            data.FlushBits();

            data.WriteString(Text);
        }
    }
}
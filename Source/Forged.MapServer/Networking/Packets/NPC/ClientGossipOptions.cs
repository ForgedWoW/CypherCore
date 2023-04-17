// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.NPC;

public class ClientGossipOptions
{
    public string Confirm = "";
    public GossipOptionFlags Flags;
    public int GossipOptionID;
    public int OptionCost;
    public byte OptionFlags;
    public uint OptionLanguage;
    public GossipOptionNpc OptionNPC;
    public int OrderIndex;
    public int? OverrideIconID;
    public int? SpellID;
    public GossipOptionStatus Status;
    public string Text = "";
    public TreasureLootList Treasure = new();

    public void Write(WorldPacket data)
    {
        data.WriteInt32(GossipOptionID);
        data.WriteUInt8((byte)OptionNPC);
        data.WriteInt8((sbyte)OptionFlags);
        data.WriteInt32(OptionCost);
        data.WriteUInt32(OptionLanguage);
        data.WriteInt32((int)Flags);
        data.WriteInt32(OrderIndex);
        data.WriteBits(Text.GetByteCount(), 12);
        data.WriteBits(Confirm.GetByteCount(), 12);
        data.WriteBits((byte)Status, 2);
        data.WriteBit(SpellID.HasValue);
        data.WriteBit(OverrideIconID.HasValue);
        data.FlushBits();

        Treasure.Write(data);

        data.WriteString(Text);
        data.WriteString(Confirm);

        if (SpellID.HasValue)
            data.WriteInt32(SpellID.Value);

        if (OverrideIconID.HasValue)
            data.WriteInt32(OverrideIconID.Value);
    }
}
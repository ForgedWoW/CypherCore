// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Quest;

internal class ChoiceResponse : ClientPacket
{
    public int ChoiceID;
    public bool IsReroll;
    public int ResponseIdentifier;
    public ChoiceResponse(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        ChoiceID = WorldPacket.ReadInt32();
        ResponseIdentifier = WorldPacket.ReadInt32();
        IsReroll = WorldPacket.HasBit();
    }
}
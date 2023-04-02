// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Misc;

public class RandomRollClient : ClientPacket
{
    public uint Max;
    public uint Min;
    public byte PartyIndex;
    public RandomRollClient(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Min = _worldPacket.ReadUInt32();
        Max = _worldPacket.ReadUInt32();
        PartyIndex = _worldPacket.ReadUInt8();
    }
}
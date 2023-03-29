// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.AreaTrigger;

internal class AreaTriggerPkt : ClientPacket
{
    public uint AreaTriggerID;
    public bool Entered;
    public bool FromClient;
    public AreaTriggerPkt(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        AreaTriggerID = _worldPacket.ReadUInt32();
        Entered = _worldPacket.HasBit();
        FromClient = _worldPacket.HasBit();
    }
}

//Structs
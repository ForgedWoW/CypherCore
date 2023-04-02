// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class SetSavedInstanceExtend : ClientPacket
{
    public uint DifficultyID;
    public bool Extend;
    public int MapID;
    public SetSavedInstanceExtend(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        MapID = _worldPacket.ReadInt32();
        DifficultyID = _worldPacket.ReadUInt32();
        Extend = _worldPacket.HasBit();
    }
}
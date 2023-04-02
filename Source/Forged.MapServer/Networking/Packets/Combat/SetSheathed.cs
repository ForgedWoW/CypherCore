// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Combat;

public class SetSheathed : ClientPacket
{
    public bool Animate = true;
    public int CurrentSheathState;
    public SetSheathed(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        CurrentSheathState = _worldPacket.ReadInt32();
        Animate = _worldPacket.HasBit();
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Character;

internal class SetFactionInactive : ClientPacket
{
    public uint Index;
    public bool State;
    public SetFactionInactive(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Index = WorldPacket.ReadUInt32();
        State = WorldPacket.HasBit();
    }
}
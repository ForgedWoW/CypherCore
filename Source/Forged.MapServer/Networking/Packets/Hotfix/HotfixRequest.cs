// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.Hotfix;

internal class HotfixRequest : ClientPacket
{
    public uint ClientBuild;
    public uint DataBuild;
    public List<int> Hotfixes = new();
    public HotfixRequest(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        ClientBuild = WorldPacket.ReadUInt32();
        DataBuild = WorldPacket.ReadUInt32();

        var hotfixCount = WorldPacket.ReadUInt32();

        for (var i = 0; i < hotfixCount; ++i)
            Hotfixes.Add(WorldPacket.ReadInt32());
    }
}
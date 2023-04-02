// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.GameObject;

public class GameObjReportUse : ClientPacket
{
    public ObjectGuid Guid;
    public bool IsSoftInteract;
    public GameObjReportUse(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Guid = WorldPacket.ReadPackedGuid();
        IsSoftInteract = WorldPacket.HasBit();
    }
}
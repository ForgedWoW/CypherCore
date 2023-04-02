// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Achievements;

public class RespondInspectAchievements : ServerPacket
{
    public AllAchievements Data = new();
    public ObjectGuid Player;
    public RespondInspectAchievements() : base(ServerOpcodes.RespondInspectAchievements, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Player);
        Data.Write(WorldPacket);
    }
}
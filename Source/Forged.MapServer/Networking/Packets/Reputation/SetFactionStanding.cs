// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Reputation;

internal class SetFactionStanding : ServerPacket
{
    public float BonusFromAchievementSystem;
    public List<FactionStandingData> Faction = new();
    public bool ShowVisual;
    public SetFactionStanding() : base(ServerOpcodes.SetFactionStanding, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteFloat(BonusFromAchievementSystem);

        WorldPacket.WriteInt32(Faction.Count);

        foreach (var factionStanding in Faction)
            factionStanding.Write(WorldPacket);

        WorldPacket.WriteBit(ShowVisual);
        WorldPacket.FlushBits();
    }
}
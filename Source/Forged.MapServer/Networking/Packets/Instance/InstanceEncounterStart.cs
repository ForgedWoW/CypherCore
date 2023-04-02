// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Instance;

internal class InstanceEncounterStart : ServerPacket
{
    public uint CombatResChargeRecovery;
    public uint InCombatResCount; // amount of usable battle ressurections
    public bool InProgress = true;
    public uint MaxInCombatResCount;
    public uint NextCombatResChargeTime;
    public InstanceEncounterStart() : base(ServerOpcodes.InstanceEncounterStart, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(InCombatResCount);
        WorldPacket.WriteUInt32(MaxInCombatResCount);
        WorldPacket.WriteUInt32(CombatResChargeRecovery);
        WorldPacket.WriteUInt32(NextCombatResChargeTime);
        WorldPacket.WriteBit(InProgress);
        WorldPacket.FlushBits();
    }
}
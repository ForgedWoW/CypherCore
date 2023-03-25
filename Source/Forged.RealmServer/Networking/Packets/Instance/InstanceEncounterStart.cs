// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

class InstanceEncounterStart : ServerPacket
{
	public uint InCombatResCount; // amount of usable battle ressurections
	public uint MaxInCombatResCount;
	public uint CombatResChargeRecovery;
	public uint NextCombatResChargeTime;
	public bool InProgress = true;
	public InstanceEncounterStart() : base(ServerOpcodes.InstanceEncounterStart, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(InCombatResCount);
		_worldPacket.WriteUInt32(MaxInCombatResCount);
		_worldPacket.WriteUInt32(CombatResChargeRecovery);
		_worldPacket.WriteUInt32(NextCombatResChargeTime);
		_worldPacket.WriteBit(InProgress);
		_worldPacket.FlushBits();
	}
}
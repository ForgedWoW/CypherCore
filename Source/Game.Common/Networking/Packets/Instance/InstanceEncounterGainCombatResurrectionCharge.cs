// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class InstanceEncounterGainCombatResurrectionCharge : ServerPacket
{
	public int InCombatResCount;
	public uint CombatResChargeRecovery;
	public InstanceEncounterGainCombatResurrectionCharge() : base(ServerOpcodes.InstanceEncounterGainCombatResurrectionCharge, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(InCombatResCount);
		_worldPacket.WriteUInt32(CombatResChargeRecovery);
	}
}
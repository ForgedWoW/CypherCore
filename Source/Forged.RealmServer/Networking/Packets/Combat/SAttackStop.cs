// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class SAttackStop : ServerPacket
{
	public ObjectGuid Attacker;
	public ObjectGuid Victim;
	public bool NowDead;

	public SAttackStop(Unit attacker, Unit victim) : base(ServerOpcodes.AttackStop, ConnectionType.Instance)
	{
		Attacker = attacker.GUID;

		if (victim)
		{
			Victim = victim.GUID;
			NowDead = !victim.IsAlive; // using isAlive instead of isDead to catch JUST_DIED death states as well
		}
	}

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Attacker);
		_worldPacket.WritePackedGuid(Victim);
		_worldPacket.WriteBit(NowDead);
		_worldPacket.FlushBits();
	}
}
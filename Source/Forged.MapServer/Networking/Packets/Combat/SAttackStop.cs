// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Combat;

public class SAttackStop : ServerPacket
{
    public ObjectGuid Attacker;
    public bool NowDead;
    public ObjectGuid Victim;

    public SAttackStop(Unit attacker, Unit victim) : base(ServerOpcodes.AttackStop, ConnectionType.Instance)
    {
        Attacker = attacker.GUID;

        if (victim == null)
            return;

        Victim = victim.GUID;
        NowDead = !victim.IsAlive; // using isAlive instead of isDead to catch JUST_DIED death states as well
    }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Attacker);
        WorldPacket.WritePackedGuid(Victim);
        WorldPacket.WriteBit(NowDead);
        WorldPacket.FlushBits();
    }
}
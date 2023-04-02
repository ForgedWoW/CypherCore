// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.CombatLog;

public class SpellInstakillLog : ServerPacket
{
    public ObjectGuid Caster;
    public uint SpellID;
    public ObjectGuid Target;
    public SpellInstakillLog() : base(ServerOpcodes.SpellInstakillLog, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Target);
        WorldPacket.WritePackedGuid(Caster);
        WorldPacket.WriteUInt32(SpellID);
    }
}
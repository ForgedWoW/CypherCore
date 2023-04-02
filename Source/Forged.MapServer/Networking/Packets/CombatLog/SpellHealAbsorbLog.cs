// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Spell;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.CombatLog;

internal class SpellHealAbsorbLog : ServerPacket
{
    public ObjectGuid AbsorbCaster;
    public int Absorbed;
    public int AbsorbedSpellID;
    public int AbsorbSpellID;
    public ContentTuningParams ContentTuning;
    public ObjectGuid Healer;
    public int OriginalHeal;
    public ObjectGuid Target;
    public SpellHealAbsorbLog() : base(ServerOpcodes.SpellHealAbsorbLog, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Target);
        WorldPacket.WritePackedGuid(AbsorbCaster);
        WorldPacket.WritePackedGuid(Healer);
        WorldPacket.WriteInt32(AbsorbSpellID);
        WorldPacket.WriteInt32(AbsorbedSpellID);
        WorldPacket.WriteInt32(Absorbed);
        WorldPacket.WriteInt32(OriginalHeal);
        WorldPacket.WriteBit(ContentTuning != null);
        WorldPacket.FlushBits();

        ContentTuning?.Write(WorldPacket);
    }
}
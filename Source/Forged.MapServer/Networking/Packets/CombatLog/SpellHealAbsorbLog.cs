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
        _worldPacket.WritePackedGuid(Target);
        _worldPacket.WritePackedGuid(AbsorbCaster);
        _worldPacket.WritePackedGuid(Healer);
        _worldPacket.WriteInt32(AbsorbSpellID);
        _worldPacket.WriteInt32(AbsorbedSpellID);
        _worldPacket.WriteInt32(Absorbed);
        _worldPacket.WriteInt32(OriginalHeal);
        _worldPacket.WriteBit(ContentTuning != null);
        _worldPacket.FlushBits();

        ContentTuning?.Write(_worldPacket);
    }
}
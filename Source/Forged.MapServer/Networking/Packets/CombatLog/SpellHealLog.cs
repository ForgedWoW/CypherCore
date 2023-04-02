// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Spell;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.CombatLog;

internal class SpellHealLog : CombatLogServerPacket
{
    public uint Absorbed;
    public ObjectGuid CasterGUID;
    public ContentTuningParams ContentTuning;
    public bool Crit;
    public float? CritRollMade;
    public float? CritRollNeeded;
    public uint Health;
    public int OriginalHeal;
    public uint OverHeal;
    public uint SpellID;
    public ObjectGuid TargetGUID;
    public SpellHealLog() : base(ServerOpcodes.SpellHealLog, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(TargetGUID);
        _worldPacket.WritePackedGuid(CasterGUID);

        _worldPacket.WriteUInt32(SpellID);
        _worldPacket.WriteUInt32(Health);
        _worldPacket.WriteInt32(OriginalHeal);
        _worldPacket.WriteUInt32(OverHeal);
        _worldPacket.WriteUInt32(Absorbed);

        _worldPacket.WriteBit(Crit);

        _worldPacket.WriteBit(CritRollMade.HasValue);
        _worldPacket.WriteBit(CritRollNeeded.HasValue);
        WriteLogDataBit();
        _worldPacket.WriteBit(ContentTuning != null);
        FlushBits();

        WriteLogData();

        if (CritRollMade.HasValue)
            _worldPacket.WriteFloat(CritRollMade.Value);

        if (CritRollNeeded.HasValue)
            _worldPacket.WriteFloat(CritRollNeeded.Value);

        ContentTuning?.Write(_worldPacket);
    }
}
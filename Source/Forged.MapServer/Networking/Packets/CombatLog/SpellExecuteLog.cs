// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.CombatLog;

internal class SpellExecuteLog : CombatLogServerPacket
{
    public ObjectGuid Caster;
    public List<SpellLogEffect> Effects = new();
    public uint SpellID;
    public SpellExecuteLog() : base(ServerOpcodes.SpellExecuteLog, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Caster);
        WorldPacket.WriteUInt32(SpellID);
        WorldPacket.WriteInt32(Effects.Count);

        foreach (var effect in Effects)
        {
            WorldPacket.WriteInt32(effect.Effect);

            WorldPacket.WriteInt32(effect.PowerDrainTargets.Count);
            WorldPacket.WriteInt32(effect.ExtraAttacksTargets.Count);
            WorldPacket.WriteInt32(effect.DurabilityDamageTargets.Count);
            WorldPacket.WriteInt32(effect.GenericVictimTargets.Count);
            WorldPacket.WriteInt32(effect.TradeSkillTargets.Count);
            WorldPacket.WriteInt32(effect.FeedPetTargets.Count);

            foreach (var powerDrainTarget in effect.PowerDrainTargets)
            {
                WorldPacket.WritePackedGuid(powerDrainTarget.Victim);
                WorldPacket.WriteUInt32(powerDrainTarget.Points);
                WorldPacket.WriteUInt32(powerDrainTarget.PowerType);
                WorldPacket.WriteFloat(powerDrainTarget.Amplitude);
            }

            foreach (var extraAttacksTarget in effect.ExtraAttacksTargets)
            {
                WorldPacket.WritePackedGuid(extraAttacksTarget.Victim);
                WorldPacket.WriteUInt32(extraAttacksTarget.NumAttacks);
            }

            foreach (var durabilityDamageTarget in effect.DurabilityDamageTargets)
            {
                WorldPacket.WritePackedGuid(durabilityDamageTarget.Victim);
                WorldPacket.WriteInt32(durabilityDamageTarget.ItemID);
                WorldPacket.WriteInt32(durabilityDamageTarget.Amount);
            }

            foreach (var genericVictimTarget in effect.GenericVictimTargets)
                WorldPacket.WritePackedGuid(genericVictimTarget.Victim);

            foreach (var tradeSkillTarget in effect.TradeSkillTargets)
                WorldPacket.WriteInt32(tradeSkillTarget.ItemID);


            foreach (var feedPetTarget in effect.FeedPetTargets)
                WorldPacket.WriteInt32(feedPetTarget.ItemID);
        }

        WriteLogDataBit();
        FlushBits();
        WriteLogData();
    }
}
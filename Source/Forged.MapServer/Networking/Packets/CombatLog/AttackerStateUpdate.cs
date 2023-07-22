// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Spell;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.CombatLog;

internal class AttackerStateUpdate : CombatLogServerPacket
{
    public ObjectGuid AttackerGUID;
    public uint AttackerState;
    public int BlockAmount;
    public ContentTuningParams ContentTuning = new();
    public int Damage;
    public HitInfo HitInfo; // Flags
    public uint MeleeSpellID;
    public int OriginalDamage;
    public int OverDamage = -1;

    public int RageGained;

    // (damage - health) or -1 if unit is still alive
    public SubDamage? SubDmg;

    public float Unk;
    public UnkAttackerState UnkState;
    public ObjectGuid VictimGUID;
    public byte VictimState;
    public AttackerStateUpdate() : base(ServerOpcodes.AttackerStateUpdate, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket attackRoundInfo = new();
        attackRoundInfo.WriteUInt32((uint)HitInfo);
        attackRoundInfo.WritePackedGuid(AttackerGUID);
        attackRoundInfo.WritePackedGuid(VictimGUID);
        attackRoundInfo.WriteInt32(Damage);
        attackRoundInfo.WriteInt32(OriginalDamage);
        attackRoundInfo.WriteInt32(OverDamage);
        attackRoundInfo.WriteUInt8((byte)(SubDmg.HasValue ? 1 : 0));

        if (SubDmg.HasValue)
        {
            attackRoundInfo.WriteInt32(SubDmg.Value.SchoolMask);
            attackRoundInfo.WriteFloat(SubDmg.Value.FDamage);
            attackRoundInfo.WriteInt32(SubDmg.Value.Damage);

            if (HitInfo.HasAnyFlag(HitInfo.FullAbsorb | HitInfo.PartialAbsorb))
                attackRoundInfo.WriteInt32(SubDmg.Value.Absorbed);

            if (HitInfo.HasAnyFlag(HitInfo.FullResist | HitInfo.PartialResist))
                attackRoundInfo.WriteInt32(SubDmg.Value.Resisted);
        }

        attackRoundInfo.WriteUInt8(VictimState);
        attackRoundInfo.WriteUInt32(AttackerState);
        attackRoundInfo.WriteUInt32(MeleeSpellID);

        if (HitInfo.HasAnyFlag(HitInfo.Block))
            attackRoundInfo.WriteInt32(BlockAmount);

        if (HitInfo.HasAnyFlag(HitInfo.RageGain))
            attackRoundInfo.WriteInt32(RageGained);

        if (HitInfo.HasAnyFlag(HitInfo.Unk1))
        {
            attackRoundInfo.WriteUInt32(UnkState.State1);
            attackRoundInfo.WriteFloat(UnkState.State2);
            attackRoundInfo.WriteFloat(UnkState.State3);
            attackRoundInfo.WriteFloat(UnkState.State4);
            attackRoundInfo.WriteFloat(UnkState.State5);
            attackRoundInfo.WriteFloat(UnkState.State6);
            attackRoundInfo.WriteFloat(UnkState.State7);
            attackRoundInfo.WriteFloat(UnkState.State8);
            attackRoundInfo.WriteFloat(UnkState.State9);
            attackRoundInfo.WriteFloat(UnkState.State10);
            attackRoundInfo.WriteFloat(UnkState.State11);
            attackRoundInfo.WriteUInt32(UnkState.State12);
        }

        if (HitInfo.HasAnyFlag(HitInfo.Block | HitInfo.Unk12))
            attackRoundInfo.WriteFloat(Unk);

        attackRoundInfo.WriteUInt8((byte)ContentTuning.TuningType);
        attackRoundInfo.WriteUInt8(ContentTuning.TargetLevel);
        attackRoundInfo.WriteUInt8(ContentTuning.Expansion);
        attackRoundInfo.WriteInt16(ContentTuning.PlayerLevelDelta);
        attackRoundInfo.WriteInt8(ContentTuning.TargetScalingLevelDelta);
        attackRoundInfo.WriteFloat(ContentTuning.PlayerItemLevel);
        attackRoundInfo.WriteFloat(ContentTuning.TargetItemLevel);
        attackRoundInfo.WriteUInt32(ContentTuning.ScalingHealthItemLevelCurveID);
        attackRoundInfo.WriteUInt32((uint)ContentTuning.Flags);
        attackRoundInfo.WriteUInt32(ContentTuning.PlayerContentTuningID);
        attackRoundInfo.WriteUInt32(ContentTuning.TargetContentTuningID);

        WriteLogDataBit();
        FlushBits();
        WriteLogData();

        WorldPacket.WriteUInt32(attackRoundInfo.GetSize());
        WorldPacket.WriteBytes(attackRoundInfo);
    }
}
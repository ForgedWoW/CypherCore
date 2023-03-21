// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

class AttackerStateUpdate : CombatLogServerPacket
{
	public HitInfo hitInfo; // Flags
	public ObjectGuid AttackerGUID;
	public ObjectGuid VictimGUID;
	public int Damage;
	public int OriginalDamage;
	public int OverDamage = -1; // (damage - health) or -1 if unit is still alive
	public SubDamage? SubDmg;
	public byte VictimState;
	public uint AttackerState;
	public uint MeleeSpellID;
	public int BlockAmount;
	public int RageGained;
	public UnkAttackerState UnkState;
	public float Unk;
	public ContentTuningParams ContentTuning = new();
	public AttackerStateUpdate() : base(ServerOpcodes.AttackerStateUpdate, ConnectionType.Instance) { }

	public override void Write()
	{
		WorldPacket attackRoundInfo = new();
		attackRoundInfo.WriteUInt32((uint)hitInfo);
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

			if (hitInfo.HasAnyFlag(HitInfo.FullAbsorb | HitInfo.PartialAbsorb))
				attackRoundInfo.WriteInt32(SubDmg.Value.Absorbed);

			if (hitInfo.HasAnyFlag(HitInfo.FullResist | HitInfo.PartialResist))
				attackRoundInfo.WriteInt32(SubDmg.Value.Resisted);
		}

		attackRoundInfo.WriteUInt8(VictimState);
		attackRoundInfo.WriteUInt32(AttackerState);
		attackRoundInfo.WriteUInt32(MeleeSpellID);

		if (hitInfo.HasAnyFlag(HitInfo.Block))
			attackRoundInfo.WriteInt32(BlockAmount);

		if (hitInfo.HasAnyFlag(HitInfo.RageGain))
			attackRoundInfo.WriteInt32(RageGained);

		if (hitInfo.HasAnyFlag(HitInfo.Unk1))
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

		if (hitInfo.HasAnyFlag(HitInfo.Block | HitInfo.Unk12))
			attackRoundInfo.WriteFloat(Unk);

		attackRoundInfo.WriteUInt8((byte)ContentTuning.TuningType);
		attackRoundInfo.WriteUInt8(ContentTuning.TargetLevel);
		attackRoundInfo.WriteUInt8(ContentTuning.Expansion);
		attackRoundInfo.WriteInt16(ContentTuning.PlayerLevelDelta);
		attackRoundInfo.WriteInt8(ContentTuning.TargetScalingLevelDelta);
		attackRoundInfo.WriteFloat(ContentTuning.PlayerItemLevel);
		attackRoundInfo.WriteFloat(ContentTuning.TargetItemLevel);
		attackRoundInfo.WriteUInt16(ContentTuning.ScalingHealthItemLevelCurveID);
		attackRoundInfo.WriteUInt32((uint)ContentTuning.Flags);
		attackRoundInfo.WriteUInt32(ContentTuning.PlayerContentTuningID);
		attackRoundInfo.WriteUInt32(ContentTuning.TargetContentTuningID);

		WriteLogDataBit();
		FlushBits();
		WriteLogData();

		_worldPacket.WriteUInt32(attackRoundInfo.GetSize());
		_worldPacket.WriteBytes(attackRoundInfo);
	}
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.Networking.Packets;

class SpellExecuteLog : CombatLogServerPacket
{
	public ObjectGuid Caster;
	public uint SpellID;
	public List<SpellLogEffect> Effects = new();
	public SpellExecuteLog() : base(ServerOpcodes.SpellExecuteLog, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Caster);
		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WriteInt32(Effects.Count);

		foreach (var effect in Effects)
		{
			_worldPacket.WriteInt32(effect.Effect);

			_worldPacket.WriteInt32(effect.PowerDrainTargets.Count);
			_worldPacket.WriteInt32(effect.ExtraAttacksTargets.Count);
			_worldPacket.WriteInt32(effect.DurabilityDamageTargets.Count);
			_worldPacket.WriteInt32(effect.GenericVictimTargets.Count);
			_worldPacket.WriteInt32(effect.TradeSkillTargets.Count);
			_worldPacket.WriteInt32(effect.FeedPetTargets.Count);

			foreach (var powerDrainTarget in effect.PowerDrainTargets)
			{
				_worldPacket.WritePackedGuid(powerDrainTarget.Victim);
				_worldPacket.WriteUInt32(powerDrainTarget.Points);
				_worldPacket.WriteUInt32(powerDrainTarget.PowerType);
				_worldPacket.WriteFloat(powerDrainTarget.Amplitude);
			}

			foreach (var extraAttacksTarget in effect.ExtraAttacksTargets)
			{
				_worldPacket.WritePackedGuid(extraAttacksTarget.Victim);
				_worldPacket.WriteUInt32(extraAttacksTarget.NumAttacks);
			}

			foreach (var durabilityDamageTarget in effect.DurabilityDamageTargets)
			{
				_worldPacket.WritePackedGuid(durabilityDamageTarget.Victim);
				_worldPacket.WriteInt32(durabilityDamageTarget.ItemID);
				_worldPacket.WriteInt32(durabilityDamageTarget.Amount);
			}

			foreach (var genericVictimTarget in effect.GenericVictimTargets)
				_worldPacket.WritePackedGuid(genericVictimTarget.Victim);

			foreach (var tradeSkillTarget in effect.TradeSkillTargets)
				_worldPacket.WriteInt32(tradeSkillTarget.ItemID);


			foreach (var feedPetTarget in effect.FeedPetTargets)
				_worldPacket.WriteInt32(feedPetTarget.ItemID);
		}

		WriteLogDataBit();
		FlushBits();
		WriteLogData();
	}
}
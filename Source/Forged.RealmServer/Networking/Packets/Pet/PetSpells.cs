// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class PetSpells : ServerPacket
{
	public ObjectGuid PetGUID;
	public ushort CreatureFamily;
	public ushort Specialization;
	public uint TimeLimit;
	public ReactStates ReactState;
	public CommandStates CommandState;
	public byte Flag;

	public uint[] ActionButtons = new uint[10];

	public List<uint> Actions = new();
	public List<PetSpellCooldown> Cooldowns = new();
	public List<PetSpellHistory> SpellHistory = new();
	public PetSpells() : base(ServerOpcodes.PetSpellsMessage, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(PetGUID);
		_worldPacket.WriteUInt16(CreatureFamily);
		_worldPacket.WriteUInt16(Specialization);
		_worldPacket.WriteUInt32(TimeLimit);
		_worldPacket.WriteUInt16((ushort)((byte)CommandState | (Flag << 16)));
		_worldPacket.WriteUInt8((byte)ReactState);

		foreach (var actionButton in ActionButtons)
			_worldPacket.WriteUInt32(actionButton);

		_worldPacket.WriteInt32(Actions.Count);
		_worldPacket.WriteInt32(Cooldowns.Count);
		_worldPacket.WriteInt32(SpellHistory.Count);

		foreach (var action in Actions)
			_worldPacket.WriteUInt32(action);

		foreach (var cooldown in Cooldowns)
		{
			_worldPacket.WriteUInt32(cooldown.SpellID);
			_worldPacket.WriteUInt32(cooldown.Duration);
			_worldPacket.WriteUInt32(cooldown.CategoryDuration);
			_worldPacket.WriteFloat(cooldown.ModRate);
			_worldPacket.WriteUInt16(cooldown.Category);
		}

		foreach (var history in SpellHistory)
		{
			_worldPacket.WriteUInt32(history.CategoryID);
			_worldPacket.WriteUInt32(history.RecoveryTime);
			_worldPacket.WriteFloat(history.ChargeModRate);
			_worldPacket.WriteInt8(history.ConsumedCharges);
		}
	}
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.Networking.Packets;

class PartyMemberFullState : ServerPacket
{
	public bool ForEnemy;
	public ObjectGuid MemberGuid;
	public PartyMemberStats MemberStats = new();
	public PartyMemberFullState() : base(ServerOpcodes.PartyMemberFullState) { }

	public override void Write()
	{
		_worldPacket.WriteBit(ForEnemy);
		_worldPacket.FlushBits();

		MemberStats.Write(_worldPacket);
		_worldPacket.WritePackedGuid(MemberGuid);
	}

	public void Initialize(Player player)
	{
		ForEnemy = false;

		MemberGuid = player.GUID;

		// Status
		MemberStats.Status = GroupMemberOnlineStatus.Online;

		if (player.IsPvP)
			MemberStats.Status |= GroupMemberOnlineStatus.PVP;

		if (!player.IsAlive)
		{
			if (player.HasPlayerFlag(PlayerFlags.Ghost))
				MemberStats.Status |= GroupMemberOnlineStatus.Ghost;
			else
				MemberStats.Status |= GroupMemberOnlineStatus.Dead;
		}

		if (player.IsFFAPvP)
			MemberStats.Status |= GroupMemberOnlineStatus.PVPFFA;

		if (player.IsAFK)
			MemberStats.Status |= GroupMemberOnlineStatus.AFK;

		if (player.IsDND)
			MemberStats.Status |= GroupMemberOnlineStatus.DND;

		if (player.Vehicle1)
			MemberStats.Status |= GroupMemberOnlineStatus.Vehicle;

		// Level
		MemberStats.Level = (ushort)player.Level;

		// Health
		MemberStats.CurrentHealth = (int)player.Health;
		MemberStats.MaxHealth = (int)player.MaxHealth;

		// Power
		MemberStats.PowerType = (byte)player.DisplayPowerType;
		MemberStats.PowerDisplayID = 0;
		MemberStats.CurrentPower = (ushort)player.GetPower(player.DisplayPowerType);
		MemberStats.MaxPower = (ushort)player.GetMaxPower(player.DisplayPowerType);

		// Position
		MemberStats.ZoneID = (ushort)player.Zone;
		MemberStats.PositionX = (short)player.Location.X;
		MemberStats.PositionY = (short)(player.Location.Y);
		MemberStats.PositionZ = (short)(player.Location.Z);

		MemberStats.SpecID = (ushort)player.GetPrimarySpecialization();
		MemberStats.PartyType[0] = (sbyte)(player.PlayerData.PartyType & 0xF);
		MemberStats.PartyType[1] = (sbyte)(player.PlayerData.PartyType >> 4);
		MemberStats.WmoGroupID = 0;
		MemberStats.WmoDoodadPlacementID = 0;

		// Vehicle
		var vehicle = player.Vehicle1;

		if (vehicle != null)
		{
			var vehicleSeat = vehicle.GetSeatForPassenger(player);

			if (vehicleSeat != null)
				MemberStats.VehicleSeat = (int)vehicleSeat.Id;
		}

		// Auras
		foreach (var aurApp in player.VisibleAuras)
		{
			PartyMemberAuraStates aura = new();
			aura.SpellID = (int)aurApp.Base.Id;
			aura.ActiveFlags = (uint)aurApp.EffectMask.ToMask();
			aura.Flags = (byte)aurApp.Flags;

			if (aurApp.Flags.HasAnyFlag(AuraFlags.Scalable))
				foreach (var aurEff in aurApp.Base.AuraEffects)
					if (aurApp.HasEffect(aurEff.Value.EffIndex))
						aura.Points.Add((float)aurEff.Value.Amount);

			MemberStats.Auras.Add(aura);
		}

		// Phases
		PhasingHandler.FillPartyMemberPhase(MemberStats.Phases, player.PhaseShift);

		// Pet
		if (player.CurrentPet)
		{
			var pet = player.CurrentPet;

			MemberStats.PetStats = new PartyMemberPetStats();

			MemberStats.PetStats.GUID = pet.GUID;
			MemberStats.PetStats.Name = pet.GetName();
			MemberStats.PetStats.ModelId = (short)pet.DisplayId;

			MemberStats.PetStats.CurrentHealth = (int)pet.Health;
			MemberStats.PetStats.MaxHealth = (int)pet.MaxHealth;

			foreach (var aurApp in pet.VisibleAuras)
			{
				PartyMemberAuraStates aura = new();

				aura.SpellID = (int)aurApp.Base.Id;
				aura.ActiveFlags = (uint)aurApp.EffectMask.ToMask();
				aura.Flags = (byte)aurApp.Flags;

				if (aurApp.Flags.HasAnyFlag(AuraFlags.Scalable))
					foreach (var aurEff in aurApp.Base.AuraEffects)
						if (aurApp.HasEffect(aurEff.Value.EffIndex))
							aura.Points.Add((float)aurEff.Value.Amount);

				MemberStats.PetStats.Auras.Add(aura);
			}
		}
	}
}
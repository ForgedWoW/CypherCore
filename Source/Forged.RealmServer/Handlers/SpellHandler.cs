// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Database;
using Forged.RealmServer.DataStorage;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Loots;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets;
using Forged.RealmServer.Scripting.Interfaces.IItem;
using Forged.RealmServer.Spells;

namespace Forged.RealmServer;

public partial class WorldSession
{
	[WorldPacketHandler(ClientOpcodes.CancelGrowthAura, Processing = PacketProcessing.Inplace)]
	void HandleCancelGrowthAura(CancelGrowthAura cancelGrowthAura)
	{
		Player.RemoveAurasByType(AuraType.ModScale,
								aurApp =>
								{
									var spellInfo = aurApp.Base.SpellInfo;

									return !spellInfo.HasAttribute(SpellAttr0.NoAuraCancel) && spellInfo.IsPositive && !spellInfo.IsPassive;
								});
	}

	[WorldPacketHandler(ClientOpcodes.PetCancelAura, Processing = PacketProcessing.Inplace)]
	void HandlePetCancelAura(PetCancelAura packet)
	{
		if (!Global.SpellMgr.HasSpellInfo(packet.SpellID, Difficulty.None))
		{
			Log.outError(LogFilter.Network, "WORLD: unknown PET spell id {0}", packet.SpellID);

			return;
		}

		var pet = ObjectAccessor.GetCreatureOrPetOrVehicle(_player, packet.PetGUID);

		if (pet == null)
		{
			Log.outError(LogFilter.Network, "HandlePetCancelAura: Attempt to cancel an aura for non-existant {0} by player '{1}'", packet.PetGUID.ToString(), Player.GetName());

			return;
		}

		if (pet != Player.GetGuardianPet() && pet != Player.Charmed)
		{
			Log.outError(LogFilter.Network, "HandlePetCancelAura: {0} is not a pet of player '{1}'", packet.PetGUID.ToString(), Player.GetName());

			return;
		}

		if (!pet.IsAlive)
		{
			pet.SendPetActionFeedback(PetActionFeedback.Dead, 0);

			return;
		}

		pet.RemoveOwnedAura(packet.SpellID, ObjectGuid.Empty, AuraRemoveMode.Cancel);
	}

	[WorldPacketHandler(ClientOpcodes.UpdateMissileTrajectory)]
	void HandleUpdateMissileTrajectory(UpdateMissileTrajectory packet)
	{
		var caster = Global.ObjAccessor.GetUnit(Player, packet.Guid);
		var spell = caster ? caster.GetCurrentSpell(CurrentSpellTypes.Generic) : null;

		if (!spell || spell.SpellInfo.Id != packet.SpellID || spell.CastId != packet.CastID || !spell.Targets.HasDst || !spell.Targets.HasSrc)
			return;

		var pos = spell.Targets.SrcPos;
		pos.Relocate(packet.FirePos);
		spell.Targets.ModSrc(pos);

		pos = spell.Targets.DstPos;
		pos.Relocate(packet.ImpactPos);
		spell.Targets.ModDst(pos);

		spell.Targets.Pitch = packet.Pitch;
		spell.Targets.Speed = packet.Speed;

		if (packet.Status != null)
			Player.ValidateMovementInfo(packet.Status);
		/*public uint opcode;
			recvPacket >> opcode;
			recvPacket.SetOpcode(CMSG_MOVE_STOP); // always set to CMSG_MOVE_STOP in client SetOpcode
			//HandleMovementOpcodes(recvPacket);*/
	}
}
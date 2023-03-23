// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Forged.RealmServer.Movement;
using Forged.RealmServer.Scripting.Interfaces.ICreature;
using Forged.RealmServer.Scripting.Interfaces.IGameObject;

namespace Forged.RealmServer.AI;

public class AISelector
{
	public static CreatureAI SelectAI(Creature creature)
	{
		if (creature.IsPet)
			return new PetAI(creature);

		//scriptname in db
		var scriptedAI = Global.ScriptMgr.RunScriptRet<ICreatureGetAI, CreatureAI>(p => p.GetAI(creature), creature.GetScriptId());

		if (scriptedAI != null)
			return scriptedAI;

		switch (creature.Template.AIName)
		{
			case "AggressorAI":
				return new AggressorAI(creature);
			case "ArcherAI":
				return new ArcherAI(creature);
			case "CombatAI":
				return new CombatAI(creature);
			case "CritterAI":
				return new CritterAI(creature);
			case "GuardAI":
				return new GuardAI(creature);
			case "NullCreatureAI":
				return new NullCreatureAI(creature);
			case "PassiveAI":
				return new PassiveAI(creature);
			case "PetAI":
				return new PetAI(creature);
			case "ReactorAI":
				return new ReactorAI(creature);
			case "SmartAI":
				return new SmartAI(creature);
			case "TotemAI":
				return new TotemAI(creature);
			case "TriggerAI":
				return new TriggerAI(creature);
			case "TurretAI":
				return new TurretAI(creature);
			case "VehicleAI":
				return new VehicleAI(creature);
		}

		// select by NPC flags
		if (creature.IsVehicle)
		{
			return new VehicleAI(creature);
		}
		else if (creature.HasUnitTypeMask(UnitTypeMask.ControlableGuardian) && ((Guardian)creature).OwnerUnit.IsTypeId(TypeId.Player))
		{
			return new PetAI(creature);
		}
		else if (creature.HasNpcFlag(NPCFlags.SpellClick))
		{
			return new NullCreatureAI(creature);
		}
		else if (creature.IsGuard)
		{
			return new GuardAI(creature);
		}
		else if (creature.HasUnitTypeMask(UnitTypeMask.ControlableGuardian))
		{
			return new PetAI(creature);
		}
		else if (creature.IsTotem)
		{
			return new TotemAI(creature);
		}
		else if (creature.IsTrigger)
		{
			if (creature.Spells[0] != 0)
				return new TriggerAI(creature);
			else
				return new NullCreatureAI(creature);
		}
		else if (creature.IsCritter && !creature.HasUnitTypeMask(UnitTypeMask.Guardian))
		{
			return new CritterAI(creature);
		}

		if (!creature.IsCivilian && !creature.IsNeutralToAll())
			return new AggressorAI(creature);

		if (creature.IsCivilian || creature.IsNeutralToAll())
			return new ReactorAI(creature);

		return new NullCreatureAI(creature);
	}

	public static MovementGenerator SelectMovementGenerator(Unit unit)
	{
		var type = unit.GetDefaultMovementType();
		var creature = unit.AsCreature;

		if (creature != null && creature.PlayerMovingMe1 == null)
			type = creature.GetDefaultMovementType();

		return type switch
		{
			MovementGeneratorType.Random   => new RandomMovementGenerator(),
			MovementGeneratorType.Waypoint => new WaypointMovementGenerator(),
			MovementGeneratorType.Idle     => new IdleMovementGenerator(),
			_                              => null,
		};
	}

	public static GameObjectAI SelectGameObjectAI(GameObject go)
	{
		// scriptname in db
		var scriptedAI = Global.ScriptMgr.RunScriptRet<IGameObjectGetAI, GameObjectAI>(p => p.GetAI(go), go.ScriptId);

		if (scriptedAI != null)
			return scriptedAI;

		return go.AiName switch
		{
			"SmartGameObjectAI" => new SmartGameObjectAI(go),
			_                   => new GameObjectAI(go),
		};
	}
}
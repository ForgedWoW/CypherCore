// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Forged.RealmServer.AI;

public class VehicleAI : CreatureAI
{
	const int VEHICLE_CONDITION_CHECK_TIME = 1000;
	const int VEHICLE_DISMISS_TIME = 5000;

	bool _hasConditions;
	uint _conditionsTimer;
	bool _doDismiss;
	uint _dismissTimer;

	public VehicleAI(Creature creature) : base(creature)
	{
		_conditionsTimer = VEHICLE_CONDITION_CHECK_TIME;
		LoadConditions();
		_doDismiss = false;
		_dismissTimer = VEHICLE_DISMISS_TIME;
	}

	public override void UpdateAI(uint diff)
	{
		CheckConditions(diff);

		if (_doDismiss)
		{
			if (_dismissTimer < diff)
			{
				_doDismiss = false;
				Me.DespawnOrUnsummon();
			}
			else
			{
				_dismissTimer -= diff;
			}
		}
	}

	public override void MoveInLineOfSight(Unit who) { }

	public override void AttackStart(Unit victim) { }

	public override void OnCharmed(bool isNew)
	{
		var charmed = Me.IsCharmed;

		if (!Me.VehicleKit1.IsVehicleInUse() && !charmed && _hasConditions) //was used and has conditions
			_doDismiss = true;                                              //needs reset
		else if (charmed)
			_doDismiss = false; //in use again

		_dismissTimer = VEHICLE_DISMISS_TIME; //reset timer
	}

	void LoadConditions()
	{
		_hasConditions = Global.ConditionMgr.HasConditionsForNotGroupedEntry(ConditionSourceType.CreatureTemplateVehicle, Me.Entry);
	}

	void CheckConditions(uint diff)
	{
		if (!_hasConditions)
			return;

		if (_conditionsTimer <= diff)
		{
			var vehicleKit = Me.VehicleKit1;

			if (vehicleKit)
				foreach (var pair in vehicleKit.Seats)
				{
					var passenger = Global.ObjAccessor.GetUnit(Me, pair.Value.Passenger.Guid);

					if (passenger)
					{
						var player = passenger.AsPlayer;

						if (player)
							if (!Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.CreatureTemplateVehicle, Me.Entry, player, Me))
							{
								player.ExitVehicle();

								return; //check other pessanger in next tick
							}
					}
				}

			_conditionsTimer = VEHICLE_CONDITION_CHECK_TIME;
		}
		else
		{
			_conditionsTimer -= diff;
		}
	}
}
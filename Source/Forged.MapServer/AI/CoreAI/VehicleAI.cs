// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.AI.CoreAI;

public class VehicleAI : CreatureAI
{
    private const int VehicleConditionCheckTime = 1000;
    private const int VehicleDismissTime = 5000;

    private uint _conditionsTimer;
    private uint _dismissTimer;
    private bool _doDismiss;
    private bool _hasConditions;

    public VehicleAI(Creature creature) : base(creature)
    {
        _conditionsTimer = VehicleConditionCheckTime;
        LoadConditions();
        _doDismiss = false;
        _dismissTimer = VehicleDismissTime;
    }

    public override void AttackStart(Unit victim) { }

    public override void MoveInLineOfSight(Unit who) { }

    public override void OnCharmed(bool isNew)
    {
        var charmed = Me.IsCharmed;

        if (!Me.VehicleKit.IsVehicleInUse && !charmed && _hasConditions) //was used and has conditions
            _doDismiss = true;                                             //needs reset
        else if (charmed)
            _doDismiss = false; //in use again

        _dismissTimer = VehicleDismissTime; //reset timer
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
                _dismissTimer -= diff;
        }
    }

    private void CheckConditions(uint diff)
    {
        if (!_hasConditions)
            return;

        if (_conditionsTimer <= diff)
        {
            var vehicleKit = Me.VehicleKit;

            if (vehicleKit != null)
                foreach (var pair in vehicleKit.Seats)
                {
                    var passenger = Me.ObjectAccessor.GetUnit(Me, pair.Value.Passenger.Guid);

                    if (passenger == null)
                        continue;

                    var player = passenger.AsPlayer;

                    if (player == null)
                        continue;

                    if (Me.ConditionManager.IsObjectMeetingNotGroupedConditions(ConditionSourceType.CreatureTemplateVehicle, Me.Entry, player, Me))
                        continue;

                    player.ExitVehicle();

                    return; //check other pessanger in next tick
                }

            _conditionsTimer = VehicleConditionCheckTime;
        }
        else
            _conditionsTimer -= diff;
    }

    private void LoadConditions()
    {
        _hasConditions = Me.ConditionManager.HasConditionsForNotGroupedEntry(ConditionSourceType.CreatureTemplateVehicle, Me.Entry);
    }
}
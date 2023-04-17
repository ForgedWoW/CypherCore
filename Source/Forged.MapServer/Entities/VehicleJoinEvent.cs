// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Movement;
using Forged.MapServer.Scripting.Interfaces.IVehicle;
using Framework.Constants;
using Framework.Dynamic;
using Serilog;

namespace Forged.MapServer.Entities;

public class VehicleJoinEvent : BasicEvent
{
    public Unit Passenger;
    public KeyValuePair<sbyte, VehicleSeat> Seat;
    public Vehicle Target;

    public VehicleJoinEvent(Vehicle v, Unit u)
    {
        Target = v;
        Passenger = u;
        Seat = Target.Seats.Last();
    }

    public override void Abort(ulong e_time)
    {
        // Check if the Vehicle was already uninstalled, in which case all auras were removed already
        if (Target != null)
        {
            Log.Logger.Debug("Passenger GuidLow: {0}, Entry: {1}, board on vehicle GuidLow: {2}, Entry: {3} SeatId: {4} cancelled",
                             Passenger.GUID.ToString(),
                             Passenger.Entry,
                             Target.GetBase().GUID.ToString(),
                             Target.GetBase().Entry,
                             Seat.Key);

            // Remove the pending event when Abort was called on the event directly
            Target.RemovePendingEvent(this);

            // @SPELL_AURA_CONTROL_VEHICLE auras can be applied even when the passenger is not (yet) on the vehicle.
            // When this code is triggered it means that something went wrong in @Vehicle.AddPassenger, and we should remove
            // the aura manually.
            Target.GetBase().RemoveAurasByType(AuraType.ControlVehicle, Passenger.GUID);
        }
        else
            Log.Logger.Debug("Passenger GuidLow: {0}, Entry: {1}, board on uninstalled vehicle SeatId: {2} cancelled",
                             Passenger.GUID.ToString(),
                             Passenger.Entry,
                             Seat.Key);

        if (Passenger.Location.IsInWorld && Passenger.HasUnitTypeMask(UnitTypeMask.Accessory))
            Passenger.AsCreature.DespawnOrUnsummon();
    }

    public override bool Execute(ulong etime, uint pTime)
    {
        var vehicleAuras = Target.GetBase().GetAuraEffectsByType(AuraType.ControlVehicle);
        var aurEffect = vehicleAuras.Find(aurEff => aurEff.CasterGuid == Passenger.GUID);

        var aurApp = aurEffect.Base.GetApplicationOfTarget(Target.GetBase().GUID);

        Target.RemovePendingEventsForSeat(Seat.Key);
        Target.RemovePendingEventsForPassenger(Passenger);

        // Passenger might've died in the meantime - abort if this is the case
        if (!Passenger.IsAlive)
        {
            Abort(0);

            return true;
        }

        //It's possible that multiple vehicle join
        //events are executed in the same update
        if (Passenger.Vehicle != null)
            Passenger.ExitVehicle();

        Passenger.Vehicle = Target;
        Seat.Value.Passenger.Guid = Passenger.GUID;
        Seat.Value.Passenger.IsUninteractible = Passenger.HasUnitFlag(UnitFlags.Uninteractible);
        Seat.Value.Passenger.IsGravityDisabled = Passenger.HasUnitMovementFlag(MovementFlag.DisableGravity);

        if (Seat.Value.SeatInfo.CanEnterOrExit())
        {
            --Target.UsableSeatNum;

            if (Target.UsableSeatNum == 0)
            {
                if (Target.GetBase().IsTypeId(TypeId.Player))
                    Target.GetBase().RemoveNpcFlag(NPCFlags.PlayerVehicle);
                else
                    Target.GetBase().RemoveNpcFlag(NPCFlags.SpellClick);
            }
        }

        Passenger.InterruptNonMeleeSpells(false);
        Passenger.RemoveAurasByType(AuraType.Mounted);

        var veSeat = Seat.Value.SeatInfo;
        var veSeatAddon = Seat.Value.SeatAddon;

        var player = Passenger.AsPlayer;

        if (player != null)
        {
            // drop Id
            var bg = player.Battleground;

            if (bg)
                bg.EventPlayerDroppedFlag(player);

            player.StopCastingCharm();
            player.StopCastingBindSight();
            player.SendOnCancelExpectedVehicleRideAura();

            if (!veSeat.HasFlag(VehicleSeatFlagsB.KeepPet))
                player.UnsummonPetTemporaryIfAny();
        }

        if (veSeat.HasFlag(VehicleSeatFlags.DisableGravity))
            Passenger.SetDisableGravity(true);

        var o = veSeatAddon?.SeatOrientationOffset ?? 0.0f;
        var x = veSeat.AttachmentOffset.X;
        var y = veSeat.AttachmentOffset.Y;
        var z = veSeat.AttachmentOffset.Z;

        Passenger.MovementInfo.Transport.Pos.Relocate(x, y, z, o);
        Passenger.MovementInfo.Transport.Time = 0;
        Passenger.MovementInfo.Transport.Seat = Seat.Key;
        Passenger.MovementInfo.Transport.Guid = Target.GetBase().GUID;

        if (Target.GetBase().IsTypeId(TypeId.Unit) && Passenger.IsTypeId(TypeId.Player) && Seat.Value.SeatInfo.HasFlag(VehicleSeatFlags.CanControl))
            // handles SMSG_CLIENT_CONTROL
            if (!Target.GetBase().SetCharmedBy(Passenger, CharmType.Vehicle, aurApp))
            {
                // charming failed, probably aura was removed by relocation/scripts/whatever
                Abort(0);

                return true;
            }

        Passenger.SendClearTarget();                   // SMSG_BREAK_TARGET
        Passenger.SetControlled(true, UnitState.Root); // SMSG_FORCE_ROOT - In some cases we send SMSG_SPLINE_MOVE_ROOT here (for creatures)
        // also adds MOVEMENTFLAG_ROOT

        var initializer = (MoveSplineInit init) =>
        {
            init.DisableTransportPathTransformations();
            init.MoveTo(x, y, z, false, true);
            init.SetFacing(o);
            init.SetTransportEnter();
        };

        Passenger.MotionMaster.LaunchMoveSpline(initializer, EventId.VehicleBoard, MovementGeneratorPriority.Highest);

        foreach (var (_, threatRef) in Passenger.GetThreatManager().ThreatenedByMeList)
            threatRef.Owner.GetThreatManager().AddThreat(Target.GetBase(), threatRef.Threat, null, true, true);

        var creature = Target.GetBase().AsCreature;

        if (creature != null)
        {
            var ai = creature.AI;

            ai?.PassengerBoarded(Passenger, Seat.Key, true);

            ScriptManager.RunScript<IVehicleOnAddPassenger>(p => p.OnAddPassenger(Target, Passenger, Seat.Key), Target.GetBase().AsCreature.GetScriptId());

            // Actually quite a redundant hook. Could just use OnAddPassenger and check for unit typemask inside script.
            if (Passenger.HasUnitTypeMask(UnitTypeMask.Accessory))
                ScriptManager.RunScript<IVehicleOnInstallAccessory>(p => p.OnInstallAccessory(Target, Passenger.AsCreature), Target.GetBase().AsCreature.GetScriptId());
        }

        return true;
    }
}
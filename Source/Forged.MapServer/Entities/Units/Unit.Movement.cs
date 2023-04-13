// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Forged.MapServer.DataStorage.Structs.L;
using Forged.MapServer.DataStorage.Structs.M;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Movement;
using Forged.MapServer.Movement.Generators;
using Forged.MapServer.Networking.Packets.Movement;
using Forged.MapServer.Networking.Packets.Vehicle;
using Forged.MapServer.Spells;
using Framework.Constants;
using Framework.Util;
using Serilog;

namespace Forged.MapServer.Entities.Units;

public partial class Unit
{
    public void AddExtraUnitMovementFlag2(MovementFlags3 f)
    {
        MovementInfo.AddExtraMovementFlag2(f);
    }

    public void AddUnitMovementFlag(MovementFlag f)
    {
        MovementInfo.AddMovementFlag(f);
    }

    public void AddUnitMovementFlag2(MovementFlag2 f)
    {
        MovementInfo.AddMovementFlag2(f);
    }

    public bool CanFreeMove()
    {
        return !HasUnitState(UnitState.Confused |
                             UnitState.Fleeing |
                             UnitState.InFlight |
                             UnitState.Root |
                             UnitState.Stunned |
                             UnitState.Distracted) &&
               OwnerGUID.IsEmpty;
    }

    public bool CreateVehicleKit(uint id, uint creatureEntry, bool loading = false)
    {
        if (!CliDB.VehicleStorage.TryGetValue(id, out var vehInfo))
            return false;

        VehicleKit = new Vehicle(this, vehInfo, creatureEntry);
        UpdateFlag.Vehicle = true;
        UnitTypeMask |= UnitTypeMask.Vehicle;

        if (!loading)
            SendSetVehicleRecId(id);

        return true;
    }

    public void DisableSpline()
    {
        MovementInfo.RemoveMovementFlag(MovementFlag.Forward);
        MoveSpline.Interrupt();
    }

    public void Dismount()
    {
        if (!IsMounted)
            return;

        MountDisplayId = 0;
        RemoveUnitFlag(UnitFlags.Mount);

        var thisPlayer = AsPlayer;

        thisPlayer?.SendMovementSetCollisionHeight(thisPlayer.CollisionHeight, UpdateCollisionHeightReason.Mount);

        // dismount as a vehicle
        if (IsTypeId(TypeId.Player) && VehicleKit != null)
            // Remove vehicle from player
            RemoveVehicleKit();

        RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Dismount);

        // only resummon old pet if the player is already added to a map
        // this prevents adding a pet to a not created map which would otherwise cause a crash
        // (it could probably happen when logging in after a previous crash)
        var player = AsPlayer;

        if (player != null)
        {
            var pPet = player.CurrentPet;

            if (pPet != null)
            {
                if (pPet.HasUnitFlag(UnitFlags.Stunned) && !pPet.HasUnitState(UnitState.Stunned))
                    pPet.RemoveUnitFlag(UnitFlags.Stunned);
            }
            else
            {
                player.ResummonPetTemporaryUnSummonedIfAny();
            }

            // if we have charmed npc, remove stun also
            var charm = player.Charmed;

            if (charm)
                if (charm.TypeId == TypeId.Unit && charm.HasUnitFlag(UnitFlags.Stunned) && !charm.HasUnitState(UnitState.Stunned))
                    charm.RemoveUnitFlag(UnitFlags.Stunned);
        }
    }

    public virtual MovementGeneratorType GetDefaultMovementType()
    {
        return MovementGeneratorType.Idle;
    }

    public MovementFlags3 GetExtraUnitMovementFlags2()
    {
        return MovementInfo.GetExtraMovementFlags2();
    }

    public MountCapabilityRecord GetMountCapability(uint mountType)
    {
        if (mountType == 0)
            return null;

        var capabilities = DB2Manager.GetMountCapabilities(mountType);

        if (capabilities == null)
            return null;

        var areaId = Location.Area;
        uint ridingSkill = 5000;
        AreaMountFlags mountFlags = 0;

        if (IsTypeId(TypeId.Player))
            ridingSkill = AsPlayer.GetSkillValue(SkillType.Riding);

        if (HasAuraType(AuraType.MountRestrictions))
        {
            foreach (var auraEffect in GetAuraEffectsByType(AuraType.MountRestrictions))
                mountFlags |= (AreaMountFlags)auraEffect.MiscValue;
        }
        else
        {
            if (CliDB.AreaTableStorage.TryGetValue(areaId, out var areaTable))
                mountFlags = (AreaMountFlags)areaTable.MountFlags;
        }

        var liquidStatus = Location.Map.GetLiquidStatus(Location.PhaseShift, Location.X, Location.Y, Location.Z, LiquidHeaderTypeFlags.AllLiquids);
        var isSubmerged = liquidStatus.HasAnyFlag(ZLiquidStatus.UnderWater) || HasUnitMovementFlag(MovementFlag.Swimming);
        var isInWater = liquidStatus.HasAnyFlag(ZLiquidStatus.InWater | ZLiquidStatus.UnderWater);

        foreach (var mountTypeXCapability in capabilities)
        {
            if (!CliDB.MountCapabilityStorage.TryGetValue(mountTypeXCapability.MountCapabilityID, out var mountCapability))
                continue;

            if (ridingSkill < mountCapability.ReqRidingSkill)
                continue;

            if (!mountCapability.Flags.HasAnyFlag(MountCapabilityFlags.IgnoreRestrictions))
            {
                if (mountCapability.Flags.HasAnyFlag(MountCapabilityFlags.Ground) && !mountFlags.HasAnyFlag(AreaMountFlags.GroundAllowed))
                    continue;

                if (mountCapability.Flags.HasAnyFlag(MountCapabilityFlags.Flying) && !mountFlags.HasAnyFlag(AreaMountFlags.FlyingAllowed))
                    continue;

                if (mountCapability.Flags.HasAnyFlag(MountCapabilityFlags.Float) && !mountFlags.HasAnyFlag(AreaMountFlags.FloatAllowed))
                    continue;

                if (mountCapability.Flags.HasAnyFlag(MountCapabilityFlags.Underwater) && !mountFlags.HasAnyFlag(AreaMountFlags.UnderwaterAllowed))
                    continue;
            }

            if (!isSubmerged)
            {
                if (!isInWater)
                {
                    // player is completely out of water
                    if (!mountCapability.Flags.HasAnyFlag(MountCapabilityFlags.Ground))
                        continue;
                }
                // player is on water surface
                else if (!mountCapability.Flags.HasAnyFlag(MountCapabilityFlags.Float))
                {
                    continue;
                }
            }
            else if (isInWater)
            {
                if (!mountCapability.Flags.HasAnyFlag(MountCapabilityFlags.Underwater))
                    continue;
            }
            else if (!mountCapability.Flags.HasAnyFlag(MountCapabilityFlags.Float))
            {
                continue;
            }

            if (mountCapability.ReqMapID != -1 &&
                Location.MapId != mountCapability.ReqMapID &&
                Location.Map.Entry.CosmeticParentMapID != mountCapability.ReqMapID &&
                Location.Map.Entry.ParentMapID != mountCapability.ReqMapID)
                continue;

            if (mountCapability.ReqAreaID != 0 && !DB2Manager.IsInArea(areaId, mountCapability.ReqAreaID))
                continue;

            if (mountCapability.ReqSpellAuraID != 0 && !HasAura(mountCapability.ReqSpellAuraID))
                continue;

            if (mountCapability.ReqSpellKnownID != 0 && !HasSpell(mountCapability.ReqSpellKnownID))
                continue;

            var thisPlayer = AsPlayer;

            if (thisPlayer != null)
            {
                if (CliDB.PlayerConditionStorage.TryGetValue((uint)mountCapability.PlayerConditionID, out var playerCondition))
                    if (!ConditionManager.IsPlayerMeetingCondition(thisPlayer, playerCondition))
                        continue;
            }

            return mountCapability;
        }

        return null;
    }

    public float GetSpeed(UnitMoveType mtype)
    {
        return SpeedRate[(int)mtype] * (ControlledByPlayer ? SharedConst.playerBaseMoveSpeed[(int)mtype] : SharedConst.baseMoveSpeed[(int)mtype]);
    }

    public float GetSpeedRate(UnitMoveType mtype)
    {
        return SpeedRate[(int)mtype];
    }

    //Transport
    public override ObjectGuid GetTransGUID()
    {
        if (Vehicle != null)
            return VehicleBase.GUID;

        if (Transport != null)
            return Transport.GetTransportGUID();

        return ObjectGuid.Empty;
    }

    public MovementFlag GetUnitMovementFlags()
    {
        return MovementInfo.MovementFlags;
    }

    public MovementFlag2 GetUnitMovementFlags2()
    {
        return MovementInfo.GetMovementFlags2();
    }

    public bool HasExtraUnitMovementFlag2(MovementFlags3 f)
    {
        return MovementInfo.HasExtraMovementFlag2(f);
    }

    public bool HasUnitMovementFlag(MovementFlag f)
    {
        return MovementInfo.HasMovementFlag(f);
    }

    public bool HasUnitMovementFlag2(MovementFlag2 f)
    {
        return MovementInfo.HasMovementFlag2(f);
    }

    public bool IsInAccessiblePlaceFor(Creature c)
    {
        if (Location.IsInWater)
            return c.CanEnterWater;
        else
            return c.CanWalk || c.CanFly;
    }

    public bool IsInBackInMap(Unit target, float distance, float arc = MathFunctions.PI)
    {
        return Location.IsWithinDistInMap(target, distance) && !Location.HasInArc(MathFunctions.TWO_PI - arc, target.Location);
    }

    public bool IsInFrontInMap(Unit target, float distance, float arc = MathFunctions.PI)
    {
        return Location.IsWithinDistInMap(target, distance) && Location.HasInArc(arc, target.Location);
    }

    public bool IsWithinBoundaryRadius(Unit obj)
    {
        if (!obj || !Location.IsInMap(obj) || !Location.InSamePhase(obj))
            return false;

        var objBoundaryRadius = Math.Max(obj.BoundingRadius, SharedConst.MinMeleeReach);

        return Location.IsInDist(obj.Location, objBoundaryRadius);
    }

    public bool IsWithinCombatRange(Unit obj, float dist2Compare)
    {
        if (!obj || !Location.IsInMap(obj) || !Location.InSamePhase(obj))
            return false;

        var dx = Location.X - obj.Location.X;
        var dy = Location.Y - obj.Location.Y;
        var dz = Location.Z - obj.Location.Z;
        var distsq = dx * dx + dy * dy + dz * dz;

        var sizefactor = CombatReach + obj.CombatReach;
        var maxdist = dist2Compare + sizefactor;

        return distsq < maxdist * maxdist;
    }

    public void JumpTo(float speedXy, float speedZ, float angle, Position dest = null)
    {
        if (dest != null)
            angle += Location.GetRelativeAngle(dest);

        if (IsTypeId(TypeId.Unit))
        {
            MotionMaster.MoveJumpTo(angle, speedXy, speedZ);
        }
        else
        {
            var vcos = (float)Math.Cos(angle + Location.Orientation);
            var vsin = (float)Math.Sin(angle + Location.Orientation);
            SendMoveKnockBack(AsPlayer, speedXy, -speedZ, vcos, vsin);
        }
    }

    public void JumpTo(WorldObject obj, float speedZ, bool withOrientation = false)
    {
        var pos = new Position();
        obj.Location.GetContactPoint(this, pos);
        var speedXy = Location.GetExactDist2d(pos.X, pos.Y) * 10.0f / speedZ;
        pos.Orientation = Location.GetAbsoluteAngle(obj.Location);
        MotionMaster.MoveJump(pos, speedXy, speedZ, EventId.Jump, withOrientation);
    }

    public void KnockbackFrom(Position origin, float speedXy, float speedZ, SpellEffectExtraData spellEffectExtraData = null)
    {
        var player = AsPlayer;

        if (!player)
        {
            var charmer = Charmer;

            if (charmer)
            {
                player = charmer.AsPlayer;

                if (player && player.UnitBeingMoved != this)
                    player = null;
            }
        }

        if (!player)
        {
            MotionMaster.MoveKnockbackFrom(origin, speedXy, speedZ, spellEffectExtraData);
        }
        else
        {
            var o = Location == origin ? Location.Orientation + MathF.PI : origin.GetRelativeAngle(Location);

            if (speedXy < 0)
            {
                speedXy = -speedXy;
                o = o - MathF.PI;
            }

            var vcos = MathF.Cos(o);
            var vsin = MathF.Sin(o);
            SendMoveKnockBack(player, speedXy, -speedZ, vcos, vsin);
        }
    }

    public void MonsterMoveWithSpeed(float x, float y, float z, float speed, bool generatePath = false, bool forceDestination = false)
    {
        void Initializer(MoveSplineInit init)
        {
            init.MoveTo(x, y, z, generatePath, forceDestination);
            init.SetVelocity(speed);
        }

        MotionMaster.LaunchMoveSpline(Initializer, 0, MovementGeneratorPriority.Normal, MovementGeneratorType.Point);
    }

    public void Mount(uint mount, uint vehicleId = 0, uint creatureEntry = 0)
    {
        RemoveAurasByType(AuraType.CosmeticMounted);

        if (mount != 0)
            MountDisplayId = mount;

        SetUnitFlag(UnitFlags.Mount);

        var player = AsPlayer;

        if (player != null)
        {
            // mount as a vehicle
            if (vehicleId != 0)
                if (CreateVehicleKit(vehicleId, creatureEntry))
                {
                    player.SendOnCancelExpectedVehicleRideAura();

                    // mounts can also have accessories
                    VehicleKit.InstallAllAccessories(false);
                }

            // unsummon pet
            var pet = player.CurrentPet;

            if (pet != null)
            {
                var bg = AsPlayer.Battleground;

                // don't unsummon pet in arena but SetFlag UNIT_FLAG_STUNNED to disable pet's interface
                if (bg && bg.IsArena)
                    pet.SetUnitFlag(UnitFlags.Stunned);
                else
                    player.UnsummonPetTemporaryIfAny();
            }

            // if we have charmed npc, stun him also (everywhere)
            var charm = player.Charmed;

            if (charm)
                if (charm.TypeId == TypeId.Unit)
                    charm.SetUnitFlag(UnitFlags.Stunned);

            player.SendMovementSetCollisionHeight(player.CollisionHeight, UpdateCollisionHeightReason.Mount);
        }

        RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Mount);
    }

    public void NearTeleportTo(float x, float y, float z, float orientation, bool casting = false)
    {
        NearTeleportTo(new Position(x, y, z, orientation), casting);
    }

    public void NearTeleportTo(Position pos, bool casting = false)
    {
        DisableSpline();

        if (IsTypeId(TypeId.Player))
        {
            WorldLocation target = new(Location.MapId, pos);
            AsPlayer.TeleportTo(target, TeleportToOptions.NotLeaveTransport | TeleportToOptions.NotLeaveCombat | TeleportToOptions.NotUnSummonPet | (casting ? TeleportToOptions.Spell : 0));
        }
        else
        {
            SendTeleportPacket(pos);
            UpdatePosition(pos, true);
            UpdateObjectVisibility();
        }
    }

    public void PauseMovement(uint timer = 0, MovementSlot slot = 0, bool forced = true)
    {
        if (MotionMaster.IsInvalidMovementSlot(slot))
            return;

        var movementGenerator = MotionMaster.GetCurrentMovementGenerator(slot);

        movementGenerator?.Pause(timer);

        if (forced && MotionMaster.GetCurrentSlot() == slot)
            StopMoving();
    }

    public void ProcessPositionDataChanged(PositionFullTerrainStatus data)
    {
        var oldLiquidStatus = Location.LiquidStatus;
        Location.ProcessPositionDataChanged(data);
        ProcessTerrainStatusUpdate(oldLiquidStatus, data.LiquidInfo);
    }

    public virtual void ProcessTerrainStatusUpdate(ZLiquidStatus oldLiquidStatus, LiquidData newLiquidData)
    {
        if (!ControlledByPlayer)
            return;

        // remove appropriate auras if we are swimming/not swimming respectively
        if (Location.IsInWater)
            RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.UnderWater);
        else
            RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.AboveWater);

        // liquid aura handling
        LiquidTypeRecord curLiquid = null;

        if (Location.IsInWater && newLiquidData != null)
            curLiquid = CliDB.LiquidTypeStorage.LookupByKey(newLiquidData.Entry);

        if (curLiquid != LastLiquid)
        {
            if (LastLiquid != null && LastLiquid.SpellID != 0)
                RemoveAura(LastLiquid.SpellID);

            var player = CharmerOrOwnerPlayerOrPlayerItself;

            // Set _lastLiquid before casting liquid spell to avoid infinite loops
            LastLiquid = curLiquid;

            if (curLiquid != null && curLiquid.SpellID != 0 && (!player || !player.IsGameMaster))
                SpellFactory.CastSpell(this, curLiquid.SpellID, true);
        }

        // mount capability depends on liquid state change
        if (oldLiquidStatus != Location.LiquidStatus)
            UpdateMountCapability();
    }

    public void RemoveExtraUnitMovementFlag2(MovementFlags3 f)
    {
        MovementInfo.RemoveExtraMovementFlag2(f);
    }

    public void RemoveUnitMovementFlag(MovementFlag f)
    {
        MovementInfo.RemoveMovementFlag(f);
    }

    public void RemoveVehicleKit(bool onRemoveFromWorld = false)
    {
        if (VehicleKit == null)
            return;

        if (!onRemoveFromWorld)
            SendSetVehicleRecId(0);

        VehicleKit.Uninstall();

        VehicleKit = null;

        UpdateFlag.Vehicle = false;
        UnitTypeMask &= ~UnitTypeMask.Vehicle;
        RemoveNpcFlag(NPCFlags.SpellClick | NPCFlags.PlayerVehicle);
    }

    public void ResumeMovement(uint timer = 0, MovementSlot slot = 0)
    {
        if (MotionMaster.IsInvalidMovementSlot(slot))
            return;

        var movementGenerator = MotionMaster.GetCurrentMovementGenerator(slot);

        movementGenerator?.Resume(timer);
    }

    //Teleport
    public void SendTeleportPacket(Position pos)
    {
        // SMSG_MOVE_UPDATE_TELEPORT is sent to nearby players to signal the teleport
        // SMSG_MOVE_TELEPORT is sent to self in order to trigger CMSG_MOVE_TELEPORT_ACK and update the position server side

        MoveUpdateTeleport moveUpdateTeleport = new()
        {
            Status = MovementInfo
        };

        if (MovementForces != null)
            moveUpdateTeleport.MovementForces = MovementForces.GetForces();

        var broadcastSource = this;

        // should this really be the unit _being_ moved? not the unit doing the moving?
        var playerMover = UnitBeingMoved?.AsPlayer;

        if (playerMover != null)
        {
            var newPos = pos.Copy();

            var transportBase = DirectTransport;

            transportBase?.CalculatePassengerOffset(newPos);

            MoveTeleport moveTeleport = new()
            {
                MoverGUID = GUID,
                Pos = newPos
            };

            if (GetTransGUID() != ObjectGuid.Empty)
                moveTeleport.TransportGUID = GetTransGUID();

            moveTeleport.Facing = newPos.Orientation;
            moveTeleport.SequenceIndex = MovementCounter++;
            playerMover.SendPacket(moveTeleport);

            broadcastSource = playerMover;
        }
        else
        {
            // This is the only packet sent for creatures which contains MovementInfo structure
            // we do not update m_movementInfo for creatures so it needs to be done manually here
            moveUpdateTeleport.Status.Guid = GUID;
            moveUpdateTeleport.Status.Pos.Relocate(pos);
            moveUpdateTeleport.Status.Time = Time.MSTime;
            var transportBase = DirectTransport;

            if (transportBase != null)
            {
                var newPos = pos.Copy();
                transportBase.CalculatePassengerOffset(newPos);
                moveUpdateTeleport.Status.Transport.Pos.Relocate(newPos);
            }
        }

        // Broadcast the packet to everyone except self.
        broadcastSource.SendMessageToSet(moveUpdateTeleport, false);
    }

    public bool SetCanDoubleJump(bool enable)
    {
        if (enable == HasUnitMovementFlag2(MovementFlag2.CanDoubleJump))
            return false;

        if (enable)
            AddUnitMovementFlag2(MovementFlag2.CanDoubleJump);
        else
            RemoveUnitMovementFlag2(MovementFlag2.CanDoubleJump);

        var playerMover = UnitBeingMoved?.AsPlayer;

        if (playerMover != null)
        {
            MoveSetFlag packet = new(enable ? ServerOpcodes.MoveEnableDoubleJump : ServerOpcodes.MoveDisableDoubleJump)
            {
                MoverGUID = GUID,
                SequenceIndex = MovementCounter++
            };

            playerMover.SendPacket(packet);

            MoveUpdate moveUpdate = new()
            {
                Status = MovementInfo
            };

            SendMessageToSet(moveUpdate, playerMover);
        }

        return true;
    }

    public bool SetCanFly(bool enable)
    {
        if (enable == HasUnitMovementFlag(MovementFlag.CanFly))
            return false;

        if (enable)
        {
            AddUnitMovementFlag(MovementFlag.CanFly);
            RemoveUnitMovementFlag(MovementFlag.Swimming | MovementFlag.SplineElevation);
        }
        else
        {
            RemoveUnitMovementFlag(MovementFlag.CanFly | MovementFlag.MaskMovingFly);
        }

        if (!enable && IsTypeId(TypeId.Player))
            AsPlayer.SetFallInformation(0, Location.Z);

        var playerMover = UnitBeingMoved?.AsPlayer;

        if (playerMover != null)
        {
            MoveSetFlag packet = new(enable ? ServerOpcodes.MoveSetCanFly : ServerOpcodes.MoveUnsetCanFly)
            {
                MoverGUID = GUID,
                SequenceIndex = MovementCounter++
            };

            playerMover.SendPacket(packet);

            MoveUpdate moveUpdate = new()
            {
                Status = MovementInfo
            };

            SendMessageToSet(moveUpdate, playerMover);
        }
        else
        {
            MoveSplineSetFlag packet = new(enable ? ServerOpcodes.MoveSplineSetFlying : ServerOpcodes.MoveSplineUnsetFlying)
            {
                MoverGUID = GUID
            };

            SendMessageToSet(packet, true);
        }

        return true;
    }

    public bool SetCanTransitionBetweenSwimAndFly(bool enable)
    {
        if (!IsTypeId(TypeId.Player))
            return false;

        if (enable == HasUnitMovementFlag2(MovementFlag2.CanSwimToFlyTrans))
            return false;

        if (enable)
            AddUnitMovementFlag2(MovementFlag2.CanSwimToFlyTrans);
        else
            RemoveUnitMovementFlag2(MovementFlag2.CanSwimToFlyTrans);

        var playerMover = UnitBeingMoved?.AsPlayer;

        if (playerMover != null)
        {
            MoveSetFlag packet = new(enable ? ServerOpcodes.MoveEnableTransitionBetweenSwimAndFly : ServerOpcodes.MoveDisableTransitionBetweenSwimAndFly)
            {
                MoverGUID = GUID,
                SequenceIndex = MovementCounter++
            };

            playerMover.SendPacket(packet);

            MoveUpdate moveUpdate = new()
            {
                Status = MovementInfo
            };

            SendMessageToSet(moveUpdate, playerMover);
        }

        return true;
    }

    public bool SetCanTurnWhileFalling(bool enable)
    {
        // Temporarily disabled for short lived auras that unapply before client had time to ACK applying
        //if (enable == HasUnitMovementFlag2(MovementFlag2.CanTurnWhileFalling))
        //return false;

        if (enable)
            AddUnitMovementFlag2(MovementFlag2.CanTurnWhileFalling);
        else
            RemoveUnitMovementFlag2(MovementFlag2.CanTurnWhileFalling);

        var playerMover = UnitBeingMoved?.AsPlayer;

        if (playerMover != null)
        {
            MoveSetFlag packet = new(enable ? ServerOpcodes.MoveSetCanTurnWhileFalling : ServerOpcodes.MoveUnsetCanTurnWhileFalling)
            {
                MoverGUID = GUID,
                SequenceIndex = MovementCounter++
            };

            playerMover.SendPacket(packet);

            MoveUpdate moveUpdate = new()
            {
                Status = MovementInfo
            };

            SendMessageToSet(moveUpdate, playerMover);
        }

        return true;
    }

    public void SetControlled(bool apply, UnitState state)
    {
        if (apply)
        {
            if (HasUnitState(state))
                return;

            if (state.HasFlag(UnitState.Controlled))
                CastStop();

            AddUnitState(state);

            switch (state)
            {
                case UnitState.Stunned:
                    SetStunned(true);

                    break;
                case UnitState.Root:
                    if (!HasUnitState(UnitState.Stunned))
                        SetRooted(true);

                    break;
                case UnitState.Confused:
                    if (!HasUnitState(UnitState.Stunned))
                    {
                        ClearUnitState(UnitState.MeleeAttacking);
                        SendMeleeAttackStop();
                        // SendAutoRepeatCancel ?
                        SetConfused(true);
                    }

                    break;
                case UnitState.Fleeing:
                    if (!HasUnitState(UnitState.Stunned | UnitState.Confused))
                    {
                        ClearUnitState(UnitState.MeleeAttacking);
                        SendMeleeAttackStop();
                        // SendAutoRepeatCancel ?
                        SetFeared(true);
                    }

                    break;
            }
        }
        else
        {
            switch (state)
            {
                case UnitState.Stunned:
                    if (HasAuraType(AuraType.ModStun) || HasAuraType(AuraType.ModStunDisableGravity))
                        return;

                    ClearUnitState(state);
                    SetStunned(false);

                    break;
                case UnitState.Root:
                    if (HasAuraType(AuraType.ModRoot) || HasAuraType(AuraType.ModRoot2) || HasAuraType(AuraType.ModRootDisableGravity) || Vehicle != null || (IsCreature && AsCreature.MovementTemplate.Rooted))
                        return;

                    ClearUnitState(state);

                    if (!HasUnitState(UnitState.Stunned))
                        SetRooted(false);

                    break;
                case UnitState.Confused:
                    if (HasAuraType(AuraType.ModConfuse))
                        return;

                    ClearUnitState(state);
                    SetConfused(false);

                    break;
                case UnitState.Fleeing:
                    if (HasAuraType(AuraType.ModFear))
                        return;

                    ClearUnitState(state);
                    SetFeared(false);

                    break;
                default:
                    return;
            }

            ApplyControlStatesIfNeeded();
        }
    }

    public bool SetDisableGravity(bool disable, bool updateAnimTier = true)
    {
        if (disable == IsGravityDisabled)
            return false;

        if (disable)
        {
            AddUnitMovementFlag(MovementFlag.DisableGravity);
            RemoveUnitMovementFlag(MovementFlag.Swimming | MovementFlag.SplineElevation);
        }
        else
        {
            RemoveUnitMovementFlag(MovementFlag.DisableGravity);
        }


        var playerMover = UnitBeingMoved?.AsPlayer;

        if (playerMover != null)
        {
            MoveSetFlag packet = new(disable ? ServerOpcodes.MoveDisableGravity : ServerOpcodes.MoveEnableGravity)
            {
                MoverGUID = GUID,
                SequenceIndex = MovementCounter++
            };

            playerMover.SendPacket(packet);

            MoveUpdate moveUpdate = new()
            {
                Status = MovementInfo
            };

            SendMessageToSet(moveUpdate, playerMover);
        }
        else
        {
            MoveSplineSetFlag packet = new(disable ? ServerOpcodes.MoveSplineDisableGravity : ServerOpcodes.MoveSplineEnableGravity)
            {
                MoverGUID = GUID
            };

            SendMessageToSet(packet, true);
        }

        if (IsCreature && updateAnimTier && IsAlive && !HasUnitState(UnitState.Root) && !AsCreature.MovementTemplate.Rooted)
        {
            if (IsGravityDisabled)
                SetAnimTier(AnimTier.Fly);
            else if (IsHovering)
                SetAnimTier(AnimTier.Hover);
            else
                SetAnimTier(AnimTier.Ground);
        }

        return true;
    }

    public bool SetDisableInertia(bool disable)
    {
        if (disable == HasExtraUnitMovementFlag2(MovementFlags3.DisableInertia))
            return false;

        if (disable)
            AddExtraUnitMovementFlag2(MovementFlags3.DisableInertia);
        else
            RemoveExtraUnitMovementFlag2(MovementFlags3.DisableInertia);

        var playerMover = UnitBeingMoved?.AsPlayer;

        if (playerMover != null)
        {
            MoveSetFlag packet = new(disable ? ServerOpcodes.MoveDisableInertia : ServerOpcodes.MoveEnableInertia)
            {
                MoverGUID = GUID,
                SequenceIndex = MovementCounter++
            };

            playerMover.SendPacket(packet);

            MoveUpdate moveUpdate = new()
            {
                Status = MovementInfo
            };

            SendMessageToSet(moveUpdate, playerMover);
        }

        return true;
    }

    public void SetExtraUnitMovementFlags2(MovementFlags3 f)
    {
        MovementInfo.SetExtraMovementFlags2(f);
    }

    public void SetFacingTo(float ori, bool force = true)
    {
        // do not face when already moving
        if (!force && (!IsStopped || !MoveSpline.Finalized()))
            return;

        MoveSplineInit init = new(this);
        init.MoveTo(Location.X, Location.Y, Location.Z, false);

        if (Transport != null)
            init.DisableTransportPathTransformations(); // It makes no sense to target global orientation

        init.SetFacing(ori);

        //GetMotionMaster().LaunchMoveSpline(init, EventId.Face, MovementGeneratorPriority.Highest);
        init.Launch();
    }

    public void SetFacingToObject(WorldObject obj, bool force = true)
    {
        // do not face when already moving
        if (!force && (!IsStopped || !MoveSpline.Finalized()))
            return;

        // @todo figure out under what conditions creature will move towards object instead of facing it where it currently is.
        MoveSplineInit init = new(this);
        init.MoveTo(Location.X, Location.Y, Location.Z, false);
        init.SetFacing(Location.GetAbsoluteAngle(obj.Location)); // when on transport, GetAbsoluteAngle will still return global coordinates (and angle) that needs transforming

        //GetMotionMaster().LaunchMoveSpline(init, EventId.Face, MovementGeneratorPriority.Highest);
        init.Launch();
    }

    public void SetFacingToUnit(Unit unit, bool force = true)
    {
        // do not face when already moving
        if (!force && (!IsStopped || !MoveSpline.Finalized()))
            return;

        // @todo figure out under what conditions creature will move towards object instead of facing it where it currently is.
        var init = new MoveSplineInit(this);
        init.MoveTo(Location.X, Location.Y, Location.Z, false);

        if (Transport != null)
            init.DisableTransportPathTransformations(); // It makes no sense to target global orientation

        init.SetFacing(unit);

        //GetMotionMaster()->LaunchMoveSpline(std::move(init), EVENT_FACE, MOTION_PRIORITY_HIGHEST);
        init.Launch();
    }

    public bool SetFall(bool enable)
    {
        if (enable == HasUnitMovementFlag(MovementFlag.Falling))
            return false;

        if (enable)
        {
            AddUnitMovementFlag(MovementFlag.Falling);
            MovementInfo.SetFallTime(0);
        }
        else
        {
            RemoveUnitMovementFlag(MovementFlag.Falling | MovementFlag.FallingFar);
        }

        return true;
    }

    public bool SetFeatherFall(bool enable)
    {
        // Temporarily disabled for short lived auras that unapply before client had time to ACK applying
        //if (enable == HasUnitMovementFlag(MovementFlag.FallingSlow))
        //return false;

        if (enable)
            AddUnitMovementFlag(MovementFlag.FallingSlow);
        else
            RemoveUnitMovementFlag(MovementFlag.FallingSlow);


        var playerMover = UnitBeingMoved?.AsPlayer;

        if (playerMover != null)
        {
            MoveSetFlag packet = new(enable ? ServerOpcodes.MoveSetFeatherFall : ServerOpcodes.MoveSetNormalFall)
            {
                MoverGUID = GUID,
                SequenceIndex = MovementCounter++
            };

            playerMover.SendPacket(packet);

            MoveUpdate moveUpdate = new()
            {
                Status = MovementInfo
            };

            SendMessageToSet(moveUpdate, playerMover);
        }
        else
        {
            MoveSplineSetFlag packet = new(enable ? ServerOpcodes.MoveSplineSetFeatherFall : ServerOpcodes.MoveSplineSetNormalFall)
            {
                MoverGUID = GUID
            };

            SendMessageToSet(packet, true);
        }

        return true;
    }

    public bool SetHover(bool enable, bool updateAnimTier = true)
    {
        if (enable == HasUnitMovementFlag(MovementFlag.Hover))
            return false;

        float hoverHeight = UnitData.HoverHeight;

        if (enable)
        {
            //! No need to check height on ascent
            AddUnitMovementFlag(MovementFlag.Hover);

            if (hoverHeight != 0 && Location.Z - Location.FloorZ < hoverHeight)
                UpdateHeight(Location.Z + hoverHeight);
        }
        else
        {
            RemoveUnitMovementFlag(MovementFlag.Hover);

            //! Dying creatures will MoveFall from setDeathState
            if (hoverHeight != 0 && (!IsDying || !IsUnit))
            {
                var newZ = Math.Max(Location.FloorZ, Location.Z - hoverHeight);
                newZ = Location.UpdateAllowedPositionZ(Location.X, Location.Y, newZ);
                UpdateHeight(newZ);
            }
        }

        var playerMover = UnitBeingMoved?.AsPlayer;

        if (playerMover != null)
        {
            MoveSetFlag packet = new(enable ? ServerOpcodes.MoveSetHovering : ServerOpcodes.MoveUnsetHovering)
            {
                MoverGUID = GUID,
                SequenceIndex = MovementCounter++
            };

            playerMover.SendPacket(packet);

            MoveUpdate moveUpdate = new()
            {
                Status = MovementInfo
            };

            SendMessageToSet(moveUpdate, playerMover);
        }
        else
        {
            MoveSplineSetFlag packet = new(enable ? ServerOpcodes.MoveSplineSetHover : ServerOpcodes.MoveSplineUnsetHover)
            {
                MoverGUID = GUID
            };

            SendMessageToSet(packet, true);
        }

        if (IsCreature && updateAnimTier && IsAlive && !HasUnitState(UnitState.Root) && !AsCreature.MovementTemplate.Rooted)
        {
            if (IsGravityDisabled)
                SetAnimTier(AnimTier.Fly);
            else if (IsHovering)
                SetAnimTier(AnimTier.Hover);
            else
                SetAnimTier(AnimTier.Ground);
        }

        return true;
    }

    public bool SetIgnoreMovementForces(bool ignore)
    {
        if (ignore == HasUnitMovementFlag2(MovementFlag2.IgnoreMovementForces))
            return false;

        if (ignore)
            AddUnitMovementFlag2(MovementFlag2.IgnoreMovementForces);
        else
            RemoveUnitMovementFlag2(MovementFlag2.IgnoreMovementForces);

        ServerOpcodes[] ignoreMovementForcesOpcodeTable =
        {
            ServerOpcodes.MoveUnsetIgnoreMovementForces, ServerOpcodes.MoveSetIgnoreMovementForces
        };

        var movingPlayer = PlayerMovingMe1;

        if (movingPlayer != null)
        {
            MoveSetFlag packet = new(ignoreMovementForcesOpcodeTable[ignore ? 1 : 0])
            {
                MoverGUID = GUID,
                SequenceIndex = MovementCounter++
            };

            movingPlayer.SendPacket(packet);

            MoveUpdate moveUpdate = new()
            {
                Status = MovementInfo
            };

            SendMessageToSet(moveUpdate, movingPlayer);
        }

        return true;
    }

    public void SetInFront(WorldObject target)
    {
        if (!HasUnitState(UnitState.CannotTurn))
            Location.Orientation = Location.GetAbsoluteAngle(target.Location);
    }

    public void SetMovedUnit(Unit target)
    {
        UnitMovedByMe.PlayerMovingMe = null;
        UnitMovedByMe = target;
        UnitMovedByMe.PlayerMovingMe = AsPlayer;

        MoveSetActiveMover packet = new()
        {
            MoverGUID = target.GUID
        };

        AsPlayer.SendPacket(packet);
    }

    public void SetRooted(bool apply, bool packetOnly = false)
    {
        if (!packetOnly)
        {
            if (apply)
            {
                // MOVEMENTFLAG_ROOT cannot be used in conjunction with MOVEMENTFLAG_MASK_MOVING (tested 3.3.5a)
                // this will freeze clients. That's why we remove MOVEMENTFLAG_MASK_MOVING before
                // setting MOVEMENTFLAG_ROOT
                RemoveUnitMovementFlag(MovementFlag.MaskMoving);
                AddUnitMovementFlag(MovementFlag.Root);
                StopMoving();
            }
            else
            {
                RemoveUnitMovementFlag(MovementFlag.Root);
            }
        }

        var playerMover = UnitBeingMoved?.AsPlayer; // unit controlled by a player.

        if (playerMover != null)
        {
            MoveSetFlag packet = new(apply ? ServerOpcodes.MoveRoot : ServerOpcodes.MoveUnroot)
            {
                MoverGUID = GUID,
                SequenceIndex = MovementCounter++
            };

            playerMover.SendPacket(packet);

            MoveUpdate moveUpdate = new()
            {
                Status = MovementInfo
            };

            SendMessageToSet(moveUpdate, playerMover);
        }
        else
        {
            MoveSplineSetFlag packet = new(apply ? ServerOpcodes.MoveSplineRoot : ServerOpcodes.MoveSplineUnroot)
            {
                MoverGUID = GUID
            };

            SendMessageToSet(packet, true);
        }
    }

    public void SetSpeed(UnitMoveType mtype, float newValue)
    {
        SetSpeedRate(mtype, newValue / (ControlledByPlayer ? SharedConst.playerBaseMoveSpeed[(int)mtype] : SharedConst.baseMoveSpeed[(int)mtype]));
    }

    public void SetSpeedRate(UnitMoveType mtype, float rate)
    {
        rate = Math.Max(rate, 0.01f);

        if (SpeedRate[(int)mtype] == rate)
            return;

        SpeedRate[(int)mtype] = rate;

        PropagateSpeedChange();

        // Spline packets are for creatures and move_update are for players
        var moveTypeToOpcode = new[,]
        {
            {
                ServerOpcodes.MoveSplineSetWalkSpeed, ServerOpcodes.MoveSetWalkSpeed, ServerOpcodes.MoveUpdateWalkSpeed
            },
            {
                ServerOpcodes.MoveSplineSetRunSpeed, ServerOpcodes.MoveSetRunSpeed, ServerOpcodes.MoveUpdateRunSpeed
            },
            {
                ServerOpcodes.MoveSplineSetRunBackSpeed, ServerOpcodes.MoveSetRunBackSpeed, ServerOpcodes.MoveUpdateRunBackSpeed
            },
            {
                ServerOpcodes.MoveSplineSetSwimSpeed, ServerOpcodes.MoveSetSwimSpeed, ServerOpcodes.MoveUpdateSwimSpeed
            },
            {
                ServerOpcodes.MoveSplineSetSwimBackSpeed, ServerOpcodes.MoveSetSwimBackSpeed, ServerOpcodes.MoveUpdateSwimBackSpeed
            },
            {
                ServerOpcodes.MoveSplineSetTurnRate, ServerOpcodes.MoveSetTurnRate, ServerOpcodes.MoveUpdateTurnRate
            },
            {
                ServerOpcodes.MoveSplineSetFlightSpeed, ServerOpcodes.MoveSetFlightSpeed, ServerOpcodes.MoveUpdateFlightSpeed
            },
            {
                ServerOpcodes.MoveSplineSetFlightBackSpeed, ServerOpcodes.MoveSetFlightBackSpeed, ServerOpcodes.MoveUpdateFlightBackSpeed
            },
            {
                ServerOpcodes.MoveSplineSetPitchRate, ServerOpcodes.MoveSetPitchRate, ServerOpcodes.MoveUpdatePitchRate
            },
        };

        if (IsTypeId(TypeId.Player))
        {
            // register forced speed changes for WorldSession.HandleForceSpeedChangeAck
            // and do it only for real sent packets and use run for run/mounted as client expected
            ++AsPlayer.ForcedSpeedChanges[(int)mtype];

            if (!IsInCombat)
            {
                var pet = AsPlayer.CurrentPet;

                if (pet)
                    pet.SetSpeedRate(mtype, SpeedRate[(int)mtype]);
            }
        }

        var playerMover = UnitBeingMoved?.AsPlayer; // unit controlled by a player.

        if (playerMover != null)
        {
            // Send notification to self
            MoveSetSpeed selfpacket = new(moveTypeToOpcode[(int)mtype, 1])
            {
                MoverGUID = GUID,
                SequenceIndex = MovementCounter++,
                Speed = GetSpeed(mtype)
            };

            playerMover.SendPacket(selfpacket);

            // Send notification to other players
            MoveUpdateSpeed packet = new(moveTypeToOpcode[(int)mtype, 2])
            {
                Status = MovementInfo,
                Speed = GetSpeed(mtype)
            };

            playerMover.SendMessageToSet(packet, false);
        }
        else
        {
            MoveSplineSetSpeed packet = new(moveTypeToOpcode[(int)mtype, 0])
            {
                MoverGUID = GUID,
                Speed = GetSpeed(mtype)
            };

            SendMessageToSet(packet, true);
        }
    }
    public bool SetSwim(bool enable)
    {
        if (enable == HasUnitMovementFlag(MovementFlag.Swimming))
            return false;

        if (enable)
            AddUnitMovementFlag(MovementFlag.Swimming);
        else
            RemoveUnitMovementFlag(MovementFlag.Swimming);

        MoveSplineSetFlag packet = new(enable ? ServerOpcodes.MoveSplineStartSwim : ServerOpcodes.MoveSplineStopSwim)
        {
            MoverGUID = GUID
        };

        SendMessageToSet(packet, true);

        return true;
    }

    public void SetUnitMovementFlags(MovementFlag f)
    {
        MovementInfo.MovementFlags = f;
    }

    public void SetUnitMovementFlags2(MovementFlag2 f)
    {
        MovementInfo.SetMovementFlags2(f);
    }

    public bool SetWalk(bool enable)
    {
        if (enable == IsWalking)
            return false;

        if (enable)
            AddUnitMovementFlag(MovementFlag.Walking);
        else
            RemoveUnitMovementFlag(MovementFlag.Walking);

        MoveSplineSetFlag packet = new(enable ? ServerOpcodes.MoveSplineSetWalkMode : ServerOpcodes.MoveSplineSetRunMode)
        {
            MoverGUID = GUID
        };

        SendMessageToSet(packet, true);

        return true;
    }

    public bool SetWaterWalking(bool enable)
    {
        if (enable == HasUnitMovementFlag(MovementFlag.WaterWalk))
            return false;

        if (enable)
            AddUnitMovementFlag(MovementFlag.WaterWalk);
        else
            RemoveUnitMovementFlag(MovementFlag.WaterWalk);


        var playerMover = UnitBeingMoved?.AsPlayer;

        if (playerMover != null)
        {
            MoveSetFlag packet = new(enable ? ServerOpcodes.MoveSetWaterWalk : ServerOpcodes.MoveSetLandWalk)
            {
                MoverGUID = GUID,
                SequenceIndex = MovementCounter++
            };

            playerMover.SendPacket(packet);

            MoveUpdate moveUpdate = new()
            {
                Status = MovementInfo
            };

            SendMessageToSet(moveUpdate, playerMover);
        }
        else
        {
            MoveSplineSetFlag packet = new(enable ? ServerOpcodes.MoveSplineSetWaterWalk : ServerOpcodes.MoveSplineSetLandWalk)
            {
                MoverGUID = GUID
            };

            SendMessageToSet(packet, true);
        }

        return true;
    }

    public void StopMoving()
    {
        ClearUnitState(UnitState.Moving);

        // not need send any packets if not in world or not moving
        if (!Location.IsInWorld || MoveSpline.Finalized())
            return;

        // Update position now since Stop does not start a new movement that can be updated later
        if (MoveSpline.HasStarted())
            UpdateSplinePosition();

        MoveSplineInit init = new(this);
        init.Stop();
    }
    public void UpdateMountCapability()
    {
        var mounts = GetAuraEffectsByType(AuraType.Mounted);

        foreach (var aurEff in mounts.ToList())
        {
            aurEff.RecalculateAmount();

            if (aurEff.Amount == 0)
            {
                aurEff.Base.Remove();
            }
            else
            {
                if (CliDB.MountCapabilityStorage.TryGetValue((uint)aurEff.Amount, out var capability)) // aura may get removed by interrupt Id, reapply
                    if (!HasAura(capability.ModSpellAuraID))
                        SpellFactory.CastSpell(this, capability.ModSpellAuraID, new CastSpellExtraArgs(aurEff));
            }
        }
    }

    public void UpdateMovementForcesModMagnitude()
    {
        var modMagnitude = (float)GetTotalAuraMultiplier(AuraType.ModMovementForceMagnitude);

        var movingPlayer = PlayerMovingMe1;

        if (movingPlayer != null)
        {
            MoveSetSpeed setModMovementForceMagnitude = new(ServerOpcodes.MoveSetModMovementForceMagnitude)
            {
                MoverGUID = GUID,
                SequenceIndex = MovementCounter++,
                Speed = modMagnitude
            };

            movingPlayer.SendPacket(setModMovementForceMagnitude);
            ++movingPlayer.MovementForceModMagnitudeChanges;
        }
        else
        {
            MoveUpdateSpeed updateModMovementForceMagnitude = new(ServerOpcodes.MoveUpdateModMovementForceMagnitude)
            {
                Status = MovementInfo,
                Speed = modMagnitude
            };

            SendMessageToSet(updateModMovementForceMagnitude, true);
        }

        if (modMagnitude != 1.0f && MovementForces == null)
            MovementForces = new MovementForces();

        if (MovementForces != null)
        {
            MovementForces.ModMagnitude = modMagnitude;

            if (MovementForces.IsEmpty)
                MovementForces = new MovementForces();
        }
    }

    public virtual bool UpdatePosition(Position obj, bool teleport = false)
    {
        return UpdatePosition(obj.X, obj.Y, obj.Z, obj.Orientation, teleport);
    }

    public virtual bool UpdatePosition(float x, float y, float z, float orientation, bool teleport = false)
    {
        if (!GridDefines.IsValidMapCoord(x, y, z, orientation))
        {
            Log.Logger.Error("Unit.UpdatePosition({0}, {1}, {2}) .. bad coordinates!", x, y, z);

            return false;
        }

        // Check if angular distance changed
        var turn = MathFunctions.fuzzyGt((float)Math.PI - Math.Abs(Math.Abs(Location.Orientation - orientation) - (float)Math.PI), 0.0f);

        // G3D::fuzzyEq won't help here, in some cases magnitudes differ by a little more than G3D::eps, but should be considered equal
        var relocated = teleport ||
                        Math.Abs(Location.X - x) > 0.001f ||
                        Math.Abs(Location.Y - y) > 0.001f ||
                        Math.Abs(Location.Z - z) > 0.001f;

        if (relocated)
        {
            // move and update visible state if need
            if (IsTypeId(TypeId.Player))
                Location.Map.PlayerRelocation(AsPlayer, x, y, z, orientation);
            else
                Location.Map.CreatureRelocation(AsCreature, x, y, z, orientation);
        }
        else if (turn)
        {
            UpdateOrientation(orientation);
        }

        _positionUpdateInfo.Relocated = relocated;
        _positionUpdateInfo.Turned = turn;

        var isInWater = Location.IsInWater;

        if (!IsFalling || isInWater || IsFlying)
            RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags2.Ground);

        if (isInWater)
            RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags2.Swimming);

        return relocated || turn;
    }

    public void UpdateSpeed(UnitMoveType mtype)
    {
        double mainSpeedMod = 0;
        double stackBonus = 1.0f;
        double nonStackBonus = 1.0f;

        switch (mtype)
        {
            // Only apply debuffs
            case UnitMoveType.FlightBack:
            case UnitMoveType.RunBack:
            case UnitMoveType.SwimBack:
                break;
            case UnitMoveType.Walk:
                return;
            case UnitMoveType.Run:
            {
                if (IsMounted) // Use on mount auras
                {
                    mainSpeedMod = GetMaxPositiveAuraModifier(AuraType.ModIncreaseMountedSpeed);
                    stackBonus = GetTotalAuraMultiplier(AuraType.ModMountedSpeedAlways);
                    nonStackBonus += GetMaxPositiveAuraModifier(AuraType.ModMountedSpeedNotStack) / 100.0f;
                }
                else
                {
                    mainSpeedMod = GetMaxPositiveAuraModifier(AuraType.ModIncreaseSpeed);
                    stackBonus = GetTotalAuraMultiplier(AuraType.ModSpeedAlways);
                    nonStackBonus += GetMaxPositiveAuraModifier(AuraType.ModSpeedNotStack) / 100.0f;
                }

                break;
            }
            case UnitMoveType.Swim:
            {
                mainSpeedMod = GetMaxPositiveAuraModifier(AuraType.ModIncreaseSwimSpeed);

                break;
            }
            case UnitMoveType.Flight:
            {
                if (IsTypeId(TypeId.Unit) && ControlledByPlayer) // not sure if good for pet
                {
                    mainSpeedMod = GetMaxPositiveAuraModifier(AuraType.ModIncreaseVehicleFlightSpeed);
                    stackBonus = GetTotalAuraMultiplier(AuraType.ModVehicleSpeedAlways);

                    // for some spells this mod is applied on vehicle owner
                    double ownerSpeedMod = 0;

                    var owner = Charmer;

                    if (owner != null)
                        ownerSpeedMod = owner.GetMaxPositiveAuraModifier(AuraType.ModIncreaseVehicleFlightSpeed);

                    mainSpeedMod = Math.Max(mainSpeedMod, ownerSpeedMod);
                }
                else if (IsMounted)
                {
                    mainSpeedMod = GetMaxPositiveAuraModifier(AuraType.ModIncreaseMountedFlightSpeed);
                    stackBonus = GetTotalAuraMultiplier(AuraType.ModMountedFlightSpeedAlways);
                }
                else // Use not mount (shapeshift for example) auras (should stack)
                {
                    mainSpeedMod = GetTotalAuraModifier(AuraType.ModIncreaseFlightSpeed) + GetTotalAuraModifier(AuraType.ModIncreaseVehicleFlightSpeed);
                }

                nonStackBonus += GetMaxPositiveAuraModifier(AuraType.ModFlightSpeedNotStack) / 100.0f;

                // Update speed for vehicle if available
                if (IsTypeId(TypeId.Player) && Vehicle != null)
                    VehicleBase.UpdateSpeed(UnitMoveType.Flight);

                break;
            }
            default:
                Log.Logger.Error("Unit.UpdateSpeed: Unsupported move type ({0})", mtype);

                return;
        }

        // now we ready for speed calculation
        var speed = Math.Max(nonStackBonus, stackBonus);

        if (mainSpeedMod != 0)
            MathFunctions.AddPct(ref speed, mainSpeedMod);

        switch (mtype)
        {
            case UnitMoveType.Run:
            case UnitMoveType.Swim:
            case UnitMoveType.Flight:
            {
                // Set creature speed rate
                if (IsTypeId(TypeId.Unit))
                    speed *= AsCreature.Template.SpeedRun; // at this point, MOVE_WALK is never reached

                // Normalize speed by 191 aura SPELL_AURA_USE_NORMAL_MOVEMENT_SPEED if need
                // @todo possible affect only on MOVE_RUN
                var normalization = GetMaxPositiveAuraModifier(AuraType.UseNormalMovementSpeed);

                if (normalization != 0)
                {
                    var creature1 = AsCreature;

                    if (creature1)
                    {
                        var immuneMask = creature1.Template.MechanicImmuneMask;

                        if (Convert.ToBoolean(immuneMask & (1 << ((int)Mechanics.Snare - 1))) || Convert.ToBoolean(immuneMask & (1 << ((int)Mechanics.Daze - 1))))
                            break;
                    }

                    // Use speed from aura
                    var maxSpeed = normalization / (ControlledByPlayer ? SharedConst.playerBaseMoveSpeed[(int)mtype] : SharedConst.baseMoveSpeed[(int)mtype]);

                    if (speed > maxSpeed)
                        speed = maxSpeed;
                }

                if (mtype == UnitMoveType.Run)
                {
                    // force minimum speed rate @ aura 437 SPELL_AURA_MOD_MINIMUM_SPEED_RATE
                    var minSpeedMod1 = GetMaxPositiveAuraModifier(AuraType.ModMinimumSpeedRate);

                    if (minSpeedMod1 != 0)
                    {
                        var minSpeed = minSpeedMod1 / (ControlledByPlayer ? SharedConst.playerBaseMoveSpeed[(int)mtype] : SharedConst.baseMoveSpeed[(int)mtype]);

                        if (speed < minSpeed)
                            speed = minSpeed;
                    }
                }

                break;
            }
        }

        var creature = AsCreature;

        if (creature != null)
            if (creature.HasUnitTypeMask(UnitTypeMask.Minion) && !creature.IsInCombat)
                if (MotionMaster.GetCurrentMovementGeneratorType() == MovementGeneratorType.Follow)
                {
                    var followed = (MotionMaster.GetCurrentMovementGenerator() as FollowMovementGenerator)?.GetTarget();

                    if (followed != null && followed.GUID == OwnerGUID && !followed.IsInCombat)
                    {
                        var ownerSpeed = followed.GetSpeedRate(mtype);

                        if (speed < ownerSpeed || creature.Location.IsWithinDist3d(followed.Location, 10.0f))
                            speed = ownerSpeed;

                        speed *= Math.Min(Math.Max(1.0f, 0.75f + (Location.GetDistance(followed) - SharedConst.PetFollowDist) * 0.05f), 1.3f);
                    }
                }

        // Apply strongest slow aura mod to speed
        var slow = GetMaxNegativeAuraModifier(AuraType.ModDecreaseSpeed);

        if (slow != 0)
            MathFunctions.AddPct(ref speed, slow);

        var minSpeedMod = GetMaxPositiveAuraModifier(AuraType.ModMinimumSpeed);

        if (minSpeedMod != 0)
        {
            var baseMinSpeed = 1.0f;

            if (!OwnerGUID.IsPlayer && !IsHunterPet && TypeId == TypeId.Unit)
                baseMinSpeed = AsCreature.Template.SpeedRun;

            var minSpeed = MathFunctions.CalculatePct(baseMinSpeed, minSpeedMod);

            if (speed < minSpeed)
                speed = minSpeed;
        }

        SetSpeedRate(mtype, (float)speed);
    }
    private void ApplyControlStatesIfNeeded()
    {
        // Unit States might have been already cleared but auras still present. I need to check with HasAuraType
        if (HasUnitState(UnitState.Stunned) || HasAuraType(AuraType.ModStun) || HasAuraType(AuraType.ModStunDisableGravity))
            SetStunned(true);

        if (HasUnitState(UnitState.Root) || HasAuraType(AuraType.ModRoot) || HasAuraType(AuraType.ModRoot2) || HasAuraType(AuraType.ModRootDisableGravity))
            SetRooted(true);

        if (HasUnitState(UnitState.Confused) || HasAuraType(AuraType.ModConfuse))
            SetConfused(true);

        if (HasUnitState(UnitState.Fleeing) || HasAuraType(AuraType.ModFear))
            SetFeared(true);
    }

    private void InterruptMovementBasedAuras()
    {
        // TODO: Check if orientation transport offset changed instead of only global orientation
        if (_positionUpdateInfo.Turned)
            RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Turning);

        if (_positionUpdateInfo.Relocated && !Vehicle)
            RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Moving);
    }

    private void PropagateSpeedChange()
    {
        MotionMaster.PropagateSpeedChange();
    }

    private void RemoveUnitMovementFlag2(MovementFlag2 f)
    {
        MovementInfo.RemoveMovementFlag2(f);
    }

    private void SendMoveKnockBack(Player player, float speedXy, float speedZ, float vcos, float vsin)
    {
        MoveKnockBack moveKnockBack = new()
        {
            MoverGUID = GUID,
            SequenceIndex = MovementCounter++
        };

        moveKnockBack.Speeds.HorzSpeed = speedXy;
        moveKnockBack.Speeds.VertSpeed = speedZ;
        moveKnockBack.Direction = new Vector2(vcos, vsin);
        player.SendPacket(moveKnockBack);
    }


    private void SendSetVehicleRecId(uint vehicleId)
    {
        var player = AsPlayer;

        if (player)
        {
            MoveSetVehicleRecID moveSetVehicleRec = new()
            {
                MoverGUID = GUID,
                SequenceIndex = MovementCounter++,
                VehicleRecID = vehicleId
            };

            player.SendPacket(moveSetVehicleRec);
        }

        SetVehicleRecID setVehicleRec = new()
        {
            VehicleGUID = GUID,
            VehicleRecID = vehicleId
        };

        SendMessageToSet(setVehicleRec, true);
    }

    private void SetConfused(bool apply)
    {
        if (apply)
        {
            SetTarget(ObjectGuid.Empty);
            MotionMaster.MoveConfused();
        }
        else
        {
            if (IsAlive)
            {
                MotionMaster.Remove(MovementGeneratorType.Confused);

                if (Victim != null)
                    SetTarget(Victim.GUID);
            }
        }

        // block / allow control to real player in control (eg charmer)
        if (IsPlayer)
            if (PlayerMovingMe)
                PlayerMovingMe.SetClientControl(this, !apply);
    }

    private void SetFeared(bool apply)
    {
        if (apply)
        {
            SetTarget(ObjectGuid.Empty);

            Unit caster = null;
            var fearAuras = GetAuraEffectsByType(AuraType.ModFear);

            if (!fearAuras.Empty())
                caster = ObjectAccessor.GetUnit(this, fearAuras[0].CasterGuid);

            if (caster == null)
                caster = GetAttackerForHelper();

            MotionMaster.MoveFleeing(caster, (uint)(fearAuras.Empty() ? Configuration.GetDefaultValue("CreatureFamilyFleeDelay", 7000) : 0)); // caster == NULL processed in MoveFleeing
        }
        else
        {
            if (IsAlive)
            {
                MotionMaster.Remove(MovementGeneratorType.Fleeing);

                if (Victim != null)
                    SetTarget(Victim.GUID);

                if (!IsPlayer && !IsInCombat)
                    MotionMaster.MoveTargetedHome();
            }
        }

        // block / allow control to real player in control (eg charmer)
        if (IsPlayer)
            if (PlayerMovingMe)
                PlayerMovingMe.SetClientControl(this, !apply);
    }

    private void SetStunned(bool apply)
    {
        if (apply)
        {
            SetTarget(ObjectGuid.Empty);
            SetUnitFlag(UnitFlags.Stunned);

            StopMoving();

            if (IsTypeId(TypeId.Player))
                SetStandState(UnitStandStateType.Stand);

            SetRooted(true);

            CastStop();
        }
        else
        {
            if (IsAlive && Victim != null)
                SetTarget(Victim.GUID);

            // don't remove UNIT_FLAG_STUNNED for pet when owner is mounted (disabled pet's interface)
            var owner = CharmerOrOwner;

            if (owner == null || !owner.IsTypeId(TypeId.Player) || !owner.AsPlayer.IsMounted)
                RemoveUnitFlag(UnitFlags.Stunned);

            if (!HasUnitState(UnitState.Root)) // prevent moving if it also has root effect
                SetRooted(false);
        }
    }

    //! Only server-side height update, does not broadcast to client
    private void UpdateHeight(float newZ)
    {
        Location.Relocate(Location.X, Location.Y, newZ);

        if (IsVehicle)
            VehicleKit.RelocatePassengers();
    }

    private void UpdateOrientation(float orientation)
    {
        Location.Orientation = orientation;

        if (IsVehicle)
            VehicleKit.RelocatePassengers();
    }
    private void UpdateSplineMovement(uint diff)
    {
        if (MoveSpline.Finalized())
            return;

        MoveSpline.UpdateState((int)diff);
        var arrived = MoveSpline.Finalized();

        if (MoveSpline.IsCyclic())
        {
            _splineSyncTimer.Update(diff);

            if (_splineSyncTimer.Passed)
            {
                _splineSyncTimer.Reset(5000); // Retail value, do not change

                FlightSplineSync flightSplineSync = new()
                {
                    Guid = GUID,
                    SplineDist = (float)MoveSpline.TimePassed / MoveSpline.Duration()
                };

                SendMessageToSet(flightSplineSync, true);
            }
        }

        if (arrived)
        {
            DisableSpline();

            var animTier = MoveSpline.GetAnimation();

            if (animTier.HasValue)
                SetAnimTier(animTier.Value);
        }

        UpdateSplinePosition();
    }

    private void UpdateSplinePosition()
    {
        var loc = new Position(MoveSpline.ComputePosition());

        if (MoveSpline.OnTransport)
        {
            MovementInfo.Transport.Pos.Relocate(loc);

            var transport = DirectTransport;

            if (transport != null)
                transport.CalculatePassengerPosition(loc);
            else
                return;
        }

        if (HasUnitState(UnitState.CannotTurn))
            loc.Orientation = Location.Orientation;

        UpdatePosition(loc);
    }
}
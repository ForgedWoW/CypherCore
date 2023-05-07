// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.AI.PlayerAI;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Movement.Generators;
using Forged.MapServer.Networking.Packets.Combat;
using Forged.MapServer.Networking.Packets.Pet;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Entities.Units;

public partial class Unit
{
    public Pet CreateTamedPetFrom(Creature creatureTarget, uint spellID = 0)
    {
        if (!IsTypeId(TypeId.Player))
            return null;

        var pet = ClassFactory.ResolveWithPositionalParameters<Pet>(AsPlayer, PetType.Hunter);

        if (!pet.CreateBaseAtCreature(creatureTarget))
            return null;

        var level = creatureTarget.GetLevelForTarget(this) + 5 < Level ? Level - 5 : creatureTarget.GetLevelForTarget(this);

        if (InitTamedPet(pet, level, spellID))
            return pet;

        pet.Dispose();

        return null;

    }

    public Pet CreateTamedPetFrom(uint creatureEntry, uint spellID = 0)
    {
        if (!IsTypeId(TypeId.Player))
            return null;

        var creatureInfo = GameObjectManager.GetCreatureTemplate(creatureEntry);

        if (creatureInfo == null)
            return null;

        var pet = ClassFactory.ResolveWithPositionalParameters<Pet>(AsPlayer, PetType.Hunter);

        if (!pet.CreateBaseAtCreatureInfo(creatureInfo, this) || !InitTamedPet(pet, Level, spellID))
            return null;

        return pet;
    }

    public void GetAllMinionsByEntry(List<TempSummon> minions, uint entry)
    {
        for (var i = 0; i < Controlled.Count; ++i)
        {
            var unit = Controlled[i];

            if (unit.Entry == entry && unit.IsSummon) // minion, actually
                minions.Add(unit.ToTempSummon());
        }
    }

    public CharmInfo GetCharmInfo()
    {
        return _charmInfo;
    }

    public Unit GetFirstControlled()
    {
        // Sequence: charmed, pet, other guardians
        var unit = Charmed;

        if (unit != null)
            return unit;

        var guid = MinionGUID;

        if (!guid.IsEmpty)
            unit = ObjectAccessor.GetUnit(this, guid);

        return unit;
    }

    public CharmInfo InitCharmInfo()
    {
        return _charmInfo ??= new CharmInfo(this);
    }

    public void RemoveAllControlled()
    {
        // possessed pet and vehicle
        if (IsTypeId(TypeId.Player))
            AsPlayer.StopCastingCharm();

        while (!Controlled.Empty())
        {
            var target = Controlled.First();
            Controlled.RemoveAt(0);

            if (target.CharmerGUID == GUID)
                target.RemoveCharmAuras();
            else if (target.OwnerGUID == GUID && target.IsSummon)
                target.ToTempSummon().UnSummon();
            else
                Log.Logger.Error("Unit {0} is trying to release unit {1} which is neither charmed nor owned by it", Entry, target.Entry);
        }

        if (!PetGUID.IsEmpty)
            Log.Logger.Fatal("Unit {0} is not able to release its pet {1}", Entry, PetGUID);

        if (!MinionGUID.IsEmpty)
            Log.Logger.Fatal("Unit {0} is not able to release its minion {1}", Entry, MinionGUID);

        if (!CharmedGUID.IsEmpty)
            Log.Logger.Fatal("Unit {0} is not able to release its charm {1}", Entry, CharmedGUID);

        if (!IsPet)                                // pets don't use the Id for this
            RemoveUnitFlag(UnitFlags.PetInCombat); // m_controlled is now empty, so we know none of our minions are in combat
    }

    public void RemoveAllMinionsByEntry(uint entry)
    {
        for (var i = 0; i < Controlled.Count; ++i)
        {
            var unit = Controlled[i];

            if (unit.Entry == entry && unit.IsTypeId(TypeId.Unit) && unit.AsCreature.IsSummon) // minion, actually
                unit.ToTempSummon().UnSummon();
            // i think this is safe because i have never heard that a despawned minion will trigger a same minion
        }
    }

    public void RemoveCharmAuras()
    {
        RemoveAurasByType(AuraType.ModCharm);
        RemoveAurasByType(AuraType.ModPossessPet);
        RemoveAurasByType(AuraType.ModPossess);
        RemoveAurasByType(AuraType.AoeCharm);
    }

    public void RemoveCharmedBy()
    {
        if (!IsCharmed)
            return;

        CharmType type;

        if (HasUnitState(UnitState.Possessed))
            type = CharmType.Possess;
        else if (Charmer.IsOnVehicle(this))
            type = CharmType.Vehicle;
        else
            type = CharmType.Charm;

        CastStop();
        AttackStop();

        if (_oldFactionId != 0)
        {
            Faction = _oldFactionId;
            _oldFactionId = 0;
        }
        else
            RestoreFaction();

        //@todo Handle SLOT_IDLE motion resume
        MotionMaster.InitializeDefault();

        // Vehicle should not attack its passenger after he exists the seat
        if (type != CharmType.Vehicle)
            LastCharmerGuid = Charmer.GUID;

        Charmer.SetCharm(this, false);
        CombatManager.RevalidateCombat();

        var playerCharmer = Charmer.AsPlayer;

        if (playerCharmer != null)
            switch (type)
            {
                case CharmType.Vehicle:
                    playerCharmer.SetClientControl(this, false);
                    playerCharmer.SetClientControl(Charmer, true);
                    RemoveUnitFlag(UnitFlags.Possessed);

                    break;
                case CharmType.Possess:
                    ClearUnitState(UnitState.Possessed);
                    playerCharmer.SetClientControl(this, false);
                    playerCharmer.SetClientControl(Charmer, true);
                    Charmer.RemoveUnitFlag(UnitFlags.RemoveClientControl);
                    RemoveUnitFlag(UnitFlags.Possessed);

                    break;
                case CharmType.Charm:
                    if (IsTypeId(TypeId.Unit) && Charmer.Class == PlayerClass.Warlock)
                    {
                        var cinfo = AsCreature.Template;

                        if (cinfo is { CreatureType: CreatureType.Demon })
                        {
                            Class = (PlayerClass)cinfo.UnitClass;

                            if (GetCharmInfo() != null)
                                GetCharmInfo().SetPetNumber(0, true);
                            else
                                Log.Logger.Error("Aura:HandleModCharm: target={0} with typeid={1} has a charm aura but no charm info!", GUID, TypeId);
                        }
                    }

                    break;
            }

        var player = AsPlayer;

        player?.SetClientControl(this, true);

        if (playerCharmer != null && this != Charmer.GetFirstControlled())
            playerCharmer.SendRemoveControlBar();

        // a guardian should always have charminfo
        if (!IsGuardian)
            DeleteCharmInfo();

        // reset confused movement for example
        ApplyControlStatesIfNeeded();

        if (!IsPlayer || Charmer.IsCreature)
        {
            var charmedAI = AI;

            if (charmedAI != null)
                charmedAI.OnCharmed(false); // AI will potentially schedule a charm ai update
            else
                ScheduleAIChange();
        }
    }

    public void SendPetActionFeedback(PetActionFeedback msg, uint spellId)
    {
        var owner = OwnerUnit;

        if (owner == null || !owner.IsTypeId(TypeId.Player))
            return;

        PetActionFeedbackPacket petActionFeedback = new()
        {
            SpellID = spellId,
            Response = msg
        };

        owner.AsPlayer.SendPacket(petActionFeedback);
    }

    public void SendPetAIReaction(ObjectGuid guid)
    {
        var owner = OwnerUnit;

        if (owner == null || !owner.IsTypeId(TypeId.Player))
            return;

        AIReaction packet = new()
        {
            UnitGUID = guid,
            Reaction = AiReaction.Hostile
        };

        owner.AsPlayer.SendPacket(packet);
    }

    public void SendPetTalk(PetTalk pettalk)
    {
        var owner = OwnerUnit;

        if (owner == null || !owner.IsTypeId(TypeId.Player))
            return;

        PetActionSound petActionSound = new()
        {
            UnitGUID = GUID,
            Action = pettalk
        };

        owner.AsPlayer.SendPacket(petActionSound);
    }

    public void SetCharm(Unit charm, bool apply)
    {
        if (apply)
        {
            if (IsTypeId(TypeId.Player))
            {
                SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Charm), charm.GUID);
                Charmed = charm;

                charm.ControlledByPlayer = true;
                // @todo maybe we can use this Id to check if controlled by player
                charm.SetUnitFlag(UnitFlags.PlayerControlled);
            }
            else
                charm.ControlledByPlayer = false;

            // PvP, FFAPvP
            charm.ReplaceAllPvpFlags(PvpFlags);

            charm.SetUpdateFieldValue(charm.Values.ModifyValue(UnitData).ModifyValue(UnitData.CharmedBy), GUID);
            charm.Charmer = this;

            _isWalkingBeforeCharm = charm.IsWalking;

            if (_isWalkingBeforeCharm)
                charm.SetWalk(false);

            if (!Controlled.Contains(charm))
                Controlled.Add(charm);
        }
        else
        {
            charm.ClearUnitState(UnitState.Charmed);

            if (IsPlayer)
            {
                SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Charm), ObjectGuid.Empty);
                Charmed = null;
            }

            charm.SetUpdateFieldValue(charm.Values.ModifyValue(UnitData).ModifyValue(UnitData.CharmedBy), ObjectGuid.Empty);
            charm.Charmer = null;

            var player = charm.CharmerOrOwnerPlayerOrPlayerItself;

            if (charm.IsTypeId(TypeId.Player))
            {
                charm.ControlledByPlayer = true;
                charm.SetUnitFlag(UnitFlags.PlayerControlled);
                charm.AsPlayer.UpdatePvPState();
            }
            else if (player != null)
            {
                charm.ControlledByPlayer = true;
                charm.SetUnitFlag(UnitFlags.PlayerControlled);
                charm.ReplaceAllPvpFlags(player.PvpFlags);
            }
            else
            {
                charm.ControlledByPlayer = false;
                charm.RemoveUnitFlag(UnitFlags.PlayerControlled);
                charm.ReplaceAllPvpFlags(UnitPVPStateFlags.None);
            }

            if (charm.IsWalking != _isWalkingBeforeCharm)
                charm.SetWalk(_isWalkingBeforeCharm);

            if (charm.IsTypeId(TypeId.Player) || !charm.AsCreature.HasUnitTypeMask(UnitTypeMask.Minion) || charm.OwnerGUID != GUID)
                Controlled.Remove(charm);
        }

        UpdatePetCombatState();
    }

    public bool SetCharmedBy(Unit charmer, CharmType type, AuraApplication aurApp = null)
    {
        if (charmer == null)
            return false;

        // dismount players when charmed
        if (IsTypeId(TypeId.Player))
            RemoveAurasByType(AuraType.Mounted);

        if (charmer.IsTypeId(TypeId.Player))
            charmer.RemoveAurasByType(AuraType.Mounted);

        Log.Logger.Debug("SetCharmedBy: charmer {0} (GUID {1}), charmed {2} (GUID {3}), type {4}.", charmer.Entry, charmer.GUID.ToString(), Entry, GUID.ToString(), type);

        if (this == charmer)
        {
            Log.Logger.Fatal("Unit:SetCharmedBy: Unit {0} (GUID {1}) is trying to charm itself!", Entry, GUID.ToString());

            return false;
        }

        if (IsPlayer && AsPlayer.Transport != null)
        {
            Log.Logger.Fatal("Unit:SetCharmedBy: Player on transport is trying to charm {0} (GUID {1})", Entry, GUID.ToString());

            return false;
        }

        // Already charmed
        if (!CharmerGUID.IsEmpty)
        {
            Log.Logger.Fatal("Unit:SetCharmedBy: {0} (GUID {1}) has already been charmed but {2} (GUID {3}) is trying to charm it!", Entry, GUID.ToString(), charmer.Entry, charmer.GUID.ToString());

            return false;
        }

        CastStop();
        AttackStop();

        var playerCharmer = charmer.AsPlayer;

        // Charmer stop charming
        if (playerCharmer != null)
        {
            playerCharmer.StopCastingCharm();
            playerCharmer.StopCastingBindSight();
        }

        // Charmed stop charming
        if (IsTypeId(TypeId.Player))
        {
            AsPlayer.StopCastingCharm();
            AsPlayer.StopCastingBindSight();
        }

        // StopCastingCharm may remove a possessed pet?
        if (!Location.IsInWorld)
        {
            Log.Logger.Fatal("Unit:SetCharmedBy: {0} (GUID {1}) is not in world but {2} (GUID {3}) is trying to charm it!", Entry, GUID.ToString(), charmer.Entry, charmer.GUID.ToString());

            return false;
        }

        // charm is set by aura, and aura effect remove handler was called during apply handler execution
        // prevent undefined behaviour
        if (aurApp != null && aurApp.RemoveMode != 0)
            return false;

        _oldFactionId = Faction;
        Faction = charmer.Faction;

        // Pause any Idle movement
        PauseMovement(0, 0, false);

        // Remove any active voluntary movement
        MotionMaster.Clear(MovementGeneratorPriority.Normal);

        // Stop any remaining spline, if no involuntary movement is found
        bool Criteria(MovementGenerator movement) => movement.Priority == MovementGeneratorPriority.Highest;

        if (!MotionMaster.HasMovementGenerator(Criteria))
            StopMoving();

        // Set charmed
        charmer.SetCharm(this, true);

        var player = AsPlayer;

        if (player != null)
        {
            if (player.IsAfk)
                player.ToggleAfk();

            player.SetClientControl(this, false);
        }

        // charm is set by aura, and aura effect remove handler was called during apply handler execution
        // prevent undefined behaviour
        if (aurApp != null && aurApp.RemoveMode != 0)
        {
            // properly clean up charm changes up to this point to avoid leaving the unit in partially charmed state
            Faction = _oldFactionId;
            MotionMaster.InitializeDefault();
            charmer.SetCharm(this, false);

            return false;
        }

        // Pets already have a properly initialized CharmInfo, don't overwrite it.
        if (type != CharmType.Vehicle && GetCharmInfo() == null)
        {
            InitCharmInfo();

            if (type == CharmType.Possess)
                GetCharmInfo().InitPossessCreateSpells();
            else
                GetCharmInfo().InitCharmCreateSpells();
        }

        if (playerCharmer != null)
            switch (type)
            {
                case CharmType.Vehicle:
                    SetUnitFlag(UnitFlags.Possessed);
                    playerCharmer.SetClientControl(this, true);
                    playerCharmer.VehicleSpellInitialize();

                    break;
                case CharmType.Possess:
                    SetUnitFlag(UnitFlags.Possessed);
                    charmer.SetUnitFlag(UnitFlags.RemoveClientControl);
                    playerCharmer.SetClientControl(this, true);
                    playerCharmer.PossessSpellInitialize();
                    AddUnitState(UnitState.Possessed);

                    break;
                case CharmType.Charm:
                    if (IsTypeId(TypeId.Unit) && charmer.Class == PlayerClass.Warlock)
                    {
                        var cinfo = AsCreature.Template;

                        if (cinfo is { CreatureType: CreatureType.Demon })
                        {
                            // to prevent client crash
                            Class = PlayerClass.Mage;

                            // just to enable stat window
                            if (GetCharmInfo() != null)
                                GetCharmInfo().SetPetNumber(GameObjectManager.GeneratePetNumber(), true);

                            // if charmed two demons the same session, the 2nd gets the 1st one's name
                            SetPetNameTimestamp((uint)GameTime.CurrentTime); // cast can't be helped
                        }
                    }

                    playerCharmer.CharmSpellInitialize();

                    break;
                default:
                case CharmType.Convert:
                    break;
            }

        AddUnitState(UnitState.Charmed);

        var creature = AsCreature;

        creature?.RefreshCanSwimFlag();

        if (!IsPlayer || !charmer.IsPlayer)
        {
            // AI will schedule its own change if appropriate
            var ai = AI;

            if (ai != null)
                ai.OnCharmed(false);
            else
                ScheduleAIChange();
        }

        return true;
    }

    public void SetMinion(Minion minion, bool apply)
    {
        Log.Logger.Debug("SetMinion {0} for {1}, apply {2}", minion.Entry, Entry, apply);

        if (apply)
        {
            if (!minion.OwnerGUID.IsEmpty)
            {
                Log.Logger.Fatal("SetMinion: Minion {0} is not the minion of owner {1}", minion.Entry, Entry);

                return;
            }

            if (!Location.IsInWorld)
            {
                Log.Logger.Fatal($"SetMinion: Minion being added to owner not in world. Minion: {minion.GUID}, Owner: {GetDebugInfo()}");

                return;
            }

            minion.SetOwnerGUID(GUID);

            if (!Controlled.Contains(minion))
                Controlled.Add(minion);

            if (IsTypeId(TypeId.Player))
            {
                minion.ControlledByPlayer = true;
                minion.SetUnitFlag(UnitFlags.PlayerControlled);
            }

            // Can only have one pet. If a new one is summoned, dismiss the old one.
            if (minion.IsGuardianPet)
            {
                var oldPet = GetGuardianPet();

                if (oldPet != null)
                {
                    if (oldPet != minion && (oldPet.IsPet || minion.IsPet || oldPet.Entry != minion.Entry))
                    {
                        // remove existing minion pet
                        var oldPetAsPet = oldPet.AsPet;

                        if (oldPetAsPet != null)
                            oldPetAsPet.Remove(PetSaveMode.NotInSlot);
                        else
                            oldPet.UnSummon();

                        PetGUID = minion.GUID;
                        MinionGUID = ObjectGuid.Empty;
                    }
                }
                else
                {
                    PetGUID = minion.GUID;
                    MinionGUID = ObjectGuid.Empty;
                }
            }

            if (minion.HasUnitTypeMask(UnitTypeMask.ControlableGuardian))
                if (MinionGUID.IsEmpty)
                    MinionGUID = minion.GUID;

            var properties = minion.SummonPropertiesRecord;

            if (properties is { Title: SummonTitle.Companion })
            {
                CritterGUID = minion.GUID;
                var thisPlayer = AsPlayer;

                if (thisPlayer != null)
                    if (properties.GetFlags().HasFlag(SummonPropertiesFlags.SummonFromBattlePetJournal))
                    {
                        var pet = thisPlayer.Session.BattlePetMgr.GetPet(thisPlayer.SummonedBattlePetGUID);

                        if (pet != null)
                        {
                            minion.BattlePetCompanionGUID = thisPlayer.SummonedBattlePetGUID;
                            minion.BattlePetCompanionNameTimestamp = (uint)pet.NameTimestamp;
                            minion.WildBattlePetLevel = pet.PacketInfo.Level;

                            var display = pet.PacketInfo.DisplayID;

                            if (display != 0)
                            {
                                minion.SetDisplayId(display);
                                minion.SetNativeDisplayId(display);
                            }
                        }
                    }
            }

            // PvP, FFAPvP
            minion.ReplaceAllPvpFlags(PvpFlags);

            // FIXME: hack, speed must be set only at follow
            if (IsTypeId(TypeId.Player) && minion.IsPet)
                for (UnitMoveType i = 0; i < UnitMoveType.Max; ++i)
                    minion.SetSpeedRate(i, SpeedRate[(int)i]);

            // Send infinity cooldown - client does that automatically but after relog cooldown needs to be set again
            var spellInfo = SpellManager.GetSpellInfo(minion.UnitData.CreatedBySpell);

            if (spellInfo is { IsCooldownStartedOnEvent: true })
                SpellHistory.StartCooldown(spellInfo, 0, null, true);
        }
        else
        {
            if (minion.OwnerGUID != GUID)
            {
                Log.Logger.Fatal("SetMinion: Minion {0} is not the minion of owner {1}", minion.Entry, Entry);

                return;
            }

            Controlled.Remove(minion);

            if (minion.SummonPropertiesRecord is { Title: SummonTitle.Companion })
                if (CritterGUID == minion.GUID)
                    CritterGUID = ObjectGuid.Empty;

            if (minion.IsGuardianPet)
            {
                if (PetGUID == minion.GUID)
                    PetGUID = ObjectGuid.Empty;
            }
            else if (minion.IsTotem)
            {
                // All summoned by totem minions must disappear when it is removed.
                var spInfo = SpellManager.GetSpellInfo(minion.ToTotem().GetSpell());

                if (spInfo != null)
                    foreach (var spellEffectInfo in spInfo.Effects)
                    {
                        if (spellEffectInfo == null || !spellEffectInfo.IsEffectName(SpellEffectName.Summon))
                            continue;

                        RemoveAllMinionsByEntry((uint)spellEffectInfo.MiscValue);
                    }
            }

            var spellInfo = SpellManager.GetSpellInfo(minion.UnitData.CreatedBySpell);

            // Remove infinity cooldown
            if (spellInfo != null && spellInfo.IsCooldownStartedOnEvent)
                SpellHistory.SendCooldownEvent(spellInfo);

            if (MinionGUID == minion.GUID)
            {
                MinionGUID = ObjectGuid.Empty;

                // Check if there is another minion
                foreach (var unit in Controlled)
                {
                    // do not use this check, creature do not have charm guid
                    if (GUID == unit.CharmerGUID)
                        continue;

                    if (unit.OwnerGUID != GUID)
                        continue;

                    if (!unit.HasUnitTypeMask(UnitTypeMask.Guardian))
                        continue;

                    MinionGUID = unit.GUID;

                    // show another pet bar if there is no charm bar
                    if (TypeId == TypeId.Player && CharmedGUID.IsEmpty)
                    {
                        if (unit.IsPet)
                            AsPlayer.PetSpellInitialize();
                        else
                            AsPlayer.CharmSpellInitialize();
                    }

                    break;
                }
            }
        }

        UpdatePetCombatState();
    }

    public void UpdateCharmAI()
    {
        if (IsCharmed)
        {
            IUnitAI newAI = null;

            if (IsPlayer)
            {
                var charmer = Charmer;

                if (charmer != null)
                {
                    // first, we check if the creature's own AI specifies an override playerai for its owned players
                    var creatureCharmer = charmer.AsCreature;

                    if (creatureCharmer != null)
                    {
                        var charmerAI = creatureCharmer.AI;

                        if (charmerAI != null)
                            newAI = charmerAI.GetAIForCharmedPlayer(AsPlayer);
                    }
                    else
                        Log.Logger.Error($"Attempt to assign charm AI to player {GUID} who is charmed by non-creature {CharmerGUID}.");
                }

                if (newAI == null) // otherwise, we default to the generic one
                    newAI = new SimpleCharmedPlayerAI(AsPlayer);
            }
            else
            {
                if (IsPossessed || IsVehicle)
                    newAI = new PossessedAI(AsCreature);
                else
                    newAI = new PetAI(AsCreature);
            }

            AI = newAI;
            newAI.OnCharmed(true);
        }
        else
        {
            RestoreDisabledAI();
            // Hack: this is required because we want to call OnCharmed(true) on the restored AI
            RefreshAI();
            var ai = AI;

            ai?.OnCharmed(true);
        }
    }

    public void UpdatePetCombatState()
    {
        var state = false;

        foreach (var minion in Controlled)
            if (minion.IsInCombat)
            {
                state = true;

                break;
            }

        if (state)
            SetUnitFlag(UnitFlags.PetInCombat);
        else
            RemoveUnitFlag(UnitFlags.PetInCombat);
    }

    private void DeleteCharmInfo()
    {
        if (_charmInfo == null)
            return;

        _charmInfo.RestoreState();
        _charmInfo = null;
    }

    private bool InitTamedPet(Pet pet, uint level, uint spellID)
    {
        var player = AsPlayer;
        var petStable = player.PetStable;

        var freeActiveSlot = Array.FindIndex(petStable.ActivePets, petInfo => petInfo == null);

        if (freeActiveSlot == -1)
            return false;

        pet.SetCreatorGUID(GUID);
        pet.Faction = Faction;
        pet.SetCreatedBySpell(spellID);

        if (IsTypeId(TypeId.Player))
            pet.SetUnitFlag(UnitFlags.PlayerControlled);

        if (!pet.InitStatsForLevel(level))
        {
            Log.Logger.Error("Pet:InitStatsForLevel() failed for creature (Entry: {0})!", pet.Entry);

            return false;
        }

        PhasingHandler.InheritPhaseShift(pet, this);

        pet.GetCharmInfo().SetPetNumber(GameObjectManager.GeneratePetNumber(), true);
        // this enables pet details window (Shift+P)
        pet.InitPetCreateSpells();
        pet.SetFullHealth();

        petStable.SetCurrentActivePetIndex((uint)freeActiveSlot);

        PetStable.PetInfo petInfo = new();
        pet.FillPetInfo(petInfo);
        petStable.ActivePets[freeActiveSlot] = petInfo;

        return true;
    }
}
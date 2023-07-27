// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Pet;
using Forged.MapServer.Networking.Packets.Query;
using Forged.MapServer.Networking.Packets.Spell;
using Forged.MapServer.Server;
using Forged.MapServer.Spells;
using Framework.Constants;
using Framework.Database;
using Game.Common.Handlers;
using Serilog;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class PetHandler : IWorldSessionHandler
{
    private readonly CharacterDatabase _characterDatabase;
    private readonly ObjectAccessor _objectAccessor;
    private readonly GameObjectManager _objectManager;
    private readonly WorldSession _session;
    private readonly SpellManager _spellManager;

    public PetHandler(WorldSession session, ObjectAccessor objectAccessor, SpellManager spellManager, GameObjectManager objectManager, CharacterDatabase characterDatabase)
    {
        _session = session;
        _objectAccessor = objectAccessor;
        _spellManager = spellManager;
        _objectManager = objectManager;
        _characterDatabase = characterDatabase;
    }

    public bool CheckStableMaster(ObjectGuid guid)
    {
        // spell case or GM
        if (guid == _session.Player.GUID)
        {
            if (_session.Player.IsGameMaster || _session.Player.HasAuraType(AuraType.OpenStable))
                return true;

            Log.Logger.Debug("{0} attempt open stable in cheating way.", guid.ToString());

            return false;
        }

        // stable master case
        if (_session.Player.GetNPCIfCanInteractWith(guid, NPCFlags.StableMaster, NPCFlags2.None) != null)
            return true;

        Log.Logger.Debug("Stablemaster {0} not found or you can't interact with him.", guid.ToString());

        return false;
    }

    [WorldPacketHandler(ClientOpcodes.DismissCritter)]
    private void HandleDismissCritter(DismissCritter packet)
    {
        Unit pet = ObjectAccessor.GetCreatureOrPetOrVehicle(_session.Player, packet.CritterGUID);

        if (pet == null)
        {
            Log.Logger.Debug("Critter {0} does not exist - player '{1}' ({2} / account: {3}) attempted to dismiss it (possibly lagged out)",
                             packet.CritterGUID.ToString(),
                             _session.Player.GetName(),
                             _session.Player.GUID.ToString(),
                             _session.AccountId);

            return;
        }

        if (_session.Player.CritterGUID != pet.GUID || !pet.IsCreature || !pet.IsSummon)
            return;

        if (!_session.Player.SummonedBattlePetGUID.IsEmpty && _session.Player.SummonedBattlePetGUID == pet.BattlePetCompanionGUID)
            _session.Player.SetBattlePetData();

        pet.ToTempSummon().UnSummon();
    }

    [WorldPacketHandler(ClientOpcodes.PetAbandon)]
    private void HandlePetAbandon(PetAbandon packet)
    {
        // pet/charmed
        var creature = ObjectAccessor.GetCreatureOrPetOrVehicle(_session.Player, packet.Pet);

        if (creature.TryGetAsPet(out var pet) && pet.PetType == PetType.Hunter)
            _session.Player.RemovePet(pet, PetSaveMode.AsDeleted);
    }

    [WorldPacketHandler(ClientOpcodes.PetAction)]
    private void HandlePetAction(PetAction packet)
    {
        var guid1 = packet.PetGUID;    //pet guid
        var guid2 = packet.TargetGUID; //tag guid

        var spellid = UnitActionBarEntry.UNIT_ACTION_BUTTON_ACTION(packet.Action);
        var flag = (ActiveStates)UnitActionBarEntry.UNIT_ACTION_BUTTON_TYPE(packet.Action); //delete = 0x07 CastSpell = C1

        // used also for charmed creature
        var pet = _objectAccessor.GetUnit(_session.Player, guid1);

        if (pet == null)
        {
            Log.Logger.Error("HandlePetAction: {0} doesn't exist for {1}", guid1.ToString(), _session.Player.GUID.ToString());

            return;
        }

        if (pet != _session.Player.GetFirstControlled())
        {
            Log.Logger.Error("HandlePetAction: {0} does not belong to {1}", guid1.ToString(), _session.Player.GUID.ToString());

            return;
        }

        if (!pet.IsAlive)
        {
            var spell = flag is ActiveStates.Enabled or ActiveStates.Passive ? _spellManager.GetSpellInfo(spellid, pet.Location.Map.DifficultyID) : null;

            if (spell == null)
                return;

            if (!spell.HasAttribute(SpellAttr0.AllowCastWhileDead))
                return;
        }

        // @todo allow control charmed player?
        if (pet.IsTypeId(TypeId.Player) && !(flag == ActiveStates.Command && spellid == (uint)CommandStates.Attack))
            return;

        if (_session.Player.Controlled.Count == 1)
            HandlePetActionHelper(pet, guid1, spellid, flag, guid2, packet.ActionPosition.X, packet.ActionPosition.Y, packet.ActionPosition.Z);
        else
        {
            //If a pet is dismissed, m_Controlled will change
            List<Unit> controlled = new();

            foreach (var unit in _session.Player.Controlled)
                if (unit.Entry == pet.Entry && unit.IsAlive)
                    controlled.Add(unit);

            foreach (var unit in controlled)
                HandlePetActionHelper(unit, guid1, spellid, flag, guid2, packet.ActionPosition.X, packet.ActionPosition.Y, packet.ActionPosition.Z);
        }
    }

    private void HandlePetActionHelper(Unit pet, ObjectGuid guid1, uint spellid, ActiveStates flag, ObjectGuid guid2, float x, float y, float z)
    {
        var charmInfo = pet.GetCharmInfo();

        if (charmInfo == null)
        {
            Log.Logger.Error("WorldSession.HandlePetAction(petGuid: {0}, tagGuid: {1}, spellId: {2}, Id: {3}): object (GUID: {4} Entry: {5} TypeId: {6}) is considered pet-like but doesn't have a charminfo!",
                             guid1,
                             guid2,
                             spellid,
                             flag,
                             pet.GUID.ToString(),
                             pet.Entry,
                             pet.TypeId);

            return;
        }

        switch (flag)
        {
            case ActiveStates.Command: //0x07
                switch ((CommandStates)spellid)
                {
                    case CommandStates.Stay: // flat = 1792  //STAY
                        pet.MotionMaster.Clear(MovementGeneratorPriority.Normal);
                        pet.MotionMaster.MoveIdle();
                        charmInfo.CommandState = CommandStates.Stay;

                        charmInfo.IsCommandAttack = false;
                        charmInfo.IsAtStay = true;
                        charmInfo.IsCommandFollow = false;
                        charmInfo.IsFollowing = false;
                        charmInfo.IsReturning = false;
                        charmInfo.SaveStayPosition();

                        break;

                    case CommandStates.Follow: // spellid = 1792  //FOLLOW
                        pet.AttackStop();
                        pet.InterruptNonMeleeSpells(false);
                        pet.MotionMaster.MoveFollow(_session.Player, SharedConst.PetFollowDist, pet.FollowAngle);
                        charmInfo.CommandState = CommandStates.Follow;

                        charmInfo.IsCommandAttack = false;
                        charmInfo.IsAtStay = false;
                        charmInfo.IsReturning = true;
                        charmInfo.IsCommandFollow = true;
                        charmInfo.IsFollowing = false;

                        break;

                    case CommandStates.Attack: // spellid = 1792  //ATTACK
                    {
                        // Can't attack if owner is pacified
                        if (_session.Player.HasAuraType(AuraType.ModPacify))
                            // @todo Send proper error message to client
                            return;

                        // only place where pet can be player
                        var targetUnit = _objectAccessor.GetUnit(_session.Player, guid2);

                        if (targetUnit == null)
                            return;

                        var owner = pet.OwnerUnit;

                        if (owner != null)
                            if (!owner.WorldObjectCombat.IsValidAttackTarget(targetUnit))
                                return;

                        // This is true if pet has no target or has target but targets differs.
                        if (pet.Victim != targetUnit || !pet.GetCharmInfo().IsCommandAttack)
                        {
                            if (pet.Victim != null)
                                pet.AttackStop();

                            if (!pet.IsTypeId(TypeId.Player) && pet.AsCreature.IsAIEnabled)
                            {
                                charmInfo.IsCommandAttack = true;
                                charmInfo.IsAtStay = false;
                                charmInfo.IsFollowing = false;
                                charmInfo.IsCommandFollow = false;
                                charmInfo.IsReturning = false;

                                var creatureAI = pet.AsCreature.AI;

                                if (creatureAI is PetAI ai)
                                    ai._AttackStart(targetUnit); // force target switch
                                else
                                    creatureAI.AttackStart(targetUnit);

                                //10% chance to play special pet attack talk, else growl
                                if (pet.IsPet && pet.AsPet.PetType == PetType.Summon && pet != targetUnit && RandomHelper.IRand(0, 100) < 10)
                                    pet.SendPetTalk(PetTalk.Attack);
                                else
                                    // 90% chance for pet and 100% chance for charmed creature
                                    pet.SendPetAIReaction(guid1);
                            }
                            else // charmed player
                            {
                                charmInfo.IsCommandAttack = true;
                                charmInfo.IsAtStay = false;
                                charmInfo.IsFollowing = false;
                                charmInfo.IsCommandFollow = false;
                                charmInfo.IsReturning = false;

                                pet.Attack(targetUnit, true);
                                pet.SendPetAIReaction(guid1);
                            }
                        }

                        break;
                    }
                    case CommandStates.Abandon: // abandon (hunter pet) or dismiss (summoned pet)
                        if (pet.CharmerGUID == _session.Player.GUID)
                            _session.Player.StopCastingCharm();
                        else if (pet.OwnerGUID == _session.Player.GUID)
                        {
                            if (pet.IsPet)
                            {
                                _session.Player.RemovePet(pet.AsPet, pet.AsPet.PetType == PetType.Hunter ? PetSaveMode.AsDeleted : PetSaveMode.NotInSlot);
                            }
                            else if (pet.HasUnitTypeMask(UnitTypeMask.Minion))
                                ((Minion)pet).UnSummon();
                        }

                        break;

                    case CommandStates.MoveTo:
                        pet.StopMoving();
                        pet.MotionMaster.Clear();
                        pet.MotionMaster.MovePoint(0, x, y, z);
                        charmInfo.CommandState = CommandStates.MoveTo;

                        charmInfo.IsCommandAttack = false;
                        charmInfo.IsAtStay = true;
                        charmInfo.IsFollowing = false;
                        charmInfo.IsReturning = false;
                        charmInfo.SaveStayPosition();

                        break;

                    default:
                        Log.Logger.Error("WORLD: unknown PET Id Action {0} and spellid {1}.", flag, spellid);

                        break;
                }

                break;

            case ActiveStates.Reaction: // 0x6
                switch ((ReactStates)spellid)
                {
                    case ReactStates.Passive: //passive
                        pet.AttackStop();
                        goto case ReactStates.Defensive;
                    case ReactStates.Defensive:  //recovery
                    case ReactStates.Aggressive: //activete
                        if (pet.IsTypeId(TypeId.Unit))
                            pet.AsCreature.ReactState = (ReactStates)spellid;

                        break;
                }

                break;

            case ActiveStates.Disabled: // 0x81    spell (disabled), ignore
            case ActiveStates.Passive:  // 0x01
            case ActiveStates.Enabled:  // 0xC1    spell
            {
                Unit unitTarget = null;

                if (!guid2.IsEmpty)
                    unitTarget = _objectAccessor.GetUnit(_session.Player, guid2);

                // do not cast unknown spells
                var spellInfo = _spellManager.GetSpellInfo(spellid, pet.Location.Map.DifficultyID);

                if (spellInfo == null)
                {
                    Log.Logger.Error("WORLD: unknown PET spell id {0}", spellid);

                    return;
                }

                foreach (var spellEffectInfo in spellInfo.Effects)
                    if (spellEffectInfo.TargetA.Target == Targets.UnitSrcAreaEnemy || spellEffectInfo.TargetA.Target == Targets.UnitDestAreaEnemy || spellEffectInfo.TargetA.Target == Targets.DestDynobjEnemy)
                        return;

                // do not cast not learned spells
                if (!pet.HasSpell(spellid) || spellInfo.IsPassive)
                    return;

                //  Clear the flags as if owner clicked 'attack'. AI will reset them
                //  after AttackStart, even if spell failed
                if (pet.GetCharmInfo() != null)
                {
                    pet.GetCharmInfo().IsAtStay = false;
                    pet.GetCharmInfo().IsCommandAttack = true;
                    pet.GetCharmInfo().IsReturning = false;
                    pet.GetCharmInfo().IsFollowing = false;
                }

                var spell = pet.SpellFactory.NewSpell(spellInfo, TriggerCastFlags.None);

                var result = spell.CheckPetCast(unitTarget);

                //auto turn to target unless possessed
                if (result == SpellCastResult.UnitNotInfront && !pet.IsPossessed && !pet.IsVehicle)
                {
                    var targetsUnitTarget = spell.Targets.UnitTarget;

                    if (unitTarget != null)
                    {
                        if (!pet.HasSpellFocus())
                            pet.SetInFront(unitTarget);

                        var player = unitTarget.AsPlayer;

                        if (player != null)
                            pet.SendUpdateToPlayer(player);
                    }
                    else if (targetsUnitTarget != null)
                    {
                        if (!pet.HasSpellFocus())
                            pet.SetInFront(targetsUnitTarget);

                        var player = targetsUnitTarget.AsPlayer;

                        if (player != null)
                            pet.SendUpdateToPlayer(player);
                    }

                    var powner = pet.CharmerOrOwner;

                    if (powner != null)
                    {
                        var player = powner.AsPlayer;

                        if (player != null)
                            pet.SendUpdateToPlayer(player);
                    }

                    result = SpellCastResult.SpellCastOk;
                }

                if (result == SpellCastResult.SpellCastOk)
                {
                    unitTarget = spell.Targets.UnitTarget;

                    //10% chance to play special pet attack talk, else growl
                    //actually this only seems to happen on special spells, fire shield for imp, torment for voidwalker, but it's stupid to check every spell
                    if (pet.IsPet && pet.AsPet.PetType == PetType.Summon && pet != unitTarget && RandomHelper.IRand(0, 100) < 10)
                        pet.SendPetTalk(PetTalk.SpecialSpell);
                    else
                        pet.SendPetAIReaction(guid1);

                    if (unitTarget != null && !_session.Player.WorldObjectCombat.IsFriendlyTo(unitTarget) && !pet.IsPossessed && !pet.IsVehicle)
                        // This is true if pet has no target or has target but targets differs.
                        if (pet.Victim != unitTarget)
                        {
                            var ai = pet.AsCreature.AI;

                            if (ai != null)
                            {
                                if (ai is PetAI petAI)
                                    petAI._AttackStart(unitTarget); // force victim switch
                                else
                                    ai.AttackStart(unitTarget);
                            }
                        }

                    spell.Prepare(spell.Targets);
                }
                else
                {
                    if (pet.IsPossessed || pet.IsVehicle) // @todo: confirm this check
                        Spell.SendCastResult(_session.Player, spellInfo, spell.SpellVisual, spell.CastId, result);
                    else
                        spell.SendPetCastResult(result);

                    if (!pet.SpellHistory.HasCooldown(spellid))
                        pet.SpellHistory.ResetCooldown(spellid, true);

                    spell.Finish(result);
                    spell.Dispose();

                    // reset specific flags in case of spell fail. AI will reset other flags
                    if (pet.GetCharmInfo() != null)
                        pet.GetCharmInfo().IsCommandAttack = false;
                }

                break;
            }
            default:
                Log.Logger.Error("WORLD: unknown PET Id Action {0} and spellid {1}.", flag, spellid);

                break;
        }
    }

    [WorldPacketHandler(ClientOpcodes.PetCastSpell, Processing = PacketProcessing.Inplace)]
    private void HandlePetCastSpell(PetCastSpell petCastSpell)
    {
        var caster = _objectAccessor.GetUnit(_session.Player, petCastSpell.PetGUID);

        if (caster == null)
        {
            Log.Logger.Error("WorldSession.HandlePetCastSpell: Caster {0} not found.", petCastSpell.PetGUID.ToString());

            return;
        }

        var spellInfo = _spellManager.GetSpellInfo(petCastSpell.Cast.SpellID, caster.Location.Map.DifficultyID);

        if (spellInfo == null)
        {
            Log.Logger.Error("WorldSession.HandlePetCastSpell: unknown spell id {0} tried to cast by {1}", petCastSpell.Cast.SpellID, petCastSpell.PetGUID.ToString());

            return;
        }

        // This opcode is also sent from charmed and possessed units (players and creatures)
        if (caster != _session.Player.GetGuardianPet() && caster != _session.Player.Charmed)
        {
            Log.Logger.Error("WorldSession.HandlePetCastSpell: {0} isn't pet of player {1} ({2}).", petCastSpell.PetGUID.ToString(), _session.Player.GetName(), _session.Player.GUID.ToString());

            return;
        }

        SpellCastTargets targets = new(caster, petCastSpell.Cast);

        var triggerCastFlags = TriggerCastFlags.None;

        if (spellInfo.IsPassive)
            return;

        // cast only learned spells
        if (!caster.HasSpell(spellInfo.Id))
        {
            var allow = false;

            // allow casting of spells triggered by clientside periodic trigger auras
            if (caster.HasAuraTypeWithTriggerSpell(AuraType.PeriodicTriggerSpellFromClient, spellInfo.Id))
            {
                allow = true;
                triggerCastFlags = TriggerCastFlags.FullMask;
            }

            if (!allow)
                return;
        }

        var spell = caster.SpellFactory.NewSpell(spellInfo, triggerCastFlags);
        spell.FromClient = true;
        spell.SpellMisc.Data0 = petCastSpell.Cast.Misc[0];
        spell.SpellMisc.Data1 = petCastSpell.Cast.Misc[1];
        spell.Targets = targets;

        var result = spell.CheckPetCast(null);

        if (result == SpellCastResult.SpellCastOk)
        {
            var creature = caster.AsCreature;

            if (creature != null)
            {
                var pet = creature.AsPet;

                if (pet != null)
                {
                    // 10% chance to play special pet attack talk, else growl
                    // actually this only seems to happen on special spells, fire shield for imp, torment for voidwalker, but it's stupid to check every spell
                    if (pet.PetType == PetType.Summon && RandomHelper.IRand(0, 100) < 10)
                        pet.SendPetTalk(PetTalk.SpecialSpell);
                    else
                        pet.SendPetAIReaction(petCastSpell.PetGUID);
                }
            }

            SpellPrepare spellPrepare = new()
            {
                ClientCastID = petCastSpell.Cast.CastID,
                ServerCastID = spell.CastId
            };

            _session.SendPacket(spellPrepare);

            spell.Prepare(targets);
        }
        else
        {
            spell.SendPetCastResult(result);

            if (!caster.SpellHistory.HasCooldown(spellInfo.Id))
                caster.SpellHistory.ResetCooldown(spellInfo.Id, true);

            spell.Finish(result);
            spell.Dispose();
        }
    }

    [WorldPacketHandler(ClientOpcodes.PetRename)]
    private void HandlePetRename(PetRename packet)
    {
        var petguid = packet.RenameData.PetGUID;
        var isdeclined = packet.RenameData.HasDeclinedNames;
        var name = packet.RenameData.NewName;

        var petStable = _session.Player.PetStable;
        var pet = ObjectAccessor.GetPet(_session.Player, petguid);

        // check it!
        if (pet is not { IsPet: true } ||
            pet.AsPet.PetType != PetType.Hunter ||
            !pet.HasPetFlag(UnitPetFlags.CanBeRenamed) ||
            pet.OwnerGUID != _session.Player.GUID ||
            pet.GetCharmInfo() == null ||
            petStable?.GetCurrentPet() == null ||
            petStable.GetCurrentPet().PetNumber != pet.GetCharmInfo().PetNumber)
            return;

        var res = _objectManager.CheckPetName(name);

        if (res != PetNameInvalidReason.Success)
        {
            SendPetNameInvalid(res, name, null);

            return;
        }

        if (_objectManager.IsReservedName(name))
        {
            SendPetNameInvalid(PetNameInvalidReason.Reserved, name, null);

            return;
        }

        pet.SetName(name);
        pet.GroupUpdateFlag = GroupUpdatePetFlags.Name;
        pet.RemovePetFlag(UnitPetFlags.CanBeRenamed);

        petStable.GetCurrentPet().Name = name;
        petStable.GetCurrentPet().WasRenamed = true;

        PreparedStatement stmt;
        SQLTransaction trans = new();

        if (isdeclined)
        {
            stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_PET_DECLINEDNAME);
            stmt.AddValue(0, pet.GetCharmInfo().PetNumber);
            trans.Append(stmt);

            stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_CHAR_PET_DECLINEDNAME);
            stmt.AddValue(0, pet.GetCharmInfo().PetNumber);
            stmt.AddValue(1, _session.Player.GUID.ToString());

            for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; i++)
                stmt.AddValue(i + 1, packet.RenameData.DeclinedNames.Name[i]);

            trans.Append(stmt);
        }

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_PET_NAME);
        stmt.AddValue(0, name);
        stmt.AddValue(1, _session.Player.GUID.ToString());
        stmt.AddValue(2, pet.GetCharmInfo().PetNumber);
        trans.Append(stmt);

        _characterDatabase.CommitTransaction(trans);

        pet.SetPetNameTimestamp((uint)GameTime.CurrentTime); // cast can't be helped
    }

    [WorldPacketHandler(ClientOpcodes.PetSetAction)]
    private void HandlePetSetAction(PetSetAction packet)
    {
        var petguid = packet.PetGUID;
        var pet = _objectAccessor.GetUnit(_session.Player, petguid);

        if (pet == null || pet != _session.Player.GetFirstControlled())
        {
            Log.Logger.Error("HandlePetSetAction: Unknown {0} or pet owner {1}", petguid.ToString(), _session.Player.GUID.ToString());

            return;
        }

        var charmInfo = pet.GetCharmInfo();

        if (charmInfo == null)
        {
            Log.Logger.Error("WorldSession.HandlePetSetAction: {0} is considered pet-like but doesn't have a charminfo!", pet.GUID.ToString());

            return;
        }

        List<Unit> pets = new();

        foreach (var controlled in _session.Player.Controlled)
            if (controlled.Entry == pet.Entry && controlled.IsAlive)
                pets.Add(controlled);

        var position = packet.Index;
        var actionData = packet.Action;

        var spellId = UnitActionBarEntry.UNIT_ACTION_BUTTON_ACTION(actionData);
        var actState = (ActiveStates)UnitActionBarEntry.UNIT_ACTION_BUTTON_TYPE(actionData);

        Log.Logger.Debug("Player {0} has changed pet spell action. Position: {1}, Spell: {2}, State: {3}", _session.Player.GetName(), position, spellId, actState);

        foreach (var petControlled in pets)
            //if it's act for spell (en/disable/cast) and there is a spell given (0 = remove spell) which pet doesn't know, don't add
            if (!(actState is ActiveStates.Enabled or ActiveStates.Disabled or ActiveStates.Passive && spellId != 0 && !petControlled.HasSpell(spellId)))
            {
                var spellInfo = _spellManager.GetSpellInfo(spellId, petControlled.Location.Map.DifficultyID);

                if (spellInfo != null)
                {
                    switch (actState)
                    {
                        //sign for autocast
                        case ActiveStates.Enabled when petControlled.TypeId == TypeId.Unit && petControlled.IsPet:
                            ((Pet)petControlled).ToggleAutocast(spellInfo, true);

                            break;

                        case ActiveStates.Enabled:
                        {
                            foreach (var unit in _session.Player.Controlled.Where(unit => unit.Entry == petControlled.Entry))
                                unit.GetCharmInfo().ToggleCreatureAutocast(spellInfo, true);

                            break;
                        }
                        //sign for no/turn off autocast
                        case ActiveStates.Disabled when petControlled.TypeId == TypeId.Unit && petControlled.IsPet:
                            petControlled.AsPet.ToggleAutocast(spellInfo, false);

                            break;

                        case ActiveStates.Disabled:
                        {
                            foreach (var unit in _session.Player.Controlled.Where(unit => unit.Entry == petControlled.Entry))
                                unit.GetCharmInfo().ToggleCreatureAutocast(spellInfo, false);

                            break;
                        }
                    }
                }

                charmInfo.SetActionBar((byte)position, spellId, actState);
            }
    }

    [WorldPacketHandler(ClientOpcodes.PetSpellAutocast, Processing = PacketProcessing.Inplace)]
    private void HandlePetSpellAutocast(PetSpellAutocast packet)
    {
        var pet = ObjectAccessor.GetCreatureOrPetOrVehicle(_session.Player, packet.PetGUID);

        if (pet == null)
        {
            Log.Logger.Error("WorldSession.HandlePetSpellAutocast: {0} not found.", packet.PetGUID.ToString());

            return;
        }

        if (pet != _session.Player.GetGuardianPet() && pet != _session.Player.Charmed)
        {
            Log.Logger.Error("WorldSession.HandlePetSpellAutocast: {0} isn't pet of player {1} ({2}).",
                             packet.PetGUID.ToString(),
                             _session.Player.GetName(),
                             _session.Player.GUID.ToString());

            return;
        }

        var spellInfo = _spellManager.GetSpellInfo(packet.SpellID, pet.Location.Map.DifficultyID);

        if (spellInfo == null)
        {
            Log.Logger.Error("WorldSession.HandlePetSpellAutocast: Unknown spell id {0} used by {1}.", packet.SpellID, packet.PetGUID.ToString());

            return;
        }

        List<Unit> pets = new();

        foreach (var controlled in _session.Player.Controlled)
            if (controlled.Entry == pet.Entry && controlled.IsAlive)
                pets.Add(controlled);

        foreach (var petControlled in pets)
        {
            // do not add not learned spells/ passive spells
            if (!petControlled.HasSpell(packet.SpellID) || !spellInfo.IsAutocastable)
                return;

            var charmInfo = petControlled.GetCharmInfo();

            if (charmInfo == null)
            {
                Log.Logger.Error("WorldSession.HandlePetSpellAutocastOpcod: object {0} is considered pet-like but doesn't have a charminfo!", petControlled.GUID.ToString());

                return;
            }

            if (petControlled.IsPet)
                petControlled.AsPet.ToggleAutocast(spellInfo, packet.AutocastEnabled);
            else
                charmInfo.ToggleCreatureAutocast(spellInfo, packet.AutocastEnabled);

            charmInfo.SetSpellAutocast(spellInfo, packet.AutocastEnabled);
        }
    }

    [WorldPacketHandler(ClientOpcodes.PetStopAttack, Processing = PacketProcessing.Inplace)]
    private void HandlePetStopAttack(PetStopAttack packet)
    {
        Unit pet = ObjectAccessor.GetCreatureOrPetOrVehicle(_session.Player, packet.PetGUID);

        if (pet == null)
        {
            Log.Logger.Error("HandlePetStopAttack: {0} does not exist", packet.PetGUID.ToString());

            return;
        }

        if (pet != _session.Player.CurrentPet && pet != _session.Player.Charmed)
        {
            Log.Logger.Error("HandlePetStopAttack: {0} isn't a pet or charmed creature of player {1}", packet.PetGUID.ToString(), _session.Player.GetName());

            return;
        }

        if (!pet.IsAlive)
            return;

        pet.AttackStop();
    }

    [WorldPacketHandler(ClientOpcodes.QueryPetName, Processing = PacketProcessing.Inplace)]
    private void HandleQueryPetName(QueryPetName packet)
    {
        SendQueryPetNameResponse(packet.UnitGUID);
    }

    [WorldPacketHandler(ClientOpcodes.RequestPetInfo)]
    private void HandleRequestPetInfo(RequestPetInfo requestPetInfo)
    {
        if (requestPetInfo == null)
            return;
        // Handle the packet CMSG_REQUEST_PET_INFO - sent when player does ingame /reload command

        // Packet sent when player has a pet
        if (_session.Player.CurrentPet != null)
            _session.Player.PetSpellInitialize();
        else
        {
            var charm = _session.Player.Charmed;

            if (charm == null)
                return;

            // Packet sent when player has a possessed unit
            if (charm.HasUnitState(UnitState.Possessed))
                _session.Player.PossessSpellInitialize();
            // Packet sent when player controlling a vehicle
            else if (charm.HasUnitFlag(UnitFlags.PlayerControlled) && charm.HasUnitFlag(UnitFlags.Possessed))
                _session.Player.VehicleSpellInitialize();
            // Packet sent when player has a charmed unit
            else
                _session.Player.CharmSpellInitialize();
        }
    }

    private void SendPetNameInvalid(PetNameInvalidReason error, string name, DeclinedName declinedName)
    {
        PetNameInvalid petNameInvalid = new()
        {
            Result = error
        };

        petNameInvalid.RenameData.NewName = name;

        for (var i = 0; i < SharedConst.MaxDeclinedNameCases; i++)
            petNameInvalid.RenameData.DeclinedNames.Name[i] = declinedName.Name[i];

        _session.SendPacket(petNameInvalid);
    }

    private void SendQueryPetNameResponse(ObjectGuid guid)
    {
        QueryPetNameResponse response = new()
        {
            UnitGUID = guid
        };

        var unit = ObjectAccessor.GetCreatureOrPetOrVehicle(_session.Player, guid);

        if (unit != null)
        {
            response.Allow = true;
            response.Timestamp = unit.UnitData.PetNameTimestamp;
            response.Name = unit.GetName();

            var pet = unit.AsPet;

            if (pet != null)
            {
                var names = pet.GetDeclinedNames();

                if (names != null)
                {
                    response.HasDeclined = true;
                    response.DeclinedNames = names;
                }
            }
        }

        _session.Player.SendPacket(response);
    }
}
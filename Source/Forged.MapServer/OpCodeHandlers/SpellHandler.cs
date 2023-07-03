// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.L;
using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.LootManagement;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.GameObject;
using Forged.MapServer.Networking.Packets.Pet;
using Forged.MapServer.Networking.Packets.Spell;
using Forged.MapServer.Networking.Packets.Totem;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IItem;
using Forged.MapServer.Server;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Database;
using Game.Common.Handlers;
using Serilog;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class SpellHandler : IWorldSessionHandler
{
    private readonly CharacterDatabase _characterDatabase;
    private readonly CollectionMgr _collectionMgr;
    private readonly DB6Storage<LockRecord> _lockRecords;
    private readonly LootStoreBox _lootStoreBox;
    private readonly MovementHandler _movementHandler;
    private readonly ObjectAccessor _objectAccessor;
    private readonly ScriptManager _scriptManager;
    private readonly WorldSession _session;
    private readonly DB6Storage<SpellKeyboundOverrideRecord> _spellKeyboundOverrideRecords;
    private readonly SpellManager _spellManager;

    public SpellHandler(WorldSession session, SpellManager spellManager, MovementHandler movementHandler, DB6Storage<SpellKeyboundOverrideRecord> spellKeyboundOverrideRecords,
                        ObjectAccessor objectAccessor, DB6Storage<LockRecord> lockRecords, CharacterDatabase characterDatabase, LootStoreBox lootStoreBox,
                        CollectionMgr collectionMgr, ScriptManager scriptManager)
    {
        _session = session;
        _spellManager = spellManager;
        _movementHandler = movementHandler;
        _spellKeyboundOverrideRecords = spellKeyboundOverrideRecords;
        _objectAccessor = objectAccessor;
        _lockRecords = lockRecords;
        _characterDatabase = characterDatabase;
        _lootStoreBox = lootStoreBox;
        _collectionMgr = collectionMgr;
        _scriptManager = scriptManager;
    }

    [WorldPacketHandler(ClientOpcodes.CancelAura, Processing = PacketProcessing.Inplace)]
    private void HandleCancelAura(CancelAura cancelAura)
    {
        var spellInfo = _spellManager.GetSpellInfo(cancelAura.SpellID, _session.Player.Location.Map.DifficultyID);

        if (spellInfo == null)
            return;

        // not allow remove spells with attr SPELL_ATTR0_CANT_CANCEL
        if (spellInfo.HasAttribute(SpellAttr0.NoAuraCancel))
            return;

        // channeled spell case (it currently casted then)
        if (spellInfo.IsChanneled)
        {
            var curSpell = _session.Player.GetCurrentSpell(CurrentSpellTypes.Channeled);

            if (curSpell != null && curSpell.SpellInfo.Id == cancelAura.SpellID)
                _session.Player.InterruptSpell(CurrentSpellTypes.Channeled);

            return;
        }

        // non channeled case:
        // don't allow remove non positive spells
        // don't allow cancelling passive auras (some of them are visible)
        if (!spellInfo.IsPositive || spellInfo.IsPassive)
            return;

        _session.Player.RemoveOwnedAura(cancelAura.SpellID, cancelAura.CasterGUID, AuraRemoveMode.Cancel);
    }

    [WorldPacketHandler(ClientOpcodes.CancelAutoRepeatSpell, Processing = PacketProcessing.Inplace)]
    private void HandleCancelAutoRepeatSpell(CancelAutoRepeatSpell packet)
    {
        //may be better send SMSG_CANCEL_AUTO_REPEAT?
        //cancel and prepare for deleting
        if (packet == null) return;
        _session.Player.InterruptSpell(CurrentSpellTypes.AutoRepeat);
    }

    [WorldPacketHandler(ClientOpcodes.CancelCast, Processing = PacketProcessing.ThreadSafe)]
    private void HandleCancelCast(CancelCast packet)
    {
        if (_session.Player.IsNonMeleeSpellCast(false))
            _session.Player.InterruptNonMeleeSpells(false, packet.SpellID, false);
    }

    [WorldPacketHandler(ClientOpcodes.CancelChannelling, Processing = PacketProcessing.Inplace)]
    private void HandleCancelChanneling(CancelChannelling cancelChanneling)
    {
        // ignore for remote control state (for player case)
        var mover = _session.Player.UnitBeingMoved;

        if (mover != _session.Player && mover.IsTypeId(TypeId.Player))
            return;

        var spellInfo = _spellManager.GetSpellInfo((uint)cancelChanneling.ChannelSpell, mover.Location.Map.DifficultyID);

        if (spellInfo == null)
            return;

        // not allow remove spells with attr SPELL_ATTR0_CANT_CANCEL
        if (spellInfo.HasAttribute(SpellAttr0.NoAuraCancel))
            return;

        var spell = mover.GetCurrentSpell(CurrentSpellTypes.Channeled);

        if (spell == null || spell.SpellInfo.Id != spellInfo.Id)
            return;

        mover.InterruptSpell(CurrentSpellTypes.Channeled);
    }

    [WorldPacketHandler(ClientOpcodes.CancelGrowthAura, Processing = PacketProcessing.Inplace)]
    private void HandleCancelGrowthAura(CancelGrowthAura cancelGrowthAura)
    {
        if (cancelGrowthAura == null) return;
        _session.Player.RemoveAurasByType(AuraType.ModScale,
                                 aurApp =>
                                 {
                                     var spellInfo = aurApp.Base.SpellInfo;

                                     return !spellInfo.HasAttribute(SpellAttr0.NoAuraCancel) && spellInfo.IsPositive && !spellInfo.IsPassive;
                                 });
    }

    [WorldPacketHandler(ClientOpcodes.CancelModSpeedNoControlAuras, Processing = PacketProcessing.Inplace)]
    private void HandleCancelModSpeedNoControlAuras(CancelModSpeedNoControlAuras cancelModSpeedNoControlAuras)
    {
        var mover = _session.Player.UnitBeingMoved;

        if (mover == null || mover.GUID != cancelModSpeedNoControlAuras.TargetGUID)
            return;

        _session.Player.RemoveAurasByType(AuraType.ModSpeedNoControl,
                                  aurApp =>
                                  {
                                      var spellInfo = aurApp.Base.SpellInfo;

                                      return !spellInfo.HasAttribute(SpellAttr0.NoAuraCancel) && spellInfo.IsPositive && !spellInfo.IsPassive;
                                  });
    }

    [WorldPacketHandler(ClientOpcodes.CancelMountAura, Processing = PacketProcessing.Inplace)]
    private void HandleCancelMountAura(CancelMountAura packet)
    {
        _session.Player.RemoveAurasByType(AuraType.Mounted,
                                 aurApp =>
                                 {
                                     var spellInfo = aurApp.Base.SpellInfo;

                                     return !spellInfo.HasAttribute(SpellAttr0.NoAuraCancel) && spellInfo.IsPositive && !spellInfo.IsPassive;
                                 });
    }

    [WorldPacketHandler(ClientOpcodes.CastSpell, Processing = PacketProcessing.ThreadSafe)]
    private void HandleCastSpell(CastSpell cast)
    {
        // ignore for remote control state (for player case)
        var mover = _session.Player.UnitBeingMoved;

        if (mover != _session.Player && mover.IsTypeId(TypeId.Player))
            return;

        var spellInfo = _spellManager.GetSpellInfo(cast.Cast.SpellID, mover.Location.Map.DifficultyID);

        if (spellInfo == null)
        {
            Log.Logger.Error("WORLD: unknown spell id {0}", cast.Cast.SpellID);

            return;
        }

        if (spellInfo.IsPassive)
            return;

        var caster = mover;

        if (caster.IsTypeId(TypeId.Unit) && !caster.AsCreature.HasSpell(spellInfo.Id))
        {
            // If the vehicle creature does not have the spell but it allows the passenger to cast own spells
            // change caster to player and let him cast
            if (!_session.Player.IsOnVehicle(caster) || spellInfo.CheckVehicle(_session.Player) != SpellCastResult.SpellCastOk)
                return;

            caster = _session.Player;
        }

        var triggerFlag = TriggerCastFlags.None;

        // client provided targets
        SpellCastTargets targets = new(caster, cast.Cast);

        // check known spell or raid marker spell (which not requires player to know it)
        if (caster.IsTypeId(TypeId.Player) && !caster.AsPlayer.HasActiveSpell(spellInfo.Id) && !spellInfo.HasEffect(SpellEffectName.ChangeRaidMarker) && !spellInfo.HasAttribute(SpellAttr8.RaidMarker))
        {
            var allow = false;

            // allow casting of unknown spells for special lock cases
            var go = targets.GOTarget;

            if (go != null)
                if (go.GetSpellForLock(caster.AsPlayer) == spellInfo)
                    allow = true;

            // allow casting of spells triggered by clientside periodic trigger auras
            if (caster.HasAuraTypeWithTriggerSpell(AuraType.PeriodicTriggerSpellFromClient, spellInfo.Id))
            {
                allow = true;
                triggerFlag = TriggerCastFlags.FullMask;
            }

            if (!allow)
                return;
        }

        // Check possible spell cast overrides
        spellInfo = caster.GetCastSpellInfo(spellInfo);

        // can't use our own spells when we're in possession of another unit,
        if (_session.Player.IsPossessing)
            return;

        // Client is resending autoshot cast opcode when other spell is cast during shoot rotation
        // Skip it to prevent "interrupt" message
        // Also check targets! target may have changed and we need to interrupt current spell
        if (spellInfo.IsAutoRepeatRangedSpell)
        {
            var autoRepeatSpell = caster.GetCurrentSpell(CurrentSpellTypes.AutoRepeat);

            if (autoRepeatSpell != null)
                if (autoRepeatSpell.SpellInfo == spellInfo && autoRepeatSpell.Targets.UnitTargetGUID == targets.UnitTargetGUID)
                    return;
        }

        // auto-selection buff level base at target level (in spellInfo)
        if (targets.UnitTarget != null)
        {
            var actualSpellInfo = spellInfo.GetAuraRankForLevel(targets.UnitTarget.GetLevelForTarget(caster));

            // if rank not found then function return NULL but in explicit cast case original spell can be casted and later failed with appropriate error message
            if (actualSpellInfo != null)
                spellInfo = actualSpellInfo;
        }

        if (cast.Cast.MoveUpdate != null)
            _movementHandler.HandleMovementOpcode(ClientOpcodes.MoveStop, cast.Cast.MoveUpdate);

        var spell = caster.SpellFactory.NewSpell(spellInfo, triggerFlag);

        SpellPrepare spellPrepare = new()
        {
            ClientCastID = cast.Cast.CastID,
            ServerCastID = spell.CastId
        };

        _session.SendPacket(spellPrepare);

        spell.FromClient = true;
        spell.SpellMisc.Data0 = cast.Cast.Misc[0];
        spell.SpellMisc.Data1 = cast.Cast.Misc[1];
        spell.Prepare(targets);
    }

    [WorldPacketHandler(ClientOpcodes.GameObjReportUse, Processing = PacketProcessing.Inplace)]
    private void HandleGameobjectReportUse(GameObjReportUse packet)
    {
        // ignore for remote control state
        if (_session.Player.UnitBeingMoved != _session.Player)
            return;

        var go = _session.Player.GetGameObjectIfCanInteractWith(packet.Guid);

        if (go == null || go.AI.OnGossipHello(_session.Player))
            return;

        _session.Player.UpdateCriteria(CriteriaType.UseGameobject, go.Entry);
    }

    [WorldPacketHandler(ClientOpcodes.GameObjUse, Processing = PacketProcessing.Inplace)]
    private void HandleGameObjectUse(GameObjUse packet)
    {
        var obj = _session.Player.GetGameObjectIfCanInteractWith(packet.Guid);

        if (obj == null)
            return;

        // ignore for remote control state
        if (_session.Player.UnitBeingMoved != _session.Player)
            if (!(_session.Player.IsOnVehicle(_session.Player.UnitBeingMoved) || _session.Player.IsMounted) && !obj.Template.IsUsableMounted())
                return;

        obj.Use(_session.Player);
    }

    [WorldPacketHandler(ClientOpcodes.KeyboundOverride, Processing = PacketProcessing.ThreadSafe)]
    private void HandleKeyboundOverride(KeyboundOverride keyboundOverride)
    {
        var player = _session.Player;

        if (!player.HasAuraTypeWithMiscvalue(AuraType.KeyboundOverride, keyboundOverride.OverrideID))
            return;

        if (!_spellKeyboundOverrideRecords.TryGetValue(keyboundOverride.OverrideID, out var spellKeyboundOverride))
            return;

        player.SpellFactory.CastSpell(player, spellKeyboundOverride.Data);
    }

    [WorldPacketHandler(ClientOpcodes.GetMirrorImageData)]
    private void HandleMirrorImageDataRequest(GetMirrorImageData packet)
    {
        var guid = packet.UnitGUID;

        // Get unit for which data is needed by client
        var unit = _objectAccessor.GetUnit(_session.Player, guid);

        if (unit == null)
            return;

        if (!unit.HasAuraType(AuraType.CloneCaster))
            return;

        // Get creator of the unit (SPELL_AURA_CLONE_CASTER does not stack)
        var creator = unit.GetAuraEffectsByType(AuraType.CloneCaster).FirstOrDefault()?.Caster;

        if (creator == null)
            return;

        if (creator.TryGetAsPlayer(out var player))
        {
            MirrorImageComponentedData mirrorImageComponentedData = new()
            {
                UnitGUID = guid,
                DisplayID = (int)creator.DisplayId,
                RaceID = (byte)creator.Race,
                Gender = (byte)creator.Gender,
                ClassID = (byte)creator.Class
            };

            foreach (var customization in player.PlayerData.Customizations)
            {
                var chrCustomizationChoice = new ChrCustomizationChoice
                {
                    ChrCustomizationOptionID = customization.ChrCustomizationOptionID,
                    ChrCustomizationChoiceID = customization.ChrCustomizationChoiceID
                };

                mirrorImageComponentedData.Customizations.Add(chrCustomizationChoice);
            }

            var guild = player.Guild;
            mirrorImageComponentedData.GuildGUID = guild?.GetGUID() ?? ObjectGuid.Empty;

            byte[] itemSlots =
            {
                EquipmentSlot.Head, EquipmentSlot.Shoulders, EquipmentSlot.Shirt, EquipmentSlot.Chest, EquipmentSlot.Waist, EquipmentSlot.Legs, EquipmentSlot.Feet, EquipmentSlot.Wrist, EquipmentSlot.Hands, EquipmentSlot.Tabard, EquipmentSlot.Cloak
            };

            // Display items in visible slots
            foreach (var slot in itemSlots)
            {
                var item = player.GetItemByPos(InventorySlots.Bag0, slot);

                var itemDisplayId = item?.GetDisplayId(player) ?? 0;

                mirrorImageComponentedData.ItemDisplayID.Add((int)itemDisplayId);
            }

            _session.SendPacket(mirrorImageComponentedData);
        }
        else
        {
            MirrorImageCreatureData data = new()
            {
                UnitGUID = guid,
                DisplayID = (int)creator.DisplayId
            };

            _session.SendPacket(data);
        }
    }

    [WorldPacketHandler(ClientOpcodes.MissileTrajectoryCollision)]
    private void HandleMissileTrajectoryCollision(MissileTrajectoryCollision packet)
    {
        var caster = _objectAccessor.GetUnit(_session.Player, packet.Target);

        var spell = caster?.FindCurrentSpellBySpellId(packet.SpellID);

        if (spell == null || !spell.Targets.HasDst)
            return;

        Position pos = spell.Targets.DstPos;
        pos.Relocate(packet.CollisionPos);
        spell.Targets.ModDst(pos);

        // we changed dest, recalculate flight time
        spell.RecalculateDelayMomentForDst();

        NotifyMissileTrajectoryCollision data = new()
        {
            Caster = packet.Target,
            CastID = packet.CastID,
            CollisionPos = packet.CollisionPos
        };

        caster.SendMessageToSet(data, true);
    }

    [WorldPacketHandler(ClientOpcodes.OpenItem, Processing = PacketProcessing.Inplace)]
    private void HandleOpenItem(OpenItem packet)
    {
        var player = _session.Player;

        // ignore for remote control state
        if (player.UnitBeingMoved != player)
            return;

        // additional check, client outputs message on its own
        if (!player.IsAlive)
        {
            player.SendEquipError(InventoryResult.PlayerDead);

            return;
        }

        var item = player.GetItemByPos(packet.Slot, packet.PackSlot);

        if (item == null)
        {
            player.SendEquipError(InventoryResult.ItemNotFound);

            return;
        }

        var proto = item.Template;

        if (proto == null)
        {
            player.SendEquipError(InventoryResult.ItemNotFound, item);

            return;
        }

        // Verify that the bag is an actual bag or wrapped item that can be used "normally"
        if (!proto.HasFlag(ItemFlags.HasLoot) && !item.IsWrapped)
        {
            player.SendEquipError(InventoryResult.ClientLockedOut, item);

            Log.Logger.Error("Possible hacking attempt: _session.Player {0} [guid: {1}] tried to open item [guid: {2}, entry: {3}] which is not openable!",
                             player.GetName(),
                             player.GUID.ToString(),
                             item.GUID.ToString(),
                             proto.Id);

            return;
        }

        // locked item
        var lockId = proto.LockID;

        if (lockId != 0)
        {
            if (!_lockRecords.ContainsKey(lockId))
            {
                player.SendEquipError(InventoryResult.ItemLocked, item);
                Log.Logger.Error("WORLD:OpenItem: item [guid = {0}] has an unknown lockId: {1}!", item.GUID.ToString(), lockId);

                return;
            }

            // was not unlocked yet
            if (item.IsLocked)
            {
                player.SendEquipError(InventoryResult.ItemLocked, item);

                return;
            }
        }

        if (item.IsWrapped) // wrapped?
        {
            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_GIFT_BY_ITEM);
            stmt.AddValue(0, item.GUID.Counter);

            var pos = item.Pos;
            var itemGuid = item.GUID;

            _session.QueryProcessor.AddCallback(_characterDatabase.AsyncQuery(stmt)
                                          .WithCallback(result => HandleOpenWrappedItemCallback(pos, itemGuid, result)));
        }
        else
        {
            // If item doesn't already have loot, attempt to load it. If that
            // fails then this is first time opening, generate loot
            if (!item.LootGenerated && !_session.Player.LootItemStorage.LoadStoredLoot(item, player))
            {
                Loot loot = _session.Player.LootFactory.GenerateLoot(player.Location.Map, item.GUID, LootType.Item);
                item.Loot = loot;
                loot.GenerateMoneyLoot(item.Template.MinMoneyLoot, item.Template.MaxMoneyLoot);
                loot.FillLoot(item.Entry, _lootStoreBox.Items, player, true, loot.Gold != 0);

                // Force save the loot and money items that were just rolled
                //  Also saves the container item ID in Loot struct (not to DB)
                if (loot.Gold > 0 || loot.UnlootedCount > 0)
                    _session.Player.LootItemStorage.AddNewStoredLoot(item.GUID.Counter, loot, player);
            }

            if (item.Loot != null)
                player.SendLoot(item.Loot);
            else
                player.SendLootError(ObjectGuid.Empty, item.GUID, LootError.NoLoot);
        }
    }

    private void HandleOpenWrappedItemCallback(ushort pos, ObjectGuid itemGuid, SQLResult result)
    {
        var item = _session.Player?.GetItemByPos(pos);

        if (item == null)
            return;

        if (item.GUID != itemGuid || !item.IsWrapped) // during getting result, gift was swapped with another item
            return;

        if (result.IsEmpty())
        {
            Log.Logger.Error($"Wrapped item {item.GUID} don't have record in character_gifts table and will deleted");
            _session.Player.DestroyItem(item.BagSlot, item.Slot, true);

            return;
        }

        SQLTransaction trans = new();

        var entry = result.Read<uint>(0);
        var flags = result.Read<uint>(1);

        item.SetGiftCreator(ObjectGuid.Empty);
        item.Entry = entry;
        item.ReplaceAllItemFlags((ItemFieldFlags)flags);
        item.SetMaxDurability(item.Template.MaxDurability);
        item.SetState(ItemUpdateState.Changed, _session.Player);

        _session.Player.SaveInventoryAndGoldToDB(trans);

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GIFT);
        stmt.AddValue(0, itemGuid.Counter);
        trans.Append(stmt);

        _characterDatabase.CommitTransaction(trans);
    }

    [WorldPacketHandler(ClientOpcodes.PetCancelAura, Processing = PacketProcessing.Inplace)]
    private void HandlePetCancelAura(PetCancelAura packet)
    {
        if (!_spellManager.HasSpellInfo(packet.SpellID))
        {
            Log.Logger.Error("WORLD: unknown PET spell id {0}", packet.SpellID);

            return;
        }

        var pet = ObjectAccessor.GetCreatureOrPetOrVehicle(_session.Player, packet.PetGUID);

        if (pet == null)
        {
            Log.Logger.Error("HandlePetCancelAura: Attempt to cancel an aura for non-existant {0} by player '{1}'", packet.PetGUID.ToString(), _session.Player.GetName());

            return;
        }

        if (pet != _session.Player.GetGuardianPet() && pet != _session.Player.Charmed)
        {
            Log.Logger.Error("HandlePetCancelAura: {0} is not a pet of player '{1}'", packet.PetGUID.ToString(), _session.Player.GetName());

            return;
        }

        if (!pet.IsAlive)
        {
            pet.SendPetActionFeedback(PetActionFeedback.Dead, 0);

            return;
        }

        pet.RemoveOwnedAura(packet.SpellID, ObjectGuid.Empty, AuraRemoveMode.Cancel);
    }

    [WorldPacketHandler(ClientOpcodes.RequestCategoryCooldowns, Processing = PacketProcessing.Inplace)]
    private void HandleRequestCategoryCooldowns(RequestCategoryCooldowns requestCategoryCooldowns)
    {
        if (requestCategoryCooldowns == null) return;
        _session.Player.SendSpellCategoryCooldowns();
    }

    [WorldPacketHandler(ClientOpcodes.SelfRes)]
    private void HandleSelfRes(SelfRes selfRes)
    {
        List<uint> selfResSpells = _session.Player.ActivePlayerData.SelfResSpells;

        if (!selfResSpells.Contains(selfRes.SpellId))
            return;

        var spellInfo = _spellManager.GetSpellInfo(selfRes.SpellId, _session.Player.Location.Map.DifficultyID);

        if (spellInfo == null)
            return;

        if (_session.Player.HasAuraType(AuraType.PreventResurrection) && !spellInfo.HasAttribute(SpellAttr7.BypassNoResurrectAura))
            return; // silent return, client should display error by itself and not send this opcode

        _session.Player.SpellFactory.CastSpell(_session.Player, selfRes.SpellId, new CastSpellExtraArgs(_session.Player.Location.Map.DifficultyID));
        _session.Player.RemoveSelfResSpell(selfRes.SpellId);
    }

    [WorldPacketHandler(ClientOpcodes.SpellClick, Processing = PacketProcessing.Inplace)]
    private void HandleSpellClick(SpellClick packet)
    {
        // this will get something not in world. crash
        var unit = ObjectAccessor.GetCreatureOrPetOrVehicle(_session.Player, packet.SpellClickUnitGuid);

        if (unit is not { Location.IsInWorld: true })
            return;

        // @todo Unit.SetCharmedBy: 28782 is not in world but 0 is trying to charm it! . crash

        unit.HandleSpellClick(_session.Player);
    }

    [WorldPacketHandler(ClientOpcodes.SetEmpowerMinHoldStagePercent)]
    private void HandleSpellEmpowerMinHoldPct(SpellEmpowerMinHold packet)
    {
        _session.Player.EmpoweredSpellMinHoldPct = packet.HoldPct;
    }

    [WorldPacketHandler(ClientOpcodes.SpellEmpowerRelease)]
    private void HandleSpellEmpowerRelease(SpellEmpowerRelease packet)
    {
        _session.Player.UpdateEmpowerState(EmpowerState.Canceled, packet.SpellID);
    }

    [WorldPacketHandler(ClientOpcodes.SpellEmpowerRestart)]
    private void HandleSpellEmpowerRelestart(SpellEmpowerRelease packet)
    {
        _session.Player.UpdateEmpowerState(EmpowerState.Empowering, packet.SpellID);
    }

    [WorldPacketHandler(ClientOpcodes.TotemDestroyed, Processing = PacketProcessing.Inplace)]
    private void HandleTotemDestroyed(TotemDestroyed totemDestroyed)
    {
        // ignore for remote control state
        if (_session.Player.UnitBeingMoved != _session.Player)
            return;

        var slotId = totemDestroyed.Slot;
        slotId += (int)SummonSlot.Totem;

        if (slotId >= SharedConst.MaxTotemSlot)
            return;

        if (_session.Player.SummonSlot[slotId].IsEmpty)
            return;

        var totem = ObjectAccessor.GetCreature(_session.Player, _session.Player.SummonSlot[slotId]);

        if (totem is { IsTotem: true }) // && totem.GetGUID() == packet.TotemGUID)  Unknown why blizz doesnt send the guid when you right click it.
            totem.ToTotem().UnSummon();
    }

    [WorldPacketHandler(ClientOpcodes.UpdateMissileTrajectory)]
    private void HandleUpdateMissileTrajectory(UpdateMissileTrajectory packet)
    {
        var caster = _objectAccessor.GetUnit(_session.Player, packet.Guid);
        var spell = caster?.GetCurrentSpell(CurrentSpellTypes.Generic);

        if (spell == null || spell.SpellInfo.Id != packet.SpellID || spell.CastId != packet.CastID || !spell.Targets.HasDst || !spell.Targets.HasSrc)
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
            _session.Player.ValidateMovementInfo(packet.Status);
        /*public uint opcode;
            recvPacket >> opcode;
            recvPacket.SetOpcode(CMSG_MOVE_STOP); // always set to CMSG_MOVE_STOP in client SetOpcode
            //HandleMovementOpcodes(recvPacket);*/
    }

    [WorldPacketHandler(ClientOpcodes.UseItem, Processing = PacketProcessing.Inplace)]
    private void HandleUseItem(UseItem packet)
    {
        var user = _session.Player;

        // ignore for remote control state
        if (user.UnitBeingMoved != user)
            return;

        var item = user.GetUseableItemByPos(packet.PackSlot, packet.Slot);

        if (item == null)
        {
            user.SendEquipError(InventoryResult.ItemNotFound);

            return;
        }

        if (item.GUID != packet.CastItem)
        {
            user.SendEquipError(InventoryResult.ItemNotFound);

            return;
        }

        var proto = item.Template;

        if (proto == null)
        {
            user.SendEquipError(InventoryResult.ItemNotFound, item);

            return;
        }

        // some item classes can be used only in equipped state
        if (proto.InventoryType != InventoryType.NonEquip && !item.IsEquipped)
        {
            user.SendEquipError(InventoryResult.ItemNotFound, item);

            return;
        }

        var msg = user.CanUseItem(item);

        if (msg != InventoryResult.Ok)
        {
            user.SendEquipError(msg, item);

            return;
        }

        // only allow conjured consumable, bandage, poisons (all should have the 2^21 item Id set in DB)
        if (proto.Class == ItemClass.Consumable && !proto.HasFlag(ItemFlags.IgnoreDefaultArenaRestrictions) && user.InArena)
        {
            user.SendEquipError(InventoryResult.NotDuringArenaMatch, item);

            return;
        }

        // don't allow items banned in arena
        if (proto.HasFlag(ItemFlags.NotUseableInArena) && user.InArena)
        {
            user.SendEquipError(InventoryResult.NotDuringArenaMatch, item);

            return;
        }

        if (user.IsInCombat)
            foreach (var effect in item.Effects)
            {
                var spellInfo = _spellManager.GetSpellInfo((uint)effect.SpellID, user.Location.Map.DifficultyID);

                if (spellInfo == null)
                    continue;

                if (spellInfo.CanBeUsedInCombat)
                    continue;

                user.SendEquipError(InventoryResult.NotInCombat, item);

                return;
            }

        // check also  BIND_WHEN_PICKED_UP and BIND_QUEST_ITEM for .additem or .additemset case by GM (not binded at adding to inventory)
        if (item.Bonding == ItemBondingType.OnUse || item.Bonding == ItemBondingType.OnAcquire || item.Bonding == ItemBondingType.Quest)
            if (!item.IsSoulBound)
            {
                item.SetState(ItemUpdateState.Changed, user);
                item.SetBinding(true);
                _collectionMgr.AddItemAppearance(item);
            }

        user.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.ItemUse);

        SpellCastTargets targets = new(user, packet.Cast);

        // Note: If script stop casting it must send appropriate data to client to prevent stuck item in gray state.
        // no script or script not process request by self
        if (!_scriptManager.RunScriptRet<IItemOnUse>(p => p.OnUse(user, item, targets, packet.Cast.CastID), item.ScriptId))
            user.CastItemUseSpell(item, targets, packet.Cast.CastID, packet.Cast.Misc);
    }
}
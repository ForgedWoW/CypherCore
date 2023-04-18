// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Autofac;
using Forged.MapServer.Accounts;
using Forged.MapServer.Achievements;
using Forged.MapServer.Arenas;
using Forged.MapServer.BattleGrounds;
using Forged.MapServer.BattlePets;
using Forged.MapServer.Chat;
using Forged.MapServer.Chat.Channels;
using Forged.MapServer.Chrono;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.DataStorage.Structs.C;
using Forged.MapServer.DataStorage.Structs.F;
using Forged.MapServer.DungeonFinding;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Events;
using Forged.MapServer.Garrisons;
using Forged.MapServer.Globals;
using Forged.MapServer.Groups;
using Forged.MapServer.Guilds;
using Forged.MapServer.LootManagement;
using Forged.MapServer.Mails;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Maps.Workers;
using Forged.MapServer.Miscellaneous;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Character;
using Forged.MapServer.Networking.Packets.Chat;
using Forged.MapServer.Networking.Packets.Combat;
using Forged.MapServer.Networking.Packets.CombatLog;
using Forged.MapServer.Networking.Packets.Item;
using Forged.MapServer.Networking.Packets.Mail;
using Forged.MapServer.Networking.Packets.Misc;
using Forged.MapServer.Networking.Packets.Movement;
using Forged.MapServer.Networking.Packets.NPC;
using Forged.MapServer.Networking.Packets.Party;
using Forged.MapServer.Networking.Packets.Pet;
using Forged.MapServer.Networking.Packets.Quest;
using Forged.MapServer.Networking.Packets.Spell;
using Forged.MapServer.Networking.Packets.Talent;
using Forged.MapServer.Networking.Packets.Toy;
using Forged.MapServer.Networking.Packets.Vehicle;
using Forged.MapServer.Networking.Packets.WorldState;
using Forged.MapServer.OutdoorPVP;
using Forged.MapServer.Quest;
using Forged.MapServer.Reputation;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Forged.MapServer.Server;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Skills;
using Forged.MapServer.Text;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Database;
using Framework.Dynamic;
using Framework.Util;
using Game.Common;
using Microsoft.Extensions.Configuration;
using Serilog;
using PlayerChoiceResponse = Forged.MapServer.Networking.Packets.Quest.PlayerChoiceResponse;
using PlayerChoiceResponseMawPower = Forged.MapServer.Networking.Packets.Quest.PlayerChoiceResponseMawPower;
using PlayerChoiceResponseReward = Forged.MapServer.Networking.Packets.Quest.PlayerChoiceResponseReward;
using PlayerChoiceResponseRewardEntry = Forged.MapServer.Networking.Packets.Quest.PlayerChoiceResponseRewardEntry;

namespace Forged.MapServer.Entities.Players;

public partial class Player : Unit
{
    public Player(WorldSession session, ClassFactory classFactory) : base(true, classFactory)
    {
        ObjectTypeMask |= TypeMask.Player;
        ObjectTypeId = TypeId.Player;

        PlayerData = new PlayerData();
        ActivePlayerData = new ActivePlayerData();
        GuildMgr = classFactory.Resolve<GuildManager>();
        CharacterDatabase = classFactory.Resolve<CharacterDatabase>();
        PlayerComputators = classFactory.Resolve<PlayerComputators>();
        WorldMgr = classFactory.Resolve<WorldManager>();
        ChannelManagerFactory = classFactory.Resolve<ChannelManagerFactory>();
        WorldStateManager = classFactory.Resolve<WorldStateManager>();
        GroupManager = classFactory.Resolve<GroupManager>();
        MapManager = classFactory.Resolve<MapManager>();
        CharacterTemplateDataStorage = classFactory.Resolve<CharacterTemplateDataStorage>();
        AccountManager = classFactory.Resolve<AccountManager>();
        CollectionMgr = classFactory.Resolve<CollectionMgr>();
        BattlegroundManager = classFactory.Resolve<BattlegroundManager>();
        OutdoorPvPManager = classFactory.Resolve<OutdoorPvPManager>();
        LoginDatabase = classFactory.Resolve<LoginDatabase>();
        ArenaTeamManager = classFactory.Resolve<ArenaTeamManager>();
        LanguageManager = classFactory.Resolve<LanguageManager>();
        InstanceLockManager = classFactory.Resolve<InstanceLockManager>();
        SocialManager = classFactory.Resolve<SocialManager>();
        RealmManager = classFactory.Resolve<RealmManager>();
        TerrainManager = classFactory.Resolve<TerrainManager>();
        GameEventManager = classFactory.Resolve<GameEventManager>();
        TraitMgr = classFactory.Resolve<TraitMgr>();
        LootItemStorage = classFactory.Resolve<LootItemStorage>();
        LFGManager = classFactory.Resolve<LFGManager>();
        ItemEnchantmentManager = classFactory.Resolve<ItemEnchantmentManager>();
        LootFactory = classFactory.Resolve<LootFactory>();
        SkillDiscovery = classFactory.Resolve<SkillDiscovery>();
        Formulas = classFactory.Resolve<Formulas>();
        Taxi = classFactory.Resolve<PlayerTaxi>();
        Session = session;

        // players always accept
        if (!Session.HasPermission(RBACPermissions.CanFilterWhispers))
            _extraFlags |= PlayerExtraFlags.AcceptWhispers;

        _zoneUpdateId = 0xffffffff;
        SaveTimer = classFactory.Resolve<IConfiguration>().GetDefaultValue("PlayerSaveInterval", 15u * Time.MINUTE * Time.IN_MILLISECONDS);
        _customizationsChanged = false;

        GroupInvite = null;

        LoginFlags = AtLoginFlags.None;
        PlayerTalkClass = classFactory.Resolve<PlayerMenu>(new PositionalParameter(0, session));
        _currentBuybackSlot = InventorySlots.BuyBackStart;

        for (byte i = 0; i < (int)MirrorTimerType.Max; i++)
            _mirrorTimer[i] = -1;

        _logintime = GameTime.CurrentTime;
        _lastTick = _logintime;

        DungeonDifficultyId = Difficulty.Normal;
        RaidDifficultyId = Difficulty.NormalRaid;
        LegacyRaidDifficultyId = Difficulty.Raid10N;
        InstanceValid = true;

        _specializationInfo = new SpecializationInfo();

        for (byte i = 0; i < (byte)BaseModGroup.End; ++i)
        {
            _auraBaseFlatMod[i] = 0.0f;
            _auraBasePctMod[i] = 1.0f;
        }

        for (var i = 0; i < (int)SpellModOp.Max; ++i)
        {
            _spellModifiers[i] = new List<SpellModifier>[(int)SpellModType.End];

            for (var c = 0; c < (int)SpellModType.End; ++c)
                _spellModifiers[i][c] = new List<SpellModifier>();
        }

        // Honor System
        _lastHonorUpdateTime = GameTime.CurrentTime;

        UnitMovedByMe = this;
        PlayerMovingMe = this;
        SeerView = this;

        IsActive = true;
        ControlledByPlayer = true;

        classFactory.Resolve<WorldManager>().IncreasePlayerCount();

        CinematicMgr = classFactory.Resolve<CinematicManager>(new PositionalParameter(0, this));

        _achievementSys = classFactory.Resolve<PlayerAchievementMgr>(new PositionalParameter(0, this));
        ReputationMgr = classFactory.Resolve<ReputationMgr>(new PositionalParameter(0, this));
        _questObjectiveCriteriaManager = classFactory.Resolve<QuestObjectiveCriteriaManager>(new PositionalParameter(0, this));
        SceneMgr = classFactory.Resolve<SceneMgr>(new PositionalParameter(0, this));

        _battlegroundQueueIdRecs[0] = new BgBattlegroundQueueIdRec();
        _battlegroundQueueIdRecs[1] = new BgBattlegroundQueueIdRec();

        _bgData = new BgData();

        RestMgr = new RestMgr(this);

        _groupUpdateTimer = new TimeTracker(5000);

        ApplyCustomConfigs();

        ObjectScale = 1;
    }

    //Team
    public static TeamFaction TeamForRace(Race race, CliDB cliDB)
    {
        return TeamIdForRace(race, cliDB) switch
        {
            0 => TeamFaction.Alliance,
            1 => TeamFaction.Horde,
            _ => TeamFaction.Alliance
        };
    }

    public static uint TeamIdForRace(Race race, CliDB cliDB)
    {
        if (cliDB.ChrRacesStorage.TryGetValue((byte)race, out var rEntry))
            return (uint)rEntry.Alliance;

        Log.Logger.Error("Race ({0}) not found in DBC: wrong DBC files?", race);

        return TeamIds.Neutral;
    }

    public bool ActivateTaxiPathTo(List<uint> nodes, Creature npc = null, uint spellid = 0, uint preferredMountDisplay = 0)
    {
        if (nodes.Count < 2)
        {
            Session.SendActivateTaxiReply(ActivateTaxiReply.NoSuchPath);

            return false;
        }

        // not let cheating with start flight in time of logout process || while in combat || has type state: stunned || has type state: root
        if (Session.IsLogingOut || IsInCombat || HasUnitState(UnitState.Stunned) || HasUnitState(UnitState.Root))
        {
            Session.SendActivateTaxiReply(ActivateTaxiReply.PlayerBusy);

            return false;
        }

        if (HasUnitFlag(UnitFlags.RemoveClientControl))
            return false;

        // taximaster case
        if (npc != null)
        {
            // not let cheating with start flight mounted
            RemoveAurasByType(AuraType.Mounted);

            if (DisplayId != NativeDisplayId)
                RestoreDisplayId(true);

            if (IsDisallowedMountForm(TransformSpell, ShapeShiftForm.None, DisplayId))
            {
                Session.SendActivateTaxiReply(ActivateTaxiReply.PlayerShapeshifted);

                return false;
            }

            // not let cheating with start flight in time of logout process || if casting not finished || while in combat || if not use Spell's with EffectSendTaxi
            if (IsNonMeleeSpellCast(false))
            {
                Session.SendActivateTaxiReply(ActivateTaxiReply.PlayerBusy);

                return false;
            }
        }
        // cast case or scripted call case
        else
        {
            RemoveAurasByType(AuraType.Mounted);

            if (DisplayId != NativeDisplayId)
                RestoreDisplayId(true);

            var spell = GetCurrentSpell(CurrentSpellTypes.Generic);

            if (spell != null)
                if (spell.SpellInfo.Id != spellid)
                    InterruptSpell(CurrentSpellTypes.Generic, false);

            InterruptSpell(CurrentSpellTypes.AutoRepeat, false);

            spell = GetCurrentSpell(CurrentSpellTypes.Channeled);

            if (spell != null)
                if (spell.SpellInfo.Id != spellid)
                    InterruptSpell(CurrentSpellTypes.Channeled);
        }

        var sourcenode = nodes[0];

        // starting node too far away (cheat?)
        if (!CliDB.TaxiNodesStorage.TryGetValue(sourcenode, out var node))
        {
            Session.SendActivateTaxiReply(ActivateTaxiReply.NoSuchPath);

            return false;
        }

        // Prepare to flight start now

        // stop combat at start taxi flight if any
        CombatStop();

        StopCastingCharm();
        StopCastingBindSight();
        ExitVehicle();

        // stop trade (client cancel trade at taxi map open but cheating tools can be used for reopen it)
        TradeCancel(true);

        // clean not finished taxi path if any
        Taxi.ClearTaxiDestinations();

        // 0 element current node
        Taxi.AddTaxiDestination(sourcenode);

        // fill destinations path tail
        uint sourcepath = 0;
        uint totalcost = 0;
        uint firstcost = 0;

        var prevnode = sourcenode;

        for (var i = 1; i < nodes.Count; ++i)
        {
            var lastnode = nodes[i];
            GameObjectManager.GetTaxiPath(prevnode, lastnode, out var path, out var cost);

            if (path == 0)
            {
                Taxi.ClearTaxiDestinations();

                return false;
            }

            totalcost += cost;

            if (i == 1)
                firstcost = cost;

            if (prevnode == sourcenode)
                sourcepath = path;

            Taxi.AddTaxiDestination(lastnode);

            prevnode = lastnode;
        }

        // get mount model (in case non taximaster (npc == NULL) allow more wide lookup)
        //
        // Hack-Fix for Alliance not being able to use Acherus taxi. There is
        // only one mount ID for both sides. Probably not good to use 315 in case DBC nodes
        // change but I couldn't find a suitable alternative. OK to use class because only DK
        // can use this taxi.
        uint mountDisplayID;

        if (node.Flags.HasAnyFlag(TaxiNodeFlags.UseFavoriteMount) && preferredMountDisplay != 0)
            mountDisplayID = preferredMountDisplay;
        else
            mountDisplayID = GameObjectManager.GetTaxiMountDisplayId(sourcenode, Team, npc == null || (sourcenode == 315 && Class == PlayerClass.Deathknight));

        // in spell case allow 0 model
        if ((mountDisplayID == 0 && spellid == 0) || sourcepath == 0)
        {
            Session.SendActivateTaxiReply(ActivateTaxiReply.UnspecifiedServerError);
            Taxi.ClearTaxiDestinations();

            return false;
        }

        var money = Money;

        if (npc != null)
        {
            var discount = GetReputationPriceDiscount(npc);
            totalcost = (uint)Math.Ceiling(totalcost * discount);
            firstcost = (uint)Math.Ceiling(firstcost * discount);
            Taxi.SetFlightMasterFactionTemplateId(npc.Faction);
        }
        else
            Taxi.SetFlightMasterFactionTemplateId(0);

        if (money < totalcost)
        {
            Session.SendActivateTaxiReply(ActivateTaxiReply.NotEnoughMoney);
            Taxi.ClearTaxiDestinations();

            return false;
        }

        //Checks and preparations done, DO FLIGHT
        UpdateCriteria(CriteriaType.BuyTaxi, 1);

        if (Configuration.GetDefaultValue("InstantFlightPaths", false))
        {
            var lastPathNode = CliDB.TaxiNodesStorage.LookupByKey(nodes[^1]);
            Taxi.ClearTaxiDestinations();
            ModifyMoney(-totalcost);
            UpdateCriteria(CriteriaType.MoneySpentOnTaxis, totalcost);
            TeleportTo(lastPathNode.ContinentID, lastPathNode.Pos.X, lastPathNode.Pos.Y, lastPathNode.Pos.Z, Location.Orientation);

            return false;
        }

        ModifyMoney(-firstcost);
        UpdateCriteria(CriteriaType.MoneySpentOnTaxis, firstcost);
        Session.SendActivateTaxiReply();
        Session.SendDoFlight(mountDisplayID, sourcepath);

        return true;
    }

    public bool ActivateTaxiPathTo(uint taxiPathID, uint spellid = 0)
    {
        if (!CliDB.TaxiPathStorage.TryGetValue(taxiPathID, out var entry))
            return false;

        List<uint> nodes = new()
        {
            entry.FromTaxiNode,
            entry.ToTaxiNode
        };

        return ActivateTaxiPathTo(nodes, null, spellid);
    }

    public ActionButton AddActionButton(byte button, ulong action, uint type)
    {
        if (!IsActionButtonDataValid(button, action, type))
            return null;

        // it create new button (NEW state) if need or return existed
        if (!_actionButtons.ContainsKey(button))
            _actionButtons[button] = new ActionButton();

        var ab = _actionButtons[button];

        // set data and update to CHANGED if not NEW
        ab.SetActionAndType(action, (ActionButtonType)type);

        Log.Logger.Debug($"Player::AddActionButton: Player '{GetName()}' ({GUID}) added action '{action}' (type {type}) to button '{button}'");

        return ab;
    }

    public void AddAuraVision(PlayerFieldByte2Flags flags)
    {
        SetUpdateFieldFlagValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.AuraVision), (byte)flags);
    }

    public void AddConditionalTransmog(uint itemModifiedAppearanceId)
    {
        AddDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ConditionalTransmog), itemModifiedAppearanceId);
    }

    public void AddCurrency(uint id, uint amount, CurrencyGainSource gainSource = CurrencyGainSource.Cheat)
    {
        ModifyCurrency(id, (int)amount, gainSource);
    }

    //Helpers
    public void AddGossipItem(GossipOptionNpc optionNpc, string text, uint sender, uint action)
    {
        PlayerTalkClass.GossipMenu.AddMenuItem(0, -1, optionNpc, text, 0, GossipOptionFlags.None, null, 0, 0, false, 0, "", null, null, sender, action);
    }

    public void AddGossipItem(GossipOptionNpc optionNpc, string text, uint sender, uint action, string popupText, uint popupMoney, bool coded)
    {
        PlayerTalkClass.GossipMenu.AddMenuItem(0, -1, optionNpc, text, 0, GossipOptionFlags.None, null, 0, 0, coded, popupMoney, popupText, null, null, sender, action);
    }

    public void AddGossipItem(uint gossipMenuID, uint gossipMenuItemID, uint sender, uint action)
    {
        PlayerTalkClass.GossipMenu.AddMenuItem(gossipMenuID, gossipMenuItemID, sender, action);
    }

    public void AddHeirloom(uint itemId, uint flags)
    {
        AddDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Heirlooms), itemId);
        AddDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.HeirloomFlags), flags);
    }

    public void AddIllusionBlock(uint blockValue)
    {
        AddDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.TransmogIllusions), blockValue);
    }

    public void AddIllusionFlag(int slot, uint flag)
    {
        SetUpdateFieldFlagValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.TransmogIllusions, slot), flag);
    }

    //Mail
    public void AddMail(Mail mail)
    {
        Mails.Insert(0, mail);
    }

    public void AddMItem(Item it)
    {
        _mailItems[it.GUID.Counter] = it;
    }

    public void AddNewMailDeliverTime(long deliverTime)
    {
        if (deliverTime <= GameTime.CurrentTime) // ready now
        {
            ++UnReadMails;
            SendNewMail();
        }
        else // not ready and no have ready mails
        {
            if (_nextMailDelivereTime == 0 || _nextMailDelivereTime > deliverTime)
                _nextMailDelivereTime = deliverTime;
        }
    }

    public void AddPetAura(PetAura petSpell)
    {
        PetAuras.Add(petSpell);

        var pet = CurrentPet;

        pet?.CastPetAura(petSpell);
    }

    public void AddSelfResSpell(uint spellId)
    {
        AddDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.SelfResSpells), spellId);
    }

    public override void AddToWorld()
    {
        // Do not add/remove the player from the object storage
        // It will crash when updating the ObjectAccessor
        // The player should only be added when logging in
        base.AddToWorld();

        for (byte i = (int)PlayerSlots.Start; i < (int)PlayerSlots.End; ++i)
            if (_items[i] != null)
                _items[i].AddToWorld();
    }

    public void AddToy(uint itemId, uint flags)
    {
        AddDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Toys), itemId);
        AddDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ToyFlags), flags);
    }

    public void AddTransmogBlock(uint blockValue)
    {
        AddDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Transmog), blockValue);
    }

    public void AddTransmogFlag(int slot, uint flag)
    {
        SetUpdateFieldFlagValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Transmog, slot), flag);
    }

    public void AddWhisperWhiteList(ObjectGuid guid)
    {
        _whisperList.Add(guid);
    }

    public void ApplyBaseModPctValue(BaseModGroup modGroup, double pct)
    {
        if (modGroup >= BaseModGroup.End)
        {
            Log.Logger.Error($"Player.ApplyBaseModPctValue: Invalid BaseModGroup/BaseModType ({modGroup}/{BaseModType.FlatMod}) for player '{GetName()}' ({GUID})");

            return;
        }

        MathFunctions.AddPct(ref _auraBasePctMod[(int)modGroup], pct);
        UpdateBaseModGroup(modGroup);
    }

    public void ApplyModFakeInebriation(int mod, bool apply)
    {
        ApplyModUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.FakeInebriation), mod, apply);
    }

    public void ApplyModOverrideApBySpellPowerPercent(float mod, bool apply)
    {
        ApplyModUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.OverrideAPBySpellPowerPercent), mod, apply);
    }

    public void ApplyModOverrideSpellPowerByApPercent(float mod, bool apply)
    {
        ApplyModUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.OverrideSpellPowerByAPPercent), mod, apply);
    }

    public void AutoUnequipOffhandIfNeed(bool force = false)
    {
        var offItem = GetItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);

        if (offItem == null)
            return;

        var offtemplate = offItem.Template;

        // unequip offhand weapon if player doesn't have dual wield anymore
        if (!CanDualWield && ((offItem.Template.InventoryType == InventoryType.WeaponOffhand && !offItem.Template.HasFlag(ItemFlags3.AlwaysAllowDualWield)) || offItem.Template.InventoryType == InventoryType.Weapon))
            force = true;

        // need unequip offhand for 2h-weapon without TitanGrip (in any from hands)
        if (!force && (CanTitanGrip() || (offtemplate.InventoryType != InventoryType.Weapon2Hand && !IsTwoHandUsed())))
            return;

        List<ItemPosCount> offDest = new();
        var offMsg = CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, offDest, offItem);

        if (offMsg == InventoryResult.Ok)
        {
            RemoveItem(InventorySlots.Bag0, EquipmentSlot.OffHand, true);
            StoreItem(offDest, offItem, true);
        }
        else
        {
            MoveItemFromInventory(InventorySlots.Bag0, EquipmentSlot.OffHand, true);
            SQLTransaction trans = new();
            offItem.DeleteFromInventoryDB(trans); // deletes item from character's inventory
            offItem.SaveToDB(trans);              // recursive and not have transaction guard into self, item not in inventory and can be save standalone

            var subject = GameObjectManager.GetCypherString(CypherStrings.NotEquippedItem);
            ClassFactory.ResolvePositional<MailDraft>(subject, "There were problems with equipping one or several items").AddItem(offItem).SendMailTo(trans, this, new MailSender(this, MailStationery.Gm), MailCheckMask.Copied);

            CharacterDatabase.CommitTransaction(trans);
        }
    }

    public override void BuildCreateUpdateBlockForPlayer(UpdateData data, Player target)
    {
        if (target == this)
        {
            for (var i = EquipmentSlot.Start; i < InventorySlots.BankBagEnd; ++i)
            {
                if (_items[i] == null)
                    continue;

                _items[i].BuildCreateUpdateBlockForPlayer(data, target);
            }

            for (var i = InventorySlots.ReagentStart; i < InventorySlots.ChildEquipmentEnd; ++i)
            {
                if (_items[i] == null)
                    continue;

                _items[i].BuildCreateUpdateBlockForPlayer(data, target);
            }
        }

        base.BuildCreateUpdateBlockForPlayer(data, target);
    }

    public void BuildPlayerRepop()
    {
        PreRessurect packet = new()
        {
            PlayerGUID = GUID
        };

        SendPacket(packet);

        // If the player has the Wisp racial then cast the Wisp aura on them
        if (HasSpell(20585))
            SpellFactory.CastSpell(this, 20584, true);

        SpellFactory.CastSpell(this, 8326, true);

        RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Release);

        // there must be SMSG.FORCE_RUN_SPEED_CHANGE, SMSG.FORCE_SWIM_SPEED_CHANGE, SMSG.MOVE_WATER_WALK
        // there must be SMSG.STOP_MIRROR_TIMER

        // the player cannot have a corpse already on current map, only bones which are not returned by GetCorpse
        var corpseLocation = CorpseLocation;

        if (corpseLocation.MapId == Location.MapId)
        {
            Log.Logger.Error("BuildPlayerRepop: player {0} ({1}) already has a corpse", GetName(), GUID.ToString());

            return;
        }

        // create a corpse and place it at the player's location
        var corpse = CreateCorpse();

        if (corpse == null)
        {
            Log.Logger.Error("Error creating corpse for Player {0} ({1})", GetName(), GUID.ToString());

            return;
        }

        Location.Map.AddToMap(corpse);

        // convert player body to ghost
        SetDeathState(DeathState.Dead);
        SetHealth(1);

        SetWaterWalking(true);

        if (!Session.IsLogingOut && !HasUnitState(UnitState.Stunned))
            SetRooted(false);

        // BG - remove insignia related
        RemoveUnitFlag(UnitFlags.Skinnable);

        var corpseReclaimDelay = CalculateCorpseReclaimDelay();

        if (corpseReclaimDelay >= 0)
            SendCorpseReclaimDelay(corpseReclaimDelay);

        // to prevent cheating
        corpse.ResetGhostTime();

        StopMirrorTimers(); //disable timers(bars)

        // OnPlayerRepop hook
        ScriptManager.ForEach<IPlayerOnPlayerRepop>(p => p.OnPlayerRepop(this));
    }

    public override void BuildValuesCreate(WorldPacket data, Player target)
    {
        var flags = GetUpdateFieldFlagsFor(target);
        WorldPacket buffer = new();

        buffer.WriteUInt8((byte)flags);
        ObjectData.WriteCreate(buffer, flags, this, target);
        UnitData.WriteCreate(buffer, flags, this, target);
        PlayerData.WriteCreate(buffer, flags, this, target);

        if (target == this)
            ActivePlayerData.WriteCreate(buffer, flags, this, target);

        data.WriteUInt32(buffer.GetSize());
        data.WriteBytes(buffer);
    }

    public override void BuildValuesUpdate(WorldPacket data, Player target)
    {
        var flags = GetUpdateFieldFlagsFor(target);
        WorldPacket buffer = new();

        buffer.WriteUInt32((uint)(Values.GetChangedObjectTypeMask() & ~((target != this ? 1 : 0) << (int)TypeId.ActivePlayer)));

        if (Values.HasChanged(TypeId.Object))
            ObjectData.WriteUpdate(buffer, flags, this, target);

        if (Values.HasChanged(TypeId.Unit))
            UnitData.WriteUpdate(buffer, flags, this, target);

        if (Values.HasChanged(TypeId.Player))
            PlayerData.WriteUpdate(buffer, flags, this, target);

        if (target == this && Values.HasChanged(TypeId.ActivePlayer))
            ActivePlayerData.WriteUpdate(buffer, flags, this, target);

        data.WriteUInt32(buffer.GetSize());
        data.WriteBytes(buffer);
    }

    public override void BuildValuesUpdateWithFlag(WorldPacket data, UpdateFieldFlag flags, Player target)
    {
        UpdateMask valuesMask = new((int)TypeId.Max);
        valuesMask.Set((int)TypeId.Unit);
        valuesMask.Set((int)TypeId.Player);

        WorldPacket buffer = new();

        UpdateMask mask = new(191);
        UnitData.AppendAllowedFieldsMaskForFlag(mask, flags);
        UnitData.WriteUpdate(buffer, mask, true, this, target);

        UpdateMask mask2 = new(161);
        PlayerData.AppendAllowedFieldsMaskForFlag(mask2, flags);
        PlayerData.WriteUpdate(buffer, mask2, true, this, target);

        data.WriteUInt32(buffer.GetSize());
        data.WriteUInt32(valuesMask.GetBlock(0));
        data.WriteBytes(buffer);
    }

    //Repitation
    public int CalculateReputationGain(ReputationSource source, uint creatureOrQuestLevel, int rep, int faction, bool noQuestBonus = false)
    {
        var noBonuses = false;

        if (CliDB.FactionStorage.TryGetValue((uint)faction, out var factionEntry))
            if (CliDB.FriendshipReputationStorage.TryGetValue(factionEntry.FriendshipRepID, out var friendshipReputation))
                if (friendshipReputation.Flags.HasAnyFlag(FriendshipReputationFlags.NoRepGainModifiers))
                    noBonuses = true;

        double percent = 100.0f;

        if (!noBonuses)
        {
            var repMod = noQuestBonus ? 0.0f : GetTotalAuraModifier(AuraType.ModReputationGain);

            // faction specific auras only seem to apply to kills
            if (source == ReputationSource.Kill)
                repMod += GetTotalAuraModifierByMiscValue(AuraType.ModFactionReputationGain, faction);

            percent += rep > 0 ? repMod : -repMod;
        }

        var rate = source switch
        {
            ReputationSource.Kill            => Configuration.GetDefaultValue("Rate:Reputation:LowLevel:Kill", 1.0f),
            ReputationSource.Quest           => Configuration.GetDefaultValue("Rate:Reputation:LowLevel:QuestId", 1.0f),
            ReputationSource.DailyQuest      => Configuration.GetDefaultValue("Rate:Reputation:LowLevel:QuestId", 1.0f),
            ReputationSource.WeeklyQuest     => Configuration.GetDefaultValue("Rate:Reputation:LowLevel:QuestId", 1.0f),
            ReputationSource.MonthlyQuest    => Configuration.GetDefaultValue("Rate:Reputation:LowLevel:QuestId", 1.0f),
            ReputationSource.RepeatableQuest => Configuration.GetDefaultValue("Rate:Reputation:LowLevel:QuestId", 1.0f),
            ReputationSource.Spell           => 1.0f,
            _                                => 1.0f
        };

        if (rate != 1.0f && creatureOrQuestLevel < Formulas.GetGrayLevel(Level))
            percent *= rate;

        if (percent <= 0.0f)
            return 0;

        // Multiply result with the faction specific rate
        var repData = GameObjectManager.GetRepRewardRate((uint)faction);

        if (repData != null)
        {
            var repRate = source switch
            {
                ReputationSource.Kill            => repData.CreatureRate,
                ReputationSource.Quest           => repData.QuestRate,
                ReputationSource.DailyQuest      => repData.QuestDailyRate,
                ReputationSource.WeeklyQuest     => repData.QuestWeeklyRate,
                ReputationSource.MonthlyQuest    => repData.QuestMonthlyRate,
                ReputationSource.RepeatableQuest => repData.QuestRepeatableRate,
                ReputationSource.Spell           => repData.SpellRate,
                _                                => 0.0f
            };

            // for custom, a rate of 0.0 will totally disable reputation gain for this faction/type
            if (repRate <= 0.0f)
                return 0;

            percent *= repRate;
        }

        if (source != ReputationSource.Spell && GetsRecruitAFriendBonus(false))
            percent *= 1.0f + Configuration.GetDefaultValue("Rate:Reputation:RecruitAFriendBonus", 0.1f);

        return MathFunctions.CalculatePct(rep, percent);
    }

    public override bool CanAlwaysSee(WorldObject obj)
    {
        // Always can see self
        if (UnitBeingMoved == obj)
            return true;

        ObjectGuid guid = ActivePlayerData.FarsightObject;

        if (!guid.IsEmpty)
            if (obj.GUID == guid)
                return true;

        return false;
    }

    public bool CanEnableWarModeInArea()
    {
        var zone = CliDB.AreaTableStorage.LookupByKey(Location.Zone);

        if (zone == null || !IsFriendlyArea(zone))
            return false;

        if (!CliDB.AreaTableStorage.TryGetValue(Location.Area, out var area))
            area = zone;

        do
        {
            if ((area.Flags[1] & (uint)AreaFlags2.CanEnableWarMode) != 0)
                return true;

            area = CliDB.AreaTableStorage.LookupByKey(area.ParentAreaID);
        } while (area != null);

        return false;
    }

    public bool CanJoinConstantChannelInZone(ChatChannelsRecord channel, AreaTableRecord zone)
    {
        if (channel.Flags.HasAnyFlag(ChannelDBCFlags.ZoneDep) && zone.HasFlag(AreaFlags.ArenaInstance))
            return false;

        if (channel.Flags.HasAnyFlag(ChannelDBCFlags.CityOnly) && !zone.HasFlag(AreaFlags.Capital))
            return false;

        if (channel.Flags.HasAnyFlag(ChannelDBCFlags.GuildReq) && GuildId != 0)
            return false;

        if (channel.Flags.HasAnyFlag(ChannelDBCFlags.NoClientJoin))
            return false;

        return true;
    }

    public override bool CanNeverSee(WorldObject obj)
    {
        // the intent is to delay sending visible objects until client is ready for them
        // some gameobjects dont function correctly if they are sent before TransportServerTime is correctly set (after CMSG_MOVE_INIT_ACTIVE_MOVER_COMPLETE)
        return !HasPlayerLocalFlag(PlayerLocalFlags.OverrideTransportServerTime) || base.CanNeverSee(obj);
    }

    public void CharmSpellInitialize()
    {
        var charm = GetFirstControlled();

        if (charm == null)
            return;

        var charmInfo = charm.GetCharmInfo();

        if (charmInfo == null)
        {
            Log.Logger.Error("Player:CharmSpellInitialize(): the player's charm ({0}) has no charminfo!", charm.GUID);

            return;
        }

        PetSpells petSpells = new()
        {
            PetGUID = charm.GUID
        };

        if (charm.IsTypeId(TypeId.Unit))
        {
            petSpells.ReactState = charm.AsCreature.ReactState;
            petSpells.CommandState = charmInfo.GetCommandState();
        }

        for (byte i = 0; i < SharedConst.ActionBarIndexMax; ++i)
            petSpells.ActionButtons[i] = charmInfo.GetActionBarEntry(i).PackedData;

        for (byte i = 0; i < SharedConst.MaxSpellCharm; ++i)
        {
            var cspell = charmInfo.GetCharmSpell(i);

            if (cspell.Action != 0)
                petSpells.Actions.Add(cspell.PackedData);
        }

        // Cooldowns
        if (!charm.IsTypeId(TypeId.Player))
            charm.SpellHistory.WritePacket(petSpells);

        SendPacket(petSpells);
    }

    public void CleanupAfterTaxiFlight()
    {
        Taxi.ClearTaxiDestinations(); // not destinations, clear source node
        Dismount();
        RemoveUnitFlag(UnitFlags.RemoveClientControl | UnitFlags.OnTaxi);
    }

    public void CleanupChannels()
    {
        while (!JoinedChannels.Empty())
        {
            var ch = JoinedChannels.FirstOrDefault();
            JoinedChannels.RemoveAt(0); // remove from player's channel list

            if (ch == null)
                continue;

            ch.LeaveChannel(this, false); // not send to client, not remove from player's channel list

            // delete channel if empty
            var cMgr = ChannelManagerFactory.ForTeam(Team);

            if (cMgr == null)
                continue;

            if (ch.IsConstant)
                cMgr.LeftChannel(ch.ChannelId, ch.ZoneEntry);
        }

        Log.Logger.Debug("Player {0}: channels cleaned up!", GetName());
    }

    public override void CleanupsBeforeDelete(bool finalCleanup = true)
    {
        TradeCancel(false);
        DuelComplete(DuelCompleteType.Interrupted);

        base.CleanupsBeforeDelete(finalCleanup);
    }

    //Clears the Menu
    public void ClearGossipMenu()
    {
        PlayerTalkClass.ClearMenus();
    }

    public void ClearResurrectRequestData()
    {
        _resurrectionData = null;
    }

    public void ClearSelfResSpell()
    {
        ClearDynamicUpdateFieldValues(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.SelfResSpells));
    }

    public override void ClearUpdateMask(bool remove)
    {
        Values.ClearChangesMask(PlayerData);
        Values.ClearChangesMask(ActivePlayerData);
        base.ClearUpdateMask(remove);
    }

    public void ClearWhisperWhiteList()
    {
        _whisperList.Clear();
    }

    // Closes the Menu
    public void CloseGossipMenu()
    {
        PlayerTalkClass.SendCloseGossip();
    }

    public void ContinueTaxiFlight()
    {
        var sourceNode = Taxi.GetTaxiSource();

        if (sourceNode == 0)
            return;

        Log.Logger.Debug("WORLD: Restart character {0} taxi flight", GUID.ToString());

        var mountDisplayId = GameObjectManager.GetTaxiMountDisplayId(sourceNode, Team, true);

        if (mountDisplayId == 0)
            return;

        var path = Taxi.GetCurrentTaxiPath();

        // search appropriate start path node
        uint startNode = 0;

        var nodeList = CliDB.TaxiPathNodesByPath[path];

        var distNext = Location.GetExactDistSq(nodeList[0].Loc.X, nodeList[0].Loc.Y, nodeList[0].Loc.Z);

        for (var i = 1; i < nodeList.Length; ++i)
        {
            var node = nodeList[i];
            var prevNode = nodeList[i - 1];

            // skip nodes at another map
            if (node.ContinentID != Location.MapId)
                continue;

            var distPrev = distNext;

            distNext = Location.GetExactDistSq(node.Loc.X, node.Loc.Y, node.Loc.Z);

            var distNodes =
                (node.Loc.X - prevNode.Loc.X) * (node.Loc.X - prevNode.Loc.X) +
                (node.Loc.Y - prevNode.Loc.Y) * (node.Loc.Y - prevNode.Loc.Y) +
                (node.Loc.Z - prevNode.Loc.Z) * (node.Loc.Z - prevNode.Loc.Z);

            if (!(distNext + distPrev < distNodes))
                continue;

            startNode = (uint)i;

            break;
        }

        Session.SendDoFlight(mountDisplayId, path, startNode);
    }

    //Core
    public bool Create(ulong guidlow, CharacterCreateInfo createInfo)
    {
        Create(ObjectGuid.Create(HighGuid.Player, guidlow));

        SetName(createInfo.Name);

        var info = GameObjectManager.GetPlayerInfo(createInfo.RaceId, createInfo.ClassId);

        if (info == null)
        {
            Log.Logger.Error("PlayerCreate: Possible hacking-attempt: Account {0} tried creating a character named '{1}' with an invalid race/class pair ({2}/{3}) - refusing to do so.",
                             Session.AccountId,
                             GetName(),
                             createInfo.RaceId,
                             createInfo.ClassId);

            return false;
        }

        if (!CliDB.ChrClassesStorage.TryGetValue((uint)createInfo.ClassId, out var cEntry))
        {
            Log.Logger.Error("PlayerCreate: Possible hacking-attempt: Account {0} tried creating a character named '{1}' with an invalid character class ({2}) - refusing to do so (wrong DBC-files?)",
                             Session.AccountId,
                             GetName(),
                             createInfo.ClassId);

            return false;
        }

        if (!Session.ValidateAppearance(createInfo.RaceId, createInfo.ClassId, createInfo.Sex, createInfo.Customizations))
        {
            Log.Logger.Error("Player.Create: Possible hacking-attempt: Account {0} tried creating a character named '{1}' with invalid appearance attributes - refusing to do so",
                             Session.AccountId,
                             GetName());

            return false;
        }

        var position = createInfo.UseNPE && info.CreatePositionNpe.HasValue ? info.CreatePositionNpe.Value : info.CreatePosition;

        _createTime = GameTime.CurrentTime;
        CreateMode = createInfo.UseNPE && info.CreatePositionNpe.HasValue ? PlayerCreateMode.NPE : PlayerCreateMode.Normal;

        Location.Relocate(position.Loc);

        Location.Map = MapManager.CreateMap(position.Loc.MapId, this);
        CheckAddToMap();

        if (position.TransportGuid.HasValue)
        {
            var transport = ObjectAccessor.GetTransport(this, ObjectGuid.Create(HighGuid.Transport, position.TransportGuid.Value));

            if (transport != null)
            {
                transport.AddPassenger(this);
                MovementInfo.Transport.Pos.Relocate(position.Loc);
                var transportPos = position.Loc.Copy();
                transport.CalculatePassengerPosition(transportPos);
                Location.Relocate(transportPos);
            }
        }

        // set initial homebind position
        SetHomebind(Location, Location.Area);

        var powertype = cEntry.DisplayPower;

        ObjectScale = 1.0f;

        SetFactionForRace(createInfo.RaceId);

        if (!PlayerComputators.IsValidGender(createInfo.Sex))
        {
            Log.Logger.Error("Player:Create: Possible hacking-attempt: Account {0} tried creating a character named '{1}' with an invalid gender ({2}) - refusing to do so",
                             Session.AccountId,
                             GetName(),
                             createInfo.Sex);

            return false;
        }

        Race = createInfo.RaceId;
        Class = createInfo.ClassId;
        Gender = createInfo.Sex;
        SetPowerType(powertype, false);
        InitDisplayIds();

        if ((RealmType)Configuration.GetDefaultValue("GameType", 0) == RealmType.PVP || (RealmType)Configuration.GetDefaultValue("GameType", 0) == RealmType.RPPVP)
        {
            SetPvpFlag(UnitPVPStateFlags.PvP);
            SetUnitFlag(UnitFlags.PlayerControlled);
        }

        SetUnitFlag2(UnitFlags2.RegeneratePower);
        SetHoverHeight(1.0f); // default for players in 3.0.3

        SetWatchedFactionIndex(0xFFFFFFFF);

        SetCustomizations(createInfo.Customizations);
        SetRestState(RestTypes.XP, Session.IsARecruiter || Session.RecruiterId != 0 ? PlayerRestState.RAFLinked : PlayerRestState.Normal);
        SetRestState(RestTypes.Honor, PlayerRestState.Normal);
        NativeGender = createInfo.Sex;
        SetInventorySlotCount(InventorySlots.DefaultSize);

        // set starting level
        SetLevel(GetStartLevel(createInfo.RaceId, createInfo.ClassId, createInfo.TemplateSet));

        InitRunes();

        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Coinage), Configuration.GetDefaultValue("StartPlayerMoney", 0ul));

        // Played time
        _lastTick = GameTime.CurrentTime;
        TotalPlayedTime = 0;
        LevelPlayedTime = 0;

        // base stats and related field values
        InitStatsForLevel();
        InitTaxiNodesForLevel();
        InitTalentForLevel();
        InitializeSkillFields();
        InitPrimaryProfessions(); // to max set before any spell added

        // apply original stats mods before spell loading or item equipment that call before equip _RemoveStatsMods()
        UpdateMaxHealth(); // Update max Health (for add bonus from stamina)
        SetFullHealth();
        SetFullPower(PowerType.Mana);

        // original spells
        LearnDefaultSkills();
        LearnCustomSpells();

        // Original action bar. Do not use Player.AddActionButton because we do not have skill spells loaded at this time
        // but checks will still be performed later when loading character from db in Player._LoadActions
        foreach (var action in info.Actions)
        {
            // create new button
            ActionButton ab = new();

            // set data
            ab.SetActionAndType(action.Action, (ActionButtonType)action.Type);

            _actionButtons[action.Button] = ab;
        }

        // original items
        foreach (var initialItem in info.Items)
            StoreNewItemInBestSlots(initialItem.ItemId, initialItem.Amount, info.ItemContext);

        // bags and main-hand weapon must equipped at this moment
        // now second pass for not equipped (offhand weapon/shield if it attempt equipped before main-hand weapon)
        var inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();

        for (var i = InventorySlots.ItemStart; i < inventoryEnd; i++)
        {
            var pItem = GetItemByPos(InventorySlots.Bag0, i);

            if (pItem != null)
            {
                // equip offhand weapon/shield if it attempt equipped before main-hand weapon
                var msg = CanEquipItem(ItemConst.NullSlot, out var eDest, pItem, false);

                if (msg == InventoryResult.Ok)
                {
                    RemoveItem(InventorySlots.Bag0, i, true);
                    EquipItem(eDest, pItem, true);
                }
                // move other items to more appropriate slots
                else
                {
                    List<ItemPosCount> sDest = new();
                    msg = CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, sDest, pItem);

                    if (msg == InventoryResult.Ok)
                    {
                        RemoveItem(InventorySlots.Bag0, i, true);
                        StoreItem(sDest, pItem, true);
                    }
                }
            }
        }
        // all item positions resolved

        var defaultSpec = DB2Manager.GetDefaultChrSpecializationForClass(Class);

        if (defaultSpec != null)
        {
            SetActiveTalentGroup(defaultSpec.OrderIndex);
            SetPrimarySpecialization(defaultSpec.Id);
        }

        GetThreatManager().Initialize();

        ApplyCustomConfigs();

        return true;
    }

    public void CreateGarrison(uint garrSiteId)
    {
        Garrison = ClassFactory.ResolvePositional<Garrison>(this);

        if (!Garrison.Create(garrSiteId))
            Garrison = null;
    }

    public override void DestroyForPlayer(Player target)
    {
        base.DestroyForPlayer(target);

        if (target != this)
            return;

        for (var i = EquipmentSlot.Start; i < InventorySlots.BankBagEnd; ++i)
        {
            if (_items[i] == null)
                continue;

            _items[i].DestroyForPlayer(target);
        }

        for (var i = InventorySlots.ReagentStart; i < InventorySlots.ChildEquipmentEnd; ++i)
        {
            if (_items[i] == null)
                continue;

            _items[i].DestroyForPlayer(target);
        }
    }

    public override void Dispose()
    {
        // Note: buy back item already deleted from DB when player was saved
        for (byte i = 0; i < (int)PlayerSlots.Count; ++i)
            if (_items[i] != null)
                _items[i].Dispose();

        _spells.Clear();
        _specializationInfo = null;
        Mails.Clear();

        foreach (var item in _mailItems.Values)
            item.Dispose();

        PlayerTalkClass.ClearMenus();
        ItemSetEff.Clear();

        DeclinedNames = null;
        _runes = null;
        _achievementSys = null;
        ReputationMgr = null;

        CinematicMgr.Dispose();

        for (byte i = 0; i < SharedConst.VoidStorageMaxSlot; ++i)
            _voidStorageItems[i] = null;

        ClearResurrectRequestData();

        WorldMgr.DecreasePlayerCount();

        base.Dispose();
    }

    //new
    public uint DoRandomRoll(uint minimum, uint maximum)
    {
        var roll = RandomHelper.URand(minimum, maximum);

        RandomRoll randomRoll = new()
        {
            Min = (int)minimum,
            Max = (int)maximum,
            Result = (int)roll,
            Roller = GUID,
            RollerWowAccount = Session.AccountGUID
        };

        if (Group != null)
            Group.BroadcastPacket(randomRoll, false);
        else
            SendPacket(randomRoll);

        return roll;
    }

    public double EnvironmentalDamage(EnviromentalDamage type, double damage)
    {
        if (IsImmuneToEnvironmentalDamage())
            return 0;

        damage = damage * GetTotalAuraMultiplier(AuraType.ModEnvironmentalDamageTaken);

        // Absorb, resist some environmental damage type
        double absorb = 0;
        double resist = 0;
        var dmgSchool = GetEnviormentDamageType(type);

        switch (type)
        {
            case EnviromentalDamage.Lava:
            case EnviromentalDamage.Slime:
                DamageInfo dmgInfo = new(this, this, damage, null, dmgSchool, DamageEffectType.Direct, WeaponAttackType.BaseAttack);
                UnitCombatHelpers.CalcAbsorbResist(dmgInfo);
                absorb = dmgInfo.Absorb;
                resist = dmgInfo.Resist;
                damage = dmgInfo.Damage;

                break;
        }

        UnitCombatHelpers.DealDamageMods(null, this, ref damage, ref absorb);

        EnvironmentalDamageLog packet = new()
        {
            Victim = GUID,
            Type = type != EnviromentalDamage.FallToVoid ? type : EnviromentalDamage.Fall,
            Amount = (int)damage,
            Absorbed = (int)absorb,
            Resisted = (int)resist
        };

        var finalDamage = UnitCombatHelpers.DealDamage(null, this, damage, null, DamageEffectType.Self, dmgSchool, null, false);
        packet.LogData.Initialize(this);

        SendCombatLogMessage(packet);

        if (!IsAlive)
        {
            if (type == EnviromentalDamage.Fall) // DealDamage not apply item durability loss at self damage
            {
                Log.Logger.Debug($"Player::EnvironmentalDamage: Player '{GetName()}' ({GUID}) fall to death, losing {Configuration.GetDefaultValue("DurabilityLoss:OnDeath", 10.0f)} durability");
                DurabilityLossAll(Configuration.GetDefaultValue("DurabilityLoss:OnDeath", 10.0f) / 100, false);
                // durability lost message
                SendDurabilityLoss(this, Configuration.GetDefaultValue("DurabilityLoss:OnDeath", 10u));
            }

            UpdateCriteria(CriteriaType.DieFromEnviromentalDamage, 1, (ulong)type);
        }

        return finalDamage;
    }

    public void FinishTaxiFlight()
    {
        if (!IsInFlight)
            return;

        MotionMaster.Remove(MovementGeneratorType.Flight);
        Taxi.ClearTaxiDestinations(); // not destinations, clear source node
    }

    public ActionButton GetActionButton(byte button)
    {
        var actionButton = _actionButtons.LookupByKey(button);

        if (actionButton == null || actionButton.UState == ActionButtonUpdateState.Deleted)
            return null;

        return actionButton;
    }

    public long GetBarberShopCost(List<ChrCustomizationChoice> newCustomizations)
    {
        if (HasAuraType(AuraType.RemoveBarberShopCost))
            return 0;

        var bsc = CliDB.BarberShopCostBaseGameTable.GetRow(Level);

        if (bsc == null) // shouldn't happen
            return 0;

        long cost = 0;

        foreach (var newChoice in newCustomizations)
        {
            var currentCustomizationIndex = PlayerData.Customizations.FindIndexIf(currentCustomization => currentCustomization.ChrCustomizationOptionID == newChoice.ChrCustomizationOptionID);

            if (currentCustomizationIndex != -1 && PlayerData.Customizations[currentCustomizationIndex].ChrCustomizationChoiceID == newChoice.ChrCustomizationChoiceID)
                continue;

            if (CliDB.ChrCustomizationOptionStorage.TryGetValue(newChoice.ChrCustomizationOptionID, out var customizationOption))
                cost += (long)(bsc.Cost * customizationOption.BarberShopCostModifier);
        }

        return cost;
    }

    //Cheat Commands
    public bool GetCommandStatus(PlayerCommandStates command)
    {
        return (_activeCheats & command) != 0;
    }

    public uint GetCorpseReclaimDelay(bool pvp)
    {
        if (pvp)
        {
            if (!Configuration.GetDefaultValue("Death:CorpseReclaimDelay:PvP", true))
                return PlayerConst.copseReclaimDelay[0];
        }
        else if (!Configuration.GetDefaultValue("Death:CorpseReclaimDelay:PvE", true))
            return 0;

        var now = GameTime.CurrentTime;
        // 0..2 full period
        // should be ceil(x)-1 but not floor(x)
        var count = (ulong)(now < _deathExpireTime - 1 ? (_deathExpireTime - 1 - now) / PlayerConst.DeathExpireStep : 0);

        return PlayerConst.copseReclaimDelay[count];
    }

    public CufProfile GetCufProfile(byte id)
    {
        return _cufProfiles[id];
    }

    public uint GetCurrencyMaxQuantity(CurrencyTypesRecord currency, bool onLoad = false, bool onUpdateVersion = false)
    {
        if (!currency.HasMaxQuantity(onLoad, onUpdateVersion))
            return 0;

        var maxQuantity = currency.MaxQty;

        if (currency.MaxQtyWorldStateID != 0)
            maxQuantity = (uint)WorldStateManager.GetValue(currency.MaxQtyWorldStateID, Location.Map);

        uint increasedCap = 0;

        if (currency.GetFlags().HasFlag(CurrencyTypesFlags.DynamicMaximum))
            increasedCap = GetCurrencyIncreasedCapQuantity(currency.Id);

        return maxQuantity + increasedCap;
    }

    public uint GetCurrencyQuantity(uint id)
    {
        return _currencyStorage.LookupByKey(id)?.Quantity ?? 0;
    }

    public uint GetCurrencyTrackedQuantity(uint id)
    {
        return _currencyStorage.LookupByKey(id)?.TrackedQuantity ?? 0;
    }

    public uint GetCurrencyWeeklyQuantity(uint id)
    {
        return _currencyStorage.LookupByKey(id)?.WeeklyQuantity ?? 0;
    }

    public uint GetCustomizationChoice(uint chrCustomizationOptionId)
    {
        var choiceIndex = PlayerData.Customizations.FindIndexIf(choice => choice.ChrCustomizationOptionID == chrCustomizationOptionId);

        return choiceIndex >= 0 ? PlayerData.Customizations[choiceIndex].ChrCustomizationChoiceID : 0;
    }

    public GameObject GetGameObjectIfCanInteractWith(ObjectGuid guid)
    {
        if (guid.IsEmpty)
            return null;

        if (!Location.IsInWorld)
            return null;

        if (IsInFlight)
            return null;

        // exist
        var go = ObjectAccessor.GetGameObject(this, guid);

        if (go == null)
            return null;

        // Players cannot interact with gameobjects that use the "Point" icon
        if (go.Template.IconName == "Point")
            return null;

        return !go.IsWithinDistInMap(this) ? null : go;
    }

    public GameObject GetGameObjectIfCanInteractWith(ObjectGuid guid, GameObjectTypes type)
    {
        var go = GetGameObjectIfCanInteractWith(guid);

        return go?.GoType != type ? null : go;
    }

    public uint GetGossipTextId(WorldObject source)
    {
        return source == null ? SharedConst.DefaultGossipMessage : GetGossipTextId(PlayerComputators.GetDefaultGossipMenuForSource(source), source);
    }

    public uint GetGossipTextId(uint menuId, WorldObject source)
    {
        uint textId = SharedConst.DefaultGossipMessage;

        if (menuId == 0)
            return textId;

        var menuBounds = GameObjectManager.GetGossipMenusMapBounds(menuId);

        foreach (var menu in menuBounds.Where(menu => ConditionManager.IsObjectMeetToConditions(this, source, menu.Conditions)))
            textId = menu.TextId;

        return textId;
    }

    public Mail GetMail(ulong id)
    {
        return Mails.Find(p => p.MessageID == id);
    }

    public Item GetMItem(ulong id)
    {
        return _mailItems.LookupByKey(id);
    }

    //Creature
    public Creature GetNPCIfCanInteractWith(ObjectGuid guid, NPCFlags npcFlags, NPCFlags2 npcFlags2)
    {
        // unit checks
        if (guid.IsEmpty)
            return null;

        if (!Location.IsInWorld)
            return null;

        if (IsInFlight)
            return null;

        // exist (we need look pets also for some interaction (quest/etc)
        var creature = ObjectAccessor.GetCreatureOrPetOrVehicle(this, guid);

        if (creature == null)
            return null;

        // Deathstate checks
        if (!IsAlive && !Convert.ToBoolean(creature.Template.TypeFlags & CreatureTypeFlags.VisibleToGhosts))
            return null;

        // alive or spirit healer
        if (!creature.IsAlive && !Convert.ToBoolean(creature.Template.TypeFlags & CreatureTypeFlags.InteractWhileDead))
            return null;

        // appropriate npc type
        bool HasNpcFlags()
        {
            if (npcFlags == 0 && npcFlags2 == 0)
                return true;

            if (creature.HasNpcFlag(npcFlags))
                return true;

            return creature.HasNpcFlag2(npcFlags2);
        }

        if (!HasNpcFlags())
            return null;

        // not allow interaction under control, but allow with own pets
        if (!creature.CharmerGUID.IsEmpty)
            return null;

        // not unfriendly/hostile
        if (creature.WorldObjectCombat.GetReactionTo(this) <= ReputationRank.Unfriendly)
            return null;

        // not too far, taken from CGGameUI::SetInteractTarget
        return !creature.Location.IsWithinDistInMap(this, creature.CombatReach + 4.0f) ? null : creature;
    }

    public int GetReputation(uint factionentry)
    {
        return ReputationMgr.GetReputation(CliDB.FactionStorage.LookupByKey(factionentry));
    }

    public float GetReputationPriceDiscount(Creature creature)
    {
        return GetReputationPriceDiscount(creature.WorldObjectCombat.GetFactionTemplateEntry());
    }

    public float GetReputationPriceDiscount(FactionTemplateRecord factionTemplate)
    {
        if (factionTemplate == null || factionTemplate.Faction == 0)
            return 1.0f;

        var rank = GetReputationRank(factionTemplate.Faction);

        if (rank <= ReputationRank.Neutral)
            return 1.0f;

        return 1.0f - 0.05f * (rank - ReputationRank.Neutral);
    }

    public ReputationRank GetReputationRank(uint faction)
    {
        var factionEntry = CliDB.FactionStorage.LookupByKey(faction);

        return ReputationMgr.GetRank(factionEntry);
    }

    public bool GetsRecruitAFriendBonus(bool forXP)
    {
        var recruitAFriend = false;

        if (Level > Configuration.GetDefaultValue("RecruitAFriend:MaxLevel", 60) && forXP)
            return false;

        var group = Group;

        if (group == null)
            return false;

        for (var refe = group.FirstMember; refe != null; refe = refe.Next())
        {
            var player = refe.Source;

            if (player == null)
                continue;

            if (!player.IsAtRecruitAFriendDistance(this))
                continue; // member (alive or dead) or his corpse at req. distance

            if (forXP)
            {
                // level must be allowed to get RaF bonus
                if (player.Level > Configuration.GetDefaultValue("RecruitAFriend:MaxLevel", 60))
                    continue;

                // level difference must be small enough to get RaF bonus, UNLESS we are lower level
                if (player.Level < Level)
                    if (Level - player.Level > Configuration.GetDefaultValue("RecruitAFriend:MaxDifference", 4))
                        continue;
            }

            var aRecruitedB = player.Session.RecruiterId == Session.AccountId;
            var bRecruitedA = Session.RecruiterId == player.Session.AccountId;

            if (!aRecruitedB && !bRecruitedA)
                continue;

            recruitAFriend = true;

            break;
        }

        return recruitAFriend;
    }

    public uint GetStartLevel(Race race, PlayerClass playerClass, uint? characterTemplateId = null)
    {
        var startLevel = Configuration.GetDefaultValue("StartPlayerLevel", 1u);

        if (CliDB.ChrRacesStorage.LookupByKey(race).GetFlags().HasAnyFlag(ChrRacesFlag.IsAlliedRace))
            startLevel = Configuration.GetDefaultValue("StartAlliedRacePlayerLevel", 10u);

        startLevel = playerClass switch
        {
            PlayerClass.Deathknight when race is Race.PandarenAlliance or Race.PandarenHorde => Math.Max(Configuration.GetDefaultValue("StartAlliedRacePlayerLevel", 10u), startLevel),
            PlayerClass.Deathknight                                                          => Math.Max(Configuration.GetDefaultValue("StartDeathKnightPlayerLevel", 8u), startLevel),
            PlayerClass.DemonHunter                                                          => Math.Max(Configuration.GetDefaultValue("StartDemonHunterPlayerLevel", 8u), startLevel),
            PlayerClass.Evoker                                                               => Math.Max(Configuration.GetDefaultValue("StartEvokerPlayerLevel", 58u), startLevel),
            _                                                                                => startLevel
        };

        if (characterTemplateId.HasValue)
        {
            if (Session.HasPermission(RBACPermissions.UseCharacterTemplates))
            {
                var charTemplate = CharacterTemplateDataStorage.GetCharacterTemplate(characterTemplateId.Value);

                if (charTemplate != null)
                    startLevel = Math.Max(charTemplate.Level, startLevel);
            }
            else
                Log.Logger.Warning($"Account: {Session.AccountId} (IP: {Session.RemoteAddress}) tried to use a character template without given permission. Possible cheating attempt.");
        }

        if (Session.HasPermission(RBACPermissions.UseStartGmLevel))
            startLevel = Math.Max(Configuration.GetDefaultValue("GM:StartLevel", 1u), startLevel);

        return startLevel;
    }

    public Creature GetSummonedBattlePet()
    {
        var summonedBattlePet = ObjectAccessor.GetCreatureOrPetOrVehicle(this, CritterGUID);

        if (summonedBattlePet == null)
            return null;

        if (!SummonedBattlePetGUID.IsEmpty && SummonedBattlePetGUID == summonedBattlePet.BattlePetCompanionGUID)
            return summonedBattlePet;

        return null;
    }

    public override UpdateFieldFlag GetUpdateFieldFlagsFor(Player target)
    {
        var flags = base.GetUpdateFieldFlagsFor(target);

        if (IsInSameRaidWith(target))
            flags |= UpdateFieldFlag.PartyMember;

        return flags;
    }

    public Item GetWeaponForAttack(WeaponAttackType attackType, bool useable = false)
    {
        byte slot;

        switch (attackType)
        {
            case WeaponAttackType.BaseAttack:
                slot = EquipmentSlot.MainHand;

                break;

            case WeaponAttackType.OffAttack:
                slot = EquipmentSlot.OffHand;

                break;

            case WeaponAttackType.RangedAttack:
                slot = EquipmentSlot.MainHand;

                break;

            default:
                return null;
        }

        Item item;

        if (useable)
            item = GetUseableItemByPos(InventorySlots.Bag0, slot);
        else
            item = GetItemByPos(InventorySlots.Bag0, slot);

        if (item == null || item.Template.Class != ItemClass.Weapon)
            return null;

        if (attackType == WeaponAttackType.RangedAttack != item.Template.IsRangedWeapon)
            return null;

        if (!useable)
            return item;

        if (item.IsBroken)
            return null;

        return item;
    }

    public void GiveLevel(uint level)
    {
        var oldLevel = Level;

        if (level == oldLevel)
            return;

        var guild = Guild;

        guild?.UpdateMemberData(this, GuildMemberData.Level, level);

        var info = GameObjectManager.GetPlayerLevelInfo(Race, Class, level);

        GameObjectManager.GetPlayerClassLevelInfo(Class, level, out var basemana);

        LevelUpInfo packet = new()
        {
            Level = level,
            HealthDelta = 0,
            PowerDelta =

            {
                // @todo find some better solution
                [
                 0] = (int)basemana - (int)GetCreateMana(),

                [
                 1] = 0,

                [
                 2] = 0,

                [
                 3] = 0,

                [
                 4] = 0,

                [
                 5] = 0,

                [
                 6] = 0
            }
        };

        for (var i = Stats.Strength; i < Stats.Max; ++i)
            packet.StatDelta[(int)i] = info.Stats[(int)i] - (int)GetCreateStat(i);

        packet.NumNewTalents = (int)(DB2Manager.GetNumTalentsAtLevel(level, Class) - DB2Manager.GetNumTalentsAtLevel(oldLevel, Class));
        packet.NumNewPvpTalentSlots = DB2Manager.GetPvpTalentNumSlotsAtLevel(level, Class) - DB2Manager.GetPvpTalentNumSlotsAtLevel(oldLevel, Class);

        SendPacket(packet);

        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.NextLevelXP), GameObjectManager.GetXPForLevel(level));

        //update level, max level of skills
        LevelPlayedTime = 0; // Level Played Time reset

        _ApplyAllLevelScaleItemMods(false);

        SetLevel(level, false);

        UpdateSkillsForLevel();
        LearnDefaultSkills();
        LearnSpecializationSpells();

        // save base values (bonuses already included in stored stats
        for (var i = Stats.Strength; i < Stats.Max; ++i)
            SetCreateStat(i, info.Stats[(int)i]);

        SetCreateHealth(0);
        SetCreateMana(basemana);

        InitTalentForLevel();
        InitTaxiNodesForLevel();

        UpdateAllStats();

        _ApplyAllLevelScaleItemMods(true); // Moved to above SetFullHealth so player will have full health from Heirlooms

        var artifactAura = GetAura(PlayerConst.ArtifactsAllWeaponsGeneralWeaponEquippedPassive);

        if (artifactAura != null)
        {
            var artifact = GetItemByGuid(artifactAura.CastItemGuid);

            artifact?.CheckArtifactRelicSlotUnlock(this);
        }

        // Only health and mana are set to maximum.
        SetFullHealth();
        SetFullPower(PowerType.Mana);

        // update level to hunter/summon pet
        var pet = CurrentPet;

        pet?.SynchronizeLevelWithOwner();

        var mailReward = GameObjectManager.GetMailLevelReward(level, (uint)SharedConst.GetMaskForRace(Race));

        if (mailReward != null)
        {
            //- TODO: Poor design of mail system
            SQLTransaction trans = new();
            ClassFactory.ResolvePositional<MailDraft>(mailReward.MailTemplateId, true).SendMailTo(trans, this, new MailSender(MailMessageType.Creature, mailReward.SenderEntry));
            CharacterDatabase.CommitTransaction(trans);
        }

        UpdateCriteria(CriteriaType.ReachLevel);
        UpdateCriteria(CriteriaType.ActivelyReachLevel, level);

        PushQuests();

        ScriptManager.ForEach<IPlayerOnLevelChanged>(Class, p => p.OnLevelChanged(this, oldLevel));
    }

    public void GiveXP(uint xp, Unit victim, float groupRate = 1.0f)
    {
        if (xp < 1)
            return;

        if (!IsAlive && BattlegroundId == 0)
            return;

        if (HasPlayerFlag(PlayerFlags.NoXPGain))
            return;

        if (victim != null && victim.IsTypeId(TypeId.Unit) && !victim.AsCreature.HasLootRecipient)
            return;

        var level = Level;

        ScriptManager.ForEach<IPlayerOnGiveXP>(p => p.OnGiveXP(this, ref xp, victim));

        // XP to money conversion processed in Player.RewardQuest
        if (IsMaxLevel)
            return;

        double bonusXP;
        var recruitAFriend = GetsRecruitAFriendBonus(true);

        // RaF does NOT stack with rested experience
        if (recruitAFriend)
            bonusXP = 2 * xp; // xp + bonus_xp must add up to 3 * xp for RaF; calculation for quests done client-side
        else
            bonusXP = victim != null ? RestMgr.GetRestBonusFor(RestTypes.XP, xp) : 0; // XP resting bonus

        LogXPGain packet = new()
        {
            Victim = victim?.GUID ?? ObjectGuid.Empty,
            Original = (int)(xp + bonusXP),
            Reason = victim != null ? PlayerLogXPReason.Kill : PlayerLogXPReason.NoKill,
            Amount = (int)xp,
            GroupBonus = groupRate
        };

        SendPacket(packet);

        var nextLvlXP = XPForNextLevel;
        var newXP = XP + xp + (uint)bonusXP;

        while (newXP >= nextLvlXP && !IsMaxLevel)
        {
            newXP -= nextLvlXP;

            if (!IsMaxLevel)
                GiveLevel(level + 1);

            level = Level;
            nextLvlXP = XPForNextLevel;
        }

        XP = newXP;
    }

    public void HandleBaseModFlatValue(BaseModGroup modGroup, double amount, bool apply)
    {
        if (modGroup >= BaseModGroup.End)
        {
            Log.Logger.Error($"Player.HandleBaseModFlatValue: Invalid BaseModGroup ({modGroup}) for player '{GetName()}' ({GUID})");

            return;
        }

        _auraBaseFlatMod[(int)modGroup] += apply ? amount : -amount;
        UpdateBaseModGroup(modGroup);
    }

    public void HandleFall(MovementInfo movementInfo)
    {
        // calculate total z distance of the fall
        var zDiff = _lastFallZ - movementInfo.Pos.Z;
        Log.Logger.Debug("zDiff = {0}", zDiff);

        //Players with low fall distance, Feather Fall or physical immunity (charges used) are ignored
        // 14.57 can be calculated by resolving damageperc formula below to 0
        if (!(zDiff >= 14.57f) ||
            IsDead ||
            IsGameMaster ||
            HasAuraType(AuraType.Hover) ||
            HasAuraType(AuraType.FeatherFall) ||
            HasAuraType(AuraType.Fly) ||
            IsImmunedToDamage(SpellSchoolMask.Normal))
            return;

        //Safe fall, fall height reduction
        var safeFall = GetTotalAuraModifier(AuraType.SafeFall);

        var damageperc = 0.018f * (zDiff - safeFall) - 0.2426f;

        if (!(damageperc > 0))
            return;

        var damage = damageperc * MaxHealth * Configuration.GetDefaultValue("Rate:Damage:Fall", 1.0f);

        var height = movementInfo.Pos.Z;
        height = Location.UpdateGroundPositionZ(movementInfo.Pos.X, movementInfo.Pos.Y, height);

        damage = damage * GetTotalAuraMultiplier(AuraType.ModifyFallDamagePct);

        if (damage > 0)
        {
            //Prevent fall damage from being more than the player maximum health
            if (damage > MaxHealth)
                damage = MaxHealth;

            // Gust of Wind
            if (HasAura(43621))
                damage = MaxHealth / 2f;

            var originalHealth = Health;
            var finalDamage = EnvironmentalDamage(EnviromentalDamage.Fall, damage);

            // recheck alive, might have died of EnvironmentalDamage, avoid cases when player die in fact like Spirit of Redemption case
            if (IsAlive && finalDamage < originalHealth)
                UpdateCriteria(CriteriaType.MaxDistFallenWithoutDying, (uint)zDiff * 100);
        }

        //Z given by moveinfo, LastZ, FallTime, WaterZ, MapZ, Damage, Safefall reduction
        Log.Logger.Debug($"FALLDAMAGE z={movementInfo.Pos.Z} sz={height} pZ={Location.Z} FallTime={movementInfo.Jump.FallTime} mZ={height} damage={damage} SF={safeFall}\nPlayer debug info:\n{GetDebugInfo()}");
    }

    //LoginFlag
    public bool HasAtLoginFlag(AtLoginFlags f)
    {
        return Convert.ToBoolean(LoginFlags & f);
    }

    public bool HasBeenGrantedLevelsFromRaF()
    {
        return _extraFlags.HasFlag(PlayerExtraFlags.GrantedLevelsFromRaf);
    }

    public bool HasCurrency(uint id, uint amount)
    {
        var playerCurrency = _currencyStorage.LookupByKey(id);

        return playerCurrency != null && playerCurrency.Quantity >= amount;
    }

    public bool HasEnoughMoney(ulong amount)
    {
        return Money >= amount;
    }

    public bool HasEnoughMoney(long amount)
    {
        if (amount > 0)
            return Money >= (ulong)amount;

        return true;
    }

    public bool HasLevelBoosted()
    {
        return _extraFlags.HasFlag(PlayerExtraFlags.LevelBoosted);
    }

    public bool HasPlayerFlag(PlayerFlags flags)
    {
        return (PlayerData.PlayerFlags & (uint)flags) != 0;
    }

    public bool HasPlayerFlagEx(PlayerFlagsEx flags)
    {
        return (PlayerData.PlayerFlagsEx & (uint)flags) != 0;
    }

    public bool HasPlayerLocalFlag(PlayerLocalFlags flags)
    {
        return (ActivePlayerData.LocalFlags & (int)flags) != 0;
    }

    public bool HasRaceChanged()
    {
        return _extraFlags.HasFlag(PlayerExtraFlags.HasRaceChanged);
    }

    public bool HasTitle(CharTitlesRecord title)
    {
        return HasTitle(title.MaskID);
    }

    public bool HasTitle(uint bitIndex)
    {
        var fieldIndexOffset = bitIndex / 64;

        if (fieldIndexOffset >= ActivePlayerData.KnownTitles.Size())
            return false;

        var flag = 1ul << ((int)bitIndex % 64);

        return (ActivePlayerData.KnownTitles[(int)fieldIndexOffset] & flag) != 0;
    }

    public bool HaveAtClient(WorldObject u)
    {
        lock (ClientGuiDs)
        {
            var one = u.GUID == GUID;
            var two = ClientGuiDs.Contains(u.GUID);

            return one || two;
        }
    }

    public void IncreaseCurrencyCap(uint id, uint amount)
    {
        if (amount == 0)
            return;

        var currency = CliDB.CurrencyTypesStorage.LookupByKey(id);

        // Check faction
        if ((currency.IsAlliance() && Team != TeamFaction.Alliance) ||
            (currency.IsHorde() && Team != TeamFaction.Horde))
            return;

        // Check dynamic maximum Id
        if (!currency.GetFlags().HasFlag(CurrencyTypesFlags.DynamicMaximum))
            return;

        // Ancient mana maximum cap
        if (id == (uint)CurrencyTypes.AncientMana)
        {
            var maxQuantity = GetCurrencyMaxQuantity(currency);

            if (maxQuantity + amount > PlayerConst.CurrencyMaxCapAncientMana)
                amount = PlayerConst.CurrencyMaxCapAncientMana - maxQuantity;
        }

        if (!_currencyStorage.TryGetValue(id, out var playerCurrency))
        {
            playerCurrency = new PlayerCurrency
            {
                State = PlayerCurrencyState.New,
                IncreasedCapQuantity = amount
            };

            _currencyStorage[id] = playerCurrency;
        }
        else
            playerCurrency.IncreasedCapQuantity += amount;

        if (playerCurrency.State != PlayerCurrencyState.New)
            playerCurrency.State = PlayerCurrencyState.Changed;

        SetCurrency packet = new()
        {
            Type = currency.Id,
            Quantity = (int)playerCurrency.Quantity,
            Flags = CurrencyGainFlags.None
        };

        if (playerCurrency.WeeklyQuantity / currency.GetScaler() > 0)
            packet.WeeklyQuantity = (int)playerCurrency.WeeklyQuantity;

        if (currency.IsTrackingQuantity())
            packet.TrackedQuantity = (int)playerCurrency.TrackedQuantity;

        packet.MaxQuantity = (int)GetCurrencyMaxQuantity(currency);
        packet.SuppressChatLog = currency.IsSuppressingChatLog();

        SendPacket(packet);
    }

    public void InitDataForForm(bool reapplyMods = false)
    {
        var form = ShapeshiftForm;

        if (CliDB.SpellShapeshiftFormStorage.TryGetValue((uint)form, out var ssEntry))
        {
            SetBaseAttackTime(WeaponAttackType.BaseAttack, ssEntry.CombatRoundTime);
            SetBaseAttackTime(WeaponAttackType.OffAttack, ssEntry.CombatRoundTime);
            SetBaseAttackTime(WeaponAttackType.RangedAttack, SharedConst.BaseAttackTime);
        }
        else
            SetRegularAttackTime();

        UpdateDisplayPower();

        // update auras at form change, ignore this at mods reapply (.reset stats/etc) when form not change.
        if (!reapplyMods)
            UpdateEquipSpellsAtFormChange();

        UpdateAttackPowerAndDamage();
        UpdateAttackPowerAndDamage(true);
    }

    public void InitDisplayIds()
    {
        var model = DB2Manager.GetChrModel(Race, NativeGender);

        if (model == null)
        {
            Log.Logger.Error($"Player {GUID} has incorrect race/gender pair. Can't init display ids.");

            return;
        }

        SetDisplayId(model.DisplayID);
        SetNativeDisplayId(model.DisplayID);
        SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.StateAnimID), DB2Manager.GetEmptyAnimStateID());
    }

    public void InitGossipMenu(uint menuId)
    {
        PlayerTalkClass.GossipMenu.MenuId = menuId;
    }

    public void InitStatsForLevel(bool reapplyMods = false)
    {
        if (reapplyMods) //reapply stats values only on .reset stats (level) command
            _RemoveAllStatBonuses();

        GameObjectManager.GetPlayerClassLevelInfo(Class, Level, out var basemana);

        var info = GameObjectManager.GetPlayerLevelInfo(Race, Class, Level);

        var expMaxLvl = (int)GameObjectManager.GetMaxLevelForExpansion(Session.Expansion);
        var confMaxLvl = Configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel);

        if (expMaxLvl == SharedConst.DefaultMaxLevel || expMaxLvl >= confMaxLvl)
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.MaxLevel), confMaxLvl);
        else
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.MaxLevel), expMaxLvl);

        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.NextLevelXP), GameObjectManager.GetXPForLevel(Level));

        if (ActivePlayerData.XP >= ActivePlayerData.NextLevelXP)
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.XP), ActivePlayerData.NextLevelXP - 1);

        // reset before any aura state sources (health set/aura apply)
        SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.AuraState), 0u);

        UpdateSkillsForLevel();

        // set default cast time multiplier
        SetModCastingSpeed(1.0f);
        SetModSpellHaste(1.0f);
        SetModHaste(1.0f);
        SetModRangedHaste(1.0f);
        SetModHasteRegen(1.0f);
        SetModTimeRate(1.0f);

        // reset size before reapply auras
        ObjectScale = 1.0f;

        // save base values (bonuses already included in stored stats
        for (var i = Stats.Strength; i < Stats.Max; ++i)
            SetCreateStat(i, info.Stats[(int)i]);

        for (var i = Stats.Strength; i < Stats.Max; ++i)
            SetStat(i, info.Stats[(int)i]);

        SetCreateHealth(0);

        //set create powers
        SetCreateMana(basemana);

        SetArmor((int)(GetCreateStat(Stats.Agility) * 2), 0);

        InitStatBuffMods();

        //reset rating fields values
        for (var index = 0; index < (int)CombatRating.Max; ++index)
            SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.CombatRatings, index), 0u);

        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModHealingDonePos), 0);
        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModHealingPercent), 1.0f);
        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModPeriodicHealingDonePercent), 1.0f);

        for (byte i = 0; i < (int)SpellSchools.Max; ++i)
        {
            SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModDamageDoneNeg, i), 0);
            SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModDamageDonePos, i), 0);
            SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModDamageDonePercent, i), 1.0f);
            SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModHealingDonePercent, i), 1.0f);
        }

        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModSpellPowerPercent), 1.0f);

        //reset attack power, damage and attack speed fields
        for (WeaponAttackType attackType = 0; attackType < WeaponAttackType.Max; ++attackType)
            SetBaseAttackTime(attackType, SharedConst.BaseAttackTime);

        SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.MinDamage), 0.0f);
        SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.MaxDamage), 0.0f);
        SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.MinOffHandDamage), 0.0f);
        SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.MaxOffHandDamage), 0.0f);
        SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.MinRangedDamage), 0.0f);
        SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.MaxRangedDamage), 0.0f);

        for (var i = 0; i < 3; ++i)
        {
            SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.WeaponDmgMultipliers, i), 1.0f);
            SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.WeaponAtkSpeedMultipliers, i), 1.0f);
        }

        SetAttackPower(0);
        SetAttackPowerMultiplier(0.0f);
        SetRangedAttackPower(0);
        SetRangedAttackPowerMultiplier(0.0f);

        // Base crit values (will be recalculated in UpdateAllStats() at loading and in _ApplyAllStatBonuses() at reset
        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.CritPercentage), 0.0f);
        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.OffhandCritPercentage), 0.0f);
        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.RangedCritPercentage), 0.0f);

        // Init spell schools (will be recalculated in UpdateAllStats() at loading and in _ApplyAllStatBonuses() at reset
        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.SpellCritPercentage), 0.0f);

        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ParryPercentage), 0.0f);
        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.BlockPercentage), 0.0f);

        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ShieldBlock), 0u);

        // Dodge percentage
        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.DodgePercentage), 0.0f);

        // set armor (resistance 0) to original value (create_agility*2)
        SetArmor((int)(GetCreateStat(Stats.Agility) * 2), 0);
        SetBonusResistanceMod(SpellSchools.Normal, 0);

        // set other resistance to original value (0)
        for (var spellSchool = SpellSchools.Holy; spellSchool < SpellSchools.Max; ++spellSchool)
        {
            SetResistance(spellSchool, 0);
            SetBonusResistanceMod(spellSchool, 0);
        }

        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModTargetResistance), 0);
        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ModTargetPhysicalResistance), 0);

        for (var i = 0; i < (int)SpellSchools.Max; ++i)
            SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.ManaCostModifier, i), 0);

        // Reset no reagent cost field
        SetNoRegentCostMask(new FlagArray128());

        // Init data for form but skip reapply item mods for form
        InitDataForForm(reapplyMods);

        // save new stats
        for (var i = PowerType.Mana; i < PowerType.Max; ++i)
            SetMaxPower(i, GetCreatePowerValue(i));

        SetMaxHealth(0); // stamina bonus will applied later

        // cleanup mounted state (it will set correctly at aura loading if player saved at mount.
        MountDisplayId = 0;

        // cleanup unit flags (will be re-applied if need at aura load).
        RemoveUnitFlag(UnitFlags.NonAttackable |
                       UnitFlags.RemoveClientControl |
                       UnitFlags.NotAttackable1 |
                       UnitFlags.ImmuneToPc |
                       UnitFlags.ImmuneToNpc |
                       UnitFlags.Looting |
                       UnitFlags.PetInCombat |
                       UnitFlags.Pacified |
                       UnitFlags.Stunned |
                       UnitFlags.InCombat |
                       UnitFlags.Disarmed |
                       UnitFlags.Confused |
                       UnitFlags.Fleeing |
                       UnitFlags.Uninteractible |
                       UnitFlags.Skinnable |
                       UnitFlags.Mount |
                       UnitFlags.OnTaxi);

        SetUnitFlag(UnitFlags.PlayerControlled); // must be set

        SetUnitFlag2(UnitFlags2.RegeneratePower); // must be set

        // cleanup player flags (will be re-applied if need at aura load), to avoid have ghost Id without ghost aura, for example.
        RemovePlayerFlag(PlayerFlags.AFK | PlayerFlags.DND | PlayerFlags.GM | PlayerFlags.Ghost);

        RemoveVisFlag(UnitVisFlags.All); // one form stealth modified bytes
        RemovePvpFlag(UnitPVPStateFlags.FFAPvp | UnitPVPStateFlags.Sanctuary);

        // restore if need some important flags
        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.LocalRegenFlags), (byte)0);
        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.AuraVision), (byte)0);

        if (reapplyMods) // reapply stats values only on .reset stats (level) command
            _ApplyAllStatBonuses();

        // set current level health and mana/energy to maximum after applying all mods.
        SetFullHealth();
        SetFullPower(PowerType.Mana);
        SetFullPower(PowerType.Energy);

        if (GetPower(PowerType.Rage) > GetMaxPower(PowerType.Rage))
            SetFullPower(PowerType.Rage);

        SetFullPower(PowerType.Focus);
        SetPower(PowerType.RunicPower, 0);

        // update level to hunter/summon pet
        CurrentPet?.SynchronizeLevelWithOwner();
    }

    //Taxi
    public void InitTaxiNodesForLevel()
    {
        Taxi.InitTaxiNodesForLevel(Race, Class, Level);
    }

    public bool IsAllowedToLoot(Creature creature)
    {
        if (!creature.IsDead)
            return false;

        if (HasPendingBind)
            return false;

        var loot = creature.GetLootForPlayer(this);

        if (loot == null || loot.IsLooted()) // nothing to loot or everything looted.
            return false;

        if (!loot.HasAllowedLooter(GUID) || (!loot.HasItemForAll() && !loot.HasItemFor(this))) // no loot in creature for this player
            return false;

        switch (loot.LootMethod)
        {
            case LootMethod.PersonalLoot:
            case LootMethod.FreeForAll:
                return true;

            case LootMethod.RoundRobin:
                // may only loot if the player is the loot roundrobin player
                // or if there are free/quest/conditional item for the player
                if (loot.RoundRobinPlayer.IsEmpty || loot.RoundRobinPlayer == GUID)
                    return true;

                return loot.HasItemFor(this);

            case LootMethod.MasterLoot:
            case LootMethod.GroupLoot:
            case LootMethod.NeedBeforeGreed:
                // may only loot if the player is the loot roundrobin player
                // or item over threshold (so roll(s) can be launched or to preview master looted items)
                // or if there are free/quest/conditional item for the player
                if (loot.RoundRobinPlayer.IsEmpty || loot.RoundRobinPlayer == GUID)
                    return true;

                return loot.HasOverThresholdItem() || loot.HasItemFor(this);
        }

        return false;
    }

    public override bool IsAlwaysDetectableFor(WorldObject seer)
    {
        if (base.IsAlwaysDetectableFor(seer))
            return true;

        if (Duel != null && Duel.State != DuelState.Challenged && Duel.Opponent == seer)
            return false;

        var seerPlayer = seer.AsPlayer;

        if (seerPlayer == null)
            return false;

        return IsGroupVisibleFor(seerPlayer);
    }

    // Used in triggers for check "Only to targets that grant experience or honor" req
    public bool IsHonorOrXPTarget(Unit victim)
    {
        var vLevel = victim.GetLevelForTarget(this);
        var kGrey = Formulas.GetGrayLevel(Level);

        // Victim level less gray level
        if (vLevel < kGrey && Configuration.GetDefaultValue("MinCreatureScaledXPRatio", 0) == 0)
            return false;

        var creature = victim.AsCreature;

        if (creature == null)
            return true;

        return !creature.IsCritter && !creature.IsTotem;
    }

    public override bool IsImmunedToSpellEffect(SpellInfo spellInfo, SpellEffectInfo spellEffectInfo, WorldObject caster, bool requireImmunityPurgesEffectAttribute = false)
    {
        // players are immune to taunt (the aura and the spell effect).
        if (spellEffectInfo.IsAuraType(AuraType.ModTaunt))
            return true;

        return spellEffectInfo.IsEffectName(SpellEffectName.AttackMe) || base.IsImmunedToSpellEffect(spellInfo, spellEffectInfo, caster, requireImmunityPurgesEffectAttribute);
    }

    public bool IsInAreaTriggerRadius(AreaTriggerRecord trigger)
    {
        if (trigger == null)
            return false;

        if (Location.MapId != trigger.ContinentID && !Location.PhaseShift.HasVisibleMapId(trigger.ContinentID))
            return false;

        if (trigger.PhaseID != 0 || trigger.PhaseGroupID != 0 || trigger.PhaseUseFlags != 0)
            if (!PhasingHandler.InDbPhaseShift(this, (PhaseUseFlagsValues)trigger.PhaseUseFlags, trigger.PhaseID, trigger.PhaseGroupID))
                return false;

        if (trigger.Radius > 0.0f)
        {
            // if we have radius check it
            var dist = Location.GetDistance(trigger.Pos.X, trigger.Pos.Y, trigger.Pos.Z);

            if (dist > trigger.Radius)
                return false;
        }
        else
        {
            Position center = new(trigger.Pos.X, trigger.Pos.Y, trigger.Pos.Z, trigger.BoxYaw);

            if (!Location.IsWithinBox(center, trigger.BoxLength / 2.0f, trigger.BoxWidth / 2.0f, trigger.BoxHeight / 2.0f))
                return false;
        }

        return true;
    }

    public bool IsInWhisperWhiteList(ObjectGuid guid)
    {
        return _whisperList.Contains(guid);
    }

    public bool IsMirrorTimerActive(MirrorTimerType type)
    {
        return _mirrorTimer[(int)type] == GetMaxTimer(type);
    }

    public override bool IsNeverVisibleFor(WorldObject seer)
    {
        if (base.IsNeverVisibleFor(seer))
            return true;

        return Session.PlayerLogout || Session.PlayerLoading;
    }

    public bool IsPetNeedBeTemporaryUnsummoned()
    {
        return !Location.IsInWorld || !IsAlive || IsMounted;
    }

    public bool IsRessurectRequestedBy(ObjectGuid guid)
    {
        if (!IsResurrectRequested)
            return false;

        return !_resurrectionData.Guid.IsEmpty && _resurrectionData.Guid == guid;
    }

    public bool IsSpellFitByClassAndRace(uint spellID)
    {
        var racemask = SharedConst.GetMaskForRace(Race);
        var classmask = ClassMask;

        var bounds = SpellManager.GetSkillLineAbilityMapBounds(spellID);

        if (bounds.Empty())
            return true;

        foreach (var spellIdx in bounds)
        {
            // skip wrong race skills
            if (spellIdx.RaceMask != 0 && (spellIdx.RaceMask & racemask) == 0)
                continue;

            // skip wrong class skills
            if (spellIdx.ClassMask != 0 && (spellIdx.ClassMask & classmask) == 0)
                continue;

            // skip wrong class and race skill saved in SkillRaceClassInfo.dbc
            if (DB2Manager.GetSkillRaceClassInfo(spellIdx.SkillLine, Race, Class) == null)
                continue;

            return true;
        }

        return false;
    }

    public bool IsVisibleGloballyFor(Player u)
    {
        if (u == null)
            return false;

        // Always can see self
        if (u.GUID == GUID)
            return true;

        // Visible units, always are visible for all players
        if (IsVisible())
            return true;

        // GMs are visible for higher gms (or players are visible for gms)
        if (!AccountManager.IsPlayerAccount(u.Session.Security))
            return Session.Security <= u.Session.Security;

        // non faction visibility non-breakable for non-GMs
        return false;
    }

    public void JoinedChannel(Channel c)
    {
        JoinedChannels.Add(c);
    }

    public void KillPlayer()
    {
        if (IsFlying && Transport == null)
            MotionMaster.MoveFall();

        SetRooted(true);

        StopMirrorTimers(); //disable timers(bars)

        SetDeathState(DeathState.Corpse);

        ReplaceAllDynamicFlags(UnitDynFlags.None);

        if (!CliDB.MapStorage.LookupByKey(Location.MapId).Instanceable() && !HasAuraType(AuraType.PreventResurrection))
            SetPlayerLocalFlag(PlayerLocalFlags.ReleaseTimer);
        else
            RemovePlayerLocalFlag(PlayerLocalFlags.ReleaseTimer);

        // 6 minutes until repop at graveyard
        DeathTimer = 6 * Time.MINUTE * Time.IN_MILLISECONDS;

        UpdateCorpseReclaimDelay(); // dependent at use SetDeathPvP() call before kill

        var corpseReclaimDelay = CalculateCorpseReclaimDelay();

        if (corpseReclaimDelay >= 0)
            SendCorpseReclaimDelay(corpseReclaimDelay);

        ScriptManager.ForEach<IPlayerOnDeath>(Class, a => a.OnDeath(this));
        // don't create corpse at this moment, player might be falling

        // update visibility
        UpdateObjectVisibility();
    }

    public void LeftChannel(Channel c)
    {
        JoinedChannels.Remove(c);
    }

    public bool MeetPlayerCondition(uint conditionId)
    {
        return !CliDB.PlayerConditionStorage.TryGetValue(conditionId, out var playerCondition) || ConditionManager.IsPlayerMeetingCondition(this, playerCondition);
    }

    public bool MeetsChrCustomizationReq(ChrCustomizationReqRecord req, PlayerClass playerClass, bool checkRequiredDependentChoices, List<ChrCustomizationChoice> selectedChoices)
    {
        if (!req.GetFlags().HasFlag(ChrCustomizationReqFlag.HasRequirements))
            return true;

        if (req.ClassMask != 0 && (req.ClassMask & (1 << ((int)playerClass - 1))) == 0)
            return false;

        if (req.AchievementID != 0 /*&& !HasAchieved(req->AchievementID)*/)
            return false;

        if (req.ItemModifiedAppearanceID != 0 && !CollectionMgr.HasItemAppearance(req.ItemModifiedAppearanceID).PermAppearance)
            return false;

        if (req.QuestID != 0 && !IsQuestRewarded((uint)req.QuestID))
            return false;

        if (!checkRequiredDependentChoices)
            return true;

        var requiredChoices = DB2Manager.GetRequiredCustomizationChoices(req.Id);

        return requiredChoices == null || requiredChoices.Keys.Select(key => requiredChoices[key].Any(requiredChoice => selectedChoices.Any(choice => choice.ChrCustomizationChoiceID == requiredChoice))).All(hasRequiredChoiceForOption => hasRequiredChoiceForOption);
    }

    public void ModifyCurrency(uint id, int amount, CurrencyGainSource gainSource = CurrencyGainSource.Cheat, CurrencyDestroyReason destroyReason = CurrencyDestroyReason.Cheat)
    {
        if (amount == 0)
            return;

        var currency = CliDB.CurrencyTypesStorage.LookupByKey(id);

        // Check faction
        if ((currency.IsAlliance() && Team != TeamFaction.Alliance) ||
            (currency.IsHorde() && Team != TeamFaction.Horde))
            return;

        // Check award condition
        if (currency.AwardConditionID != 0)
        {
            var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(currency.AwardConditionID);

            if (playerCondition != null && !ConditionManager.IsPlayerMeetingCondition(this, playerCondition))
                return;
        }

        var isGainOnRefund = gainSource is CurrencyGainSource.ItemRefund or
                                           CurrencyGainSource.GarrisonBuildingRefund or
                                           CurrencyGainSource.PlayerTraitRefund;

        if (amount > 0 && !isGainOnRefund && gainSource != CurrencyGainSource.Vendor)
        {
            amount = (int)(amount * GetTotalAuraMultiplierByMiscValue(AuraType.ModCurrencyGain, (int)id));
            amount = (int)(amount * GetTotalAuraMultiplierByMiscValue(AuraType.ModCurrencyCategoryGainPct, currency.CategoryID));
        }

        var scaler = currency.GetScaler();

        // Currency that is immediately converted into reputation with that faction instead
        if (CliDB.FactionStorage.TryGetValue((uint)currency.FactionID, out var factionEntry))
        {
            amount /= scaler;
            ReputationMgr.ModifyReputation(factionEntry, amount, false, true);

            return;
        }

        // Azerite
        if (id == (uint)CurrencyTypes.Azerite)
        {
            if (amount <= 0)
                return;

            var heartOfAzeroth = GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);

            heartOfAzeroth?.AsAzeriteItem.GiveXP((ulong)amount);

            return;
        }

        if (!_currencyStorage.TryGetValue(id, out var playerCurrency))
        {
            playerCurrency = new PlayerCurrency
            {
                State = PlayerCurrencyState.New
            };

            _currencyStorage.Add(id, playerCurrency);
        }

        // Weekly cap
        var weeklyCap = GetCurrencyWeeklyCap(currency);

        if (weeklyCap != 0 && amount > 0 && playerCurrency.WeeklyQuantity + amount > weeklyCap)
            if (!isGainOnRefund) // Ignore weekly cap for refund
                amount = (int)(weeklyCap - playerCurrency.WeeklyQuantity);

        // Max cap
        var maxCap = GetCurrencyMaxQuantity(currency, false, gainSource == CurrencyGainSource.UpdatingVersion);

        if (maxCap != 0 && amount > 0 && playerCurrency.Quantity + amount > maxCap)
            amount = (int)(maxCap - playerCurrency.Quantity);

        // Underflow protection
        if (amount < 0 && Math.Abs(amount) > playerCurrency.Quantity)
            amount = (int)(playerCurrency.Quantity * -1);

        if (amount == 0)
            return;

        if (playerCurrency.State != PlayerCurrencyState.New)
            playerCurrency.State = PlayerCurrencyState.Changed;

        playerCurrency.Quantity += (uint)amount;

        if (amount > 0 && !isGainOnRefund) // Ignore total values update for refund
        {
            if (weeklyCap != 0)
                playerCurrency.WeeklyQuantity += (uint)amount;

            if (currency.IsTrackingQuantity())
                playerCurrency.TrackedQuantity += (uint)amount;

            if (currency.HasTotalEarned())
                playerCurrency.EarnedQuantity += (uint)amount;

            UpdateCriteria(CriteriaType.CurrencyGained, id, (ulong)amount);
        }

        CurrencyChanged(id, amount);

        SetCurrency packet = new()
        {
            Type = currency.Id,
            Quantity = (int)playerCurrency.Quantity,
            Flags = CurrencyGainFlags.None // TODO: Check when flags are applied
        };

        if (playerCurrency.WeeklyQuantity / currency.GetScaler() > 0)
            packet.WeeklyQuantity = (int)playerCurrency.WeeklyQuantity;

        if (currency.HasMaxQuantity(false, gainSource == CurrencyGainSource.UpdatingVersion))
            packet.MaxQuantity = (int)GetCurrencyMaxQuantity(currency);

        if (currency.HasTotalEarned())
            packet.TotalEarned = (int)playerCurrency.EarnedQuantity;

        packet.SuppressChatLog = currency.IsSuppressingChatLog(gainSource == CurrencyGainSource.UpdatingVersion);
        packet.QuantityChange = amount;

        if (amount > 0)
            packet.QuantityGainSource = gainSource;
        else
            packet.QuantityLostSource = destroyReason;

        // TODO: FirstCraftOperationID, LastSpendTime & Toasts
        SendPacket(packet);
    }

    public bool ModifyMoney(long amount, bool sendError = true)
    {
        if (amount == 0)
            return true;

        ScriptManager.ForEach<IPlayerOnMoneyChanged>(p => p.OnMoneyChanged(this, amount));

        if (amount < 0)
            Money = (ulong)(Money > (ulong)-amount ? (long)Money + amount : 0);
        else
        {
            if (Money <= PlayerConst.MaxMoneyAmount - (ulong)amount)
                Money = Money + (ulong)amount;
            else
            {
                if (sendError)
                    SendEquipError(InventoryResult.TooMuchGold);

                return false;
            }
        }

        return true;
    }

    public void OnGossipSelect(WorldObject source, int gossipOptionId, uint menuId)
    {
        var gossipMenu = PlayerTalkClass.GossipMenu;

        // if not same, then something funky is going on
        if (menuId != gossipMenu.MenuId)
            return;

        var item = gossipMenu.GetItem(gossipOptionId);

        if (item == null)
            return;

        var gossipOptionNpc = item.OptionNpc;
        var guid = source.GUID;

        if (source.IsTypeId(TypeId.GameObject))
            if (gossipOptionNpc != GossipOptionNpc.None)
            {
                Log.Logger.Error("Player guid {0} request invalid gossip option for GameObject entry {1}", GUID.ToString(), source.Entry);

                return;
            }

        long cost = item.BoxMoney;

        if (!HasEnoughMoney(cost))
        {
            SendBuyError(BuyResult.NotEnoughtMoney, null, 0);
            PlayerTalkClass.SendCloseGossip();

            return;
        }

        if (item.ActionPoiId != 0)
            PlayerTalkClass.SendPointOfInterest(item.ActionPoiId);

        if (item.ActionMenuId != 0)
        {
            PrepareGossipMenu(source, item.ActionMenuId);
            SendPreparedGossip(source);
        }

        // types that have their dedicated open opcode dont send WorldPackets::NPC::GossipOptionNPCInteraction
        var handled = true;

        switch (gossipOptionNpc)
        {
            case GossipOptionNpc.Vendor:
                Session.SendListInventory(guid);

                break;

            case GossipOptionNpc.Taxinode:
                Session.SendTaxiMenu(source.AsCreature);

                break;

            case GossipOptionNpc.Trainer:
                Session.SendTrainerList(source.AsCreature, GameObjectManager.GetCreatureTrainerForGossipOption(source.Entry, menuId, item.OrderIndex));

                break;

            case GossipOptionNpc.SpiritHealer:
                source.SpellFactory.CastSpell(source.AsCreature, 17251, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCaster(GUID));
                handled = false;

                break;

            case GossipOptionNpc.PetitionVendor:
                PlayerTalkClass.SendCloseGossip();
                Session.SendPetitionShowList(guid);

                break;

            case GossipOptionNpc.Battlemaster:
            {
                var bgTypeId = BattlegroundManager.GetBattleMasterBG(source.Entry);

                if (bgTypeId == BattlegroundTypeId.None)
                {
                    Log.Logger.Error("a user (guid {0}) requested Battlegroundlist from a npc who is no battlemaster", GUID.ToString());

                    return;
                }

                BattlegroundManager.SendBattlegroundList(this, guid, bgTypeId);

                break;
            }
            case GossipOptionNpc.Auctioneer:
                Session.SendAuctionHello(guid, source.AsCreature);

                break;

            case GossipOptionNpc.TalentMaster:
                PlayerTalkClass.SendCloseGossip();
                SendRespecWipeConfirm(guid, Configuration.GetDefaultValue("NoResetTalentsCost", false) ? 0 : GetNextResetTalentsCost(), SpecResetType.Talents);

                break;

            case GossipOptionNpc.Stablemaster:
                Session.SendStablePet(guid);

                break;

            case GossipOptionNpc.PetSpecializationMaster:
                PlayerTalkClass.SendCloseGossip();
                SendRespecWipeConfirm(guid, Configuration.GetDefaultValue("NoResetTalentsCost", false) ? 0 : GetNextResetTalentsCost(), SpecResetType.PetTalents);

                break;

            case GossipOptionNpc.GuildBanker:
                var guild = Guild;

                if (guild != null)
                    guild.SendBankList(Session, 0, true);
                else
                    Guild.SendCommandResult(Session, GuildCommandType.ViewTab, GuildCommandError.PlayerNotInGuild);

                break;

            case GossipOptionNpc.Spellclick:
                var sourceUnit = source.AsUnit;

                sourceUnit?.HandleSpellClick(this);

                break;

            case GossipOptionNpc.DisableXPGain:
                PlayerTalkClass.SendCloseGossip();
                SpellFactory.CastSpell(null, PlayerConst.SpellExperienceEliminated, true);
                SetPlayerFlag(PlayerFlags.NoXPGain);

                break;

            case GossipOptionNpc.EnableXPGain:
                PlayerTalkClass.SendCloseGossip();
                RemoveAura(PlayerConst.SpellExperienceEliminated);
                RemovePlayerFlag(PlayerFlags.NoXPGain);

                break;

            case GossipOptionNpc.SpecializationMaster:
                PlayerTalkClass.SendCloseGossip();
                SendRespecWipeConfirm(guid, 0, SpecResetType.Specialization);

                break;

            case GossipOptionNpc.GlyphMaster:
                PlayerTalkClass.SendCloseGossip();
                SendRespecWipeConfirm(guid, 0, SpecResetType.Glyphs);

                break;

            case GossipOptionNpc.GarrisonTradeskillNpc: // NYI
                break;

            case GossipOptionNpc.GarrisonRecruitment: // NYI
                break;

            case GossipOptionNpc.ChromieTimeNpc: // NYI
                break;

            case GossipOptionNpc.RuneforgeLegendaryCrafting: // NYI
                break;

            case GossipOptionNpc.RuneforgeLegendaryUpgrade: // NYI
                break;

            case GossipOptionNpc.ProfessionsCraftingOrder: // NYI
                break;

            case GossipOptionNpc.ProfessionsCustomerOrder: // NYI
                break;

            case GossipOptionNpc.BarbersChoice: // NYI - unknown if needs sending
            default:
                handled = false;

                break;
        }

        if (!handled)
        {
            if (item.GossipNpcOptionId.HasValue)
            {
                var addon = GameObjectManager.GetGossipMenuAddon(menuId);

                GossipOptionNPCInteraction npcInteraction = new()
                {
                    GossipGUID = source.GUID,
                    GossipNpcOptionID = item.GossipNpcOptionId.Value
                };

                if (addon != null && addon.FriendshipFactionId != 0)
                    npcInteraction.FriendshipFactionID = addon.FriendshipFactionId;

                SendPacket(npcInteraction);
            }
            else
            {
                PlayerInteractionType[] gossipOptionNpcToInteractionType =
                {
                    PlayerInteractionType.None, PlayerInteractionType.Vendor, PlayerInteractionType.TaxiNode, PlayerInteractionType.Trainer, PlayerInteractionType.SpiritHealer, PlayerInteractionType.Binder, PlayerInteractionType.Banker, PlayerInteractionType.PetitionVendor, PlayerInteractionType.TabardVendor, PlayerInteractionType.BattleMaster, PlayerInteractionType.Auctioneer, PlayerInteractionType.TalentMaster, PlayerInteractionType.StableMaster, PlayerInteractionType.None, PlayerInteractionType.GuildBanker, PlayerInteractionType.None, PlayerInteractionType.None, PlayerInteractionType.None, PlayerInteractionType.MailInfo, PlayerInteractionType.None, PlayerInteractionType.LFGDungeon, PlayerInteractionType.ArtifactForge, PlayerInteractionType.None, PlayerInteractionType.SpecializationMaster, PlayerInteractionType.None, PlayerInteractionType.None, PlayerInteractionType.GarrArchitect, PlayerInteractionType.GarrMission, PlayerInteractionType.ShipmentCrafter, PlayerInteractionType.GarrTradeskill, PlayerInteractionType.GarrRecruitment, PlayerInteractionType.AdventureMap, PlayerInteractionType.GarrTalent, PlayerInteractionType.ContributionCollector, PlayerInteractionType.Transmogrifier, PlayerInteractionType.AzeriteRespec, PlayerInteractionType.IslandQueue, PlayerInteractionType.ItemInteraction, PlayerInteractionType.WorldMap, PlayerInteractionType.Soulbind, PlayerInteractionType.ChromieTime, PlayerInteractionType.CovenantPreview, PlayerInteractionType.LegendaryCrafting, PlayerInteractionType.NewPlayerGuide, PlayerInteractionType.LegendaryCrafting, PlayerInteractionType.Renown, PlayerInteractionType.BlackMarketAuctioneer, PlayerInteractionType.PerksProgramVendor, PlayerInteractionType.ProfessionsCraftingOrder, PlayerInteractionType.Professions, PlayerInteractionType.ProfessionsCustomerOrder, PlayerInteractionType.TraitSystem, PlayerInteractionType.BarbersChoice, PlayerInteractionType.MajorFactionRenown
                };

                var interactionType = gossipOptionNpcToInteractionType[(int)gossipOptionNpc];

                if (interactionType != PlayerInteractionType.None)
                {
                    NPCInteractionOpenResult npcInteraction = new()
                    {
                        Npc = source.GUID,
                        InteractionType = interactionType,
                        Success = true
                    };

                    SendPacket(npcInteraction);
                }
            }
        }

        ModifyMoney(-cost);
    }

    public override void OnPhaseChange()
    {
        base.OnPhaseChange();

        Location.Map.UpdatePersonalPhasesForPlayer(this);
    }

    public void PossessSpellInitialize()
    {
        var charm = Charmed;

        if (charm == null)
            return;

        var charmInfo = charm.GetCharmInfo();

        if (charmInfo == null)
        {
            Log.Logger.Error("Player:PossessSpellInitialize(): charm ({0}) has no charminfo!", charm.GUID);

            return;
        }

        PetSpells petSpellsPacket = new()
        {
            PetGUID = charm.GUID
        };

        for (byte i = 0; i < SharedConst.ActionBarIndexMax; ++i)
            petSpellsPacket.ActionButtons[i] = charmInfo.GetActionBarEntry(i).PackedData;

        // Cooldowns
        charm.SpellHistory.WritePacket(petSpellsPacket);

        SendPacket(petSpellsPacket);
    }

    //Chat - Text - Channel
    public void PrepareGossipMenu(WorldObject source, uint menuId, bool showQuests = false)
    {
        var menu = PlayerTalkClass;
        menu.ClearMenus();

        menu.GossipMenu.MenuId = menuId;

        var menuItemBounds = GameObjectManager.GetGossipMenuItemsMapBounds(menuId);

        if (source.IsTypeId(TypeId.Unit))
        {
            if (showQuests && source.AsUnit.IsQuestGiver)
                PrepareQuestMenu(source.GUID);
        }
        else if (source.IsTypeId(TypeId.GameObject))
            if (source.AsGameObject.GoType == GameObjectTypes.QuestGiver)
                PrepareQuestMenu(source.GUID);

        foreach (var gossipMenuItem in menuItemBounds)
        {
            if (!ConditionManager.IsObjectMeetToConditions(this, source, gossipMenuItem.Conditions))
                continue;

            var canTalk = true;
            var go = source.AsGameObject;
            var creature = source.AsCreature;

            if (creature != null)
                switch (gossipMenuItem.OptionNpc)
                {
                    case GossipOptionNpc.Taxinode:
                        if (Session.SendLearnNewTaxiNode(creature))
                            return;

                        break;

                    case GossipOptionNpc.SpiritHealer:
                        if (!IsDead)
                            canTalk = false;

                        break;

                    case GossipOptionNpc.Battlemaster:
                        if (!creature.CanInteractWithBattleMaster(this, false))
                            canTalk = false;

                        break;

                    case GossipOptionNpc.TalentMaster:
                    case GossipOptionNpc.SpecializationMaster:
                    case GossipOptionNpc.GlyphMaster:
                        if (!creature.CanResetTalents(this))
                            canTalk = false;

                        break;

                    case GossipOptionNpc.Stablemaster:
                    case GossipOptionNpc.PetSpecializationMaster:
                        if (Class != PlayerClass.Hunter)
                            canTalk = false;

                        break;

                    case GossipOptionNpc.DisableXPGain:
                        if (HasPlayerFlag(PlayerFlags.NoXPGain) || IsMaxLevel)
                            canTalk = false;

                        break;

                    case GossipOptionNpc.EnableXPGain:
                        if (!HasPlayerFlag(PlayerFlags.NoXPGain) || IsMaxLevel)
                            canTalk = false;

                        break;

                    case GossipOptionNpc.None:
                    case GossipOptionNpc.Vendor:
                    case GossipOptionNpc.Trainer:
                    case GossipOptionNpc.Binder:
                    case GossipOptionNpc.Banker:
                    case GossipOptionNpc.PetitionVendor:
                    case GossipOptionNpc.TabardVendor:
                    case GossipOptionNpc.Auctioneer:
                    case GossipOptionNpc.Mailbox:
                    case GossipOptionNpc.Transmogrify:
                    case GossipOptionNpc.AzeriteRespec:
                        break; // No checks
                    case GossipOptionNpc.CemeterySelect:
                        canTalk = false; // Deprecated

                        break;

                    default:
                        if (gossipMenuItem.OptionNpc >= GossipOptionNpc.Max)
                        {
                            Log.Logger.Error($"Creature entry {creature.Entry} has an unknown gossip option icon {gossipMenuItem.OptionNpc} for menu {gossipMenuItem.MenuId}.");
                            canTalk = false;
                        }

                        break;
                }
            else if (go != null)
                switch (gossipMenuItem.OptionNpc)
                {
                    case GossipOptionNpc.None:
                        if (go.GoType != GameObjectTypes.QuestGiver && go.GoType != GameObjectTypes.Goober)
                            canTalk = false;

                        break;

                    default:
                        canTalk = false;

                        break;
                }

            if (canTalk)
                menu.GossipMenu.AddMenuItem(gossipMenuItem, gossipMenuItem.MenuId, gossipMenuItem.OrderIndex);
        }
    }

    public void ProcessDelayedOperations()
    {
        if (_delayedOperations == 0)
            return;

        if (_delayedOperations.HasAnyFlag(PlayerDelayedOperations.ResurrectPlayer))
            ResurrectUsingRequestDataImpl();

        if (_delayedOperations.HasAnyFlag(PlayerDelayedOperations.SavePlayer))
            SaveToDB();

        if (_delayedOperations.HasAnyFlag(PlayerDelayedOperations.SpellCastDeserter))
            SpellFactory.CastSpell(this, 26013, true); // Deserter

        if (_delayedOperations.HasAnyFlag(PlayerDelayedOperations.BGMountRestore))
            if (_bgData.MountSpell != 0)
            {
                SpellFactory.CastSpell(this, _bgData.MountSpell, true);
                _bgData.MountSpell = 0;
            }

        if (_delayedOperations.HasAnyFlag(PlayerDelayedOperations.BGTaxiRestore))
            if (_bgData.HasTaxiPath)
            {
                Taxi.AddTaxiDestination(_bgData.TaxiPath[0]);
                Taxi.AddTaxiDestination(_bgData.TaxiPath[1]);
                _bgData.ClearTaxiPath();

                ContinueTaxiFlight();
            }

        if (_delayedOperations.HasAnyFlag(PlayerDelayedOperations.BGGroupRestore))
        {
            var g = Group;

            g?.SendUpdateToPlayer(GUID);
        }

        //we have executed ALL delayed ops, so clear the Id
        _delayedOperations = 0;
    }

    public void RecallLocation()
    {
        TeleportTo(Recall, 0, _recallInstanceId);
    }

    public void RemoveActionButton(byte button)
    {
        var actionButton = _actionButtons.LookupByKey(button);

        if (actionButton == null || actionButton.UState == ActionButtonUpdateState.Deleted)
            return;

        if (actionButton.UState == ActionButtonUpdateState.New)
            _actionButtons.Remove(button); // new and not saved
        else
            actionButton.UState = ActionButtonUpdateState.Deleted; // saved, will deleted at next save

        Log.Logger.Debug("Action Button '{0}' Removed from Player '{1}'", actionButton, GUID.ToString());
    }

    public void RemoveAtLoginFlag(AtLoginFlags flags, bool persist = false)
    {
        LoginFlags &= ~flags;

        if (!persist)
            return;

        var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_REM_AT_LOGIN_FLAG);
        stmt.AddValue(0, (ushort)flags);
        stmt.AddValue(1, GUID.Counter);

        CharacterDatabase.Execute(stmt);
    }

    public void RemoveAuraVision(PlayerFieldByte2Flags flags)
    {
        RemoveUpdateFieldFlagValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.AuraVision), (byte)flags);
    }

    public void RemoveConditionalTransmog(uint itemModifiedAppearanceId)
    {
        var index = ActivePlayerData.ConditionalTransmog.FindIndex(itemModifiedAppearanceId);

        if (index >= 0)
            RemoveDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ConditionalTransmog), index);
    }

    public void RemoveCurrency(uint id, int amount, CurrencyDestroyReason destroyReason = CurrencyDestroyReason.Cheat)
    {
        ModifyCurrency(id, -amount, default, destroyReason);
    }

    public void RemoveFromWhisperWhiteList(ObjectGuid guid)
    {
        _whisperList.Remove(guid);
    }

    public override void RemoveFromWorld()
    {
        // cleanup
        if (Location.IsInWorld)
        {
            // Release charmed creatures, unsummon totems and remove pets/guardians
            StopCastingCharm();
            StopCastingBindSight();
            UnsummonPetTemporaryIfAny();
            ClearComboPoints();
            Session.DoLootReleaseAll();
            _lootRolls.Clear();
            OutdoorPvPManager.HandlePlayerLeaveZone(this, _zoneUpdateId);
            BattleFieldManager.HandlePlayerLeaveZone(this, _zoneUpdateId);
        }

        // Remove items from world before self - player must be found in Item.RemoveFromObjectUpdate
        for (byte i = (int)PlayerSlots.Start; i < (int)PlayerSlots.End; ++i)
            if (_items[i] != null)
                _items[i].RemoveFromWorld();

        // Do not add/remove the player from the object storage
        // It will crash when updating the ObjectAccessor
        // The player should only be removed when logging out
        base.RemoveFromWorld();

        var viewpoint = Viewpoint;

        if (viewpoint != null)
        {
            Log.Logger.Error("Player {0} has viewpoint {1} {2} when removed from world",
                             GetName(),
                             viewpoint.Entry,
                             viewpoint.TypeId);

            SetViewpoint(viewpoint, false);
        }

        RemovePlayerLocalFlag(PlayerLocalFlags.OverrideTransportServerTime);
        SetTransportServerTime(0);
    }

    public void RemoveMail(ulong id)
    {
        foreach (var mail in Mails.Where(mail => mail.MessageID == id))
        {
            //do not delete item, because Player.removeMail() is called when returning mail to sender.
            Mails.Remove(mail);

            return;
        }
    }

    public bool RemoveMItem(ulong id)
    {
        return _mailItems.Remove(id);
    }

    public void RemovePet(Pet pet, PetSaveMode mode, bool returnreagent = false)
    {
        pet ??= CurrentPet;

        if (pet != null)
        {
            Log.Logger.Debug("RemovePet {0}, {1}, {2}", pet.Entry, mode, returnreagent);

            if (pet.Removed)
                return;
        }

        if (returnreagent && (pet != null || TemporaryUnsummonedPetNumber != 0) && !InBattleground)
        {
            //returning of reagents only for players, so best done here
            var spellId = pet != null ? pet.UnitData.CreatedBySpell : _oldpetspell;
            var spellInfo = SpellManager.GetSpellInfo(spellId, Location.Map.DifficultyID);

            if (spellInfo != null)
                for (uint i = 0; i < SpellConst.MaxReagents; ++i)
                    if (spellInfo.Reagent[i] > 0)
                    {
                        List<ItemPosCount> dest = new(); //for succubus, voidwalker, felhunter and felguard credit soulshard when despawn reason other than death (out of range, logout)
                        var msg = CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, (uint)spellInfo.Reagent[i], spellInfo.ReagentCount[i]);

                        if (msg != InventoryResult.Ok)
                            continue;

                        var item = StoreNewItem(dest, (uint)spellInfo.Reagent[i], true);

                        if (Location.IsInWorld)
                            SendNewItem(item, spellInfo.ReagentCount[i], true, false);
                    }

            TemporaryUnsummonedPetNumber = 0;
        }

        if (pet == null)
        {
            // Handle removing pet while it is in "temporarily unsummoned" state, for example on mount
            if (mode == PetSaveMode.NotInSlot && PetStable is { CurrentPetIndex: { } })
                PetStable.CurrentPetIndex = null;

            return;
        }

        pet.CombatStop();

        // only if current pet in slot
        pet.SavePetToDB(mode);

        PetStable.GetCurrentPet();

        if (mode is PetSaveMode.NotInSlot or PetSaveMode.AsDeleted)
            PetStable.CurrentPetIndex = null;
        // else if (stable slots) handled in opcode handlers due to required swaps
        // else (current pet) doesnt need to do anything

        SetMinion(pet, false);

        pet.Location.AddObjectToRemoveList();
        pet.Removed = true;

        if (!pet.IsControlled)
            return;

        SendPacket(new PetSpells());

        if (Group != null)
            SetGroupUpdateFlag(GroupUpdateFlags.Pet);
    }

    public void RemovePetAura(PetAura petSpell)
    {
        PetAuras.Remove(petSpell);

        var pet = CurrentPet;

        pet?.RemoveAura(petSpell.GetAura(pet.Entry));
    }

    public void RemovePlayerFlag(PlayerFlags flags)
    {
        RemoveUpdateFieldFlagValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.PlayerFlags), (uint)flags);
    }

    public void RemovePlayerFlagEx(PlayerFlagsEx flags)
    {
        RemoveUpdateFieldFlagValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.PlayerFlagsEx), (uint)flags);
    }

    public void RemovePlayerLocalFlag(PlayerLocalFlags flags)
    {
        RemoveUpdateFieldFlagValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.LocalFlags), (uint)flags);
    }

    public void RemoveSelfResSpell(uint spellId)
    {
        var index = ActivePlayerData.SelfResSpells.FindIndex(spellId);

        if (index >= 0)
            RemoveDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.SelfResSpells), index);
    }

    public void RemoveSocial()
    {
        SocialManager.RemovePlayerSocial(GUID);
        Social = null;
    }

    public void RemoveTrackCreatureFlag(uint flags)
    {
        RemoveUpdateFieldFlagValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.TrackCreatureMask), flags);
    }

    public void ReplaceAllPlayerFlags(PlayerFlags flags)
    {
        SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.PlayerFlags), (uint)flags);
    }

    public void ReplaceAllPlayerFlagsEx(PlayerFlagsEx flags)
    {
        SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.PlayerFlagsEx), (uint)flags);
    }

    public void ReplaceAllPlayerLocalFlags(PlayerLocalFlags flags)
    {
        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.LocalFlags), (uint)flags);
    }

    public void RepopAtGraveyard()
    {
        // note: this can be called also when the player is alive
        // for example from WorldSession.HandleMovementOpcodes

        var zone = CliDB.AreaTableStorage.LookupByKey(Location.Area);

        var shouldResurrect = false;

        // Such zones are considered unreachable as a ghost and the player must be automatically revived
        if ((!IsAlive && zone != null && zone.HasFlag(AreaFlags.NeedFly)) || Transport != null || Location.Z < Location.Map.GetMinHeight(Location.PhaseShift, Location.X, Location.Y))
        {
            shouldResurrect = true;
            SpawnCorpseBones();
        }

        WorldSafeLocsEntry closestGrave;

        // Special handle for Battlegroundmaps
        var bg = Battleground;

        if (bg != null)
            closestGrave = bg.GetClosestGraveYard(this);
        else
        {
            var bf = BattleFieldManager.GetBattlefieldToZoneId(Location.Map, Location.Zone);

            closestGrave = bf != null ? bf.GetClosestGraveYard(this) : GameObjectManager.GetClosestGraveYard(Location, Team, this);
        }

        // stop countdown until repop
        DeathTimer = 0;

        // if no grave found, stay at the current location
        // and don't show spirit healer location
        if (closestGrave != null)
        {
            TeleportTo(closestGrave.Location, shouldResurrect ? TeleportToOptions.ReviveAtTeleport : 0);

            if (IsDead) // not send if alive, because it used in TeleportTo()
            {
                DeathReleaseLoc packet = new()
                {
                    MapID = (int)closestGrave.Location.MapId,
                    Loc = closestGrave.Location
                };

                SendPacket(packet);
            }
        }
        else if (Location.Z < Location.Map.GetMinHeight(Location.PhaseShift, Location.X, Location.Y))
            TeleportTo(Homebind);

        RemovePlayerFlag(PlayerFlags.IsOutOfBounds);
    }

    public void ResetAllPowers()
    {
        SetFullHealth();

        switch (DisplayPowerType)
        {
            case PowerType.Mana:
                SetFullPower(PowerType.Mana);

                break;

            case PowerType.Rage:
                SetPower(PowerType.Rage, 0);

                break;

            case PowerType.Energy:
                SetFullPower(PowerType.Energy);

                break;

            case PowerType.RunicPower:
                SetPower(PowerType.RunicPower, 0);

                break;

            case PowerType.LunarPower:
                SetPower(PowerType.LunarPower, 0);

                break;
        }
    }

    public void ResummonPetTemporaryUnSummonedIfAny()
    {
        if (TemporaryUnsummonedPetNumber == 0)
            return;

        // not resummon in not appropriate state
        if (IsPetNeedBeTemporaryUnsummoned())
            return;

        if (!PetGUID.IsEmpty)
            return;

        Pet newPet = new(this);
        newPet.LoadPetFromDB(this, 0, TemporaryUnsummonedPetNumber, true);

        TemporaryUnsummonedPetNumber = 0;
    }

    public void ResurrectPlayer(float restorePercent, bool applySickness = false)
    {
        DeathReleaseLoc packet = new()
        {
            MapID = -1
        };

        SendPacket(packet);

        // speed change, land walk

        // remove death Id + set aura
        RemovePlayerFlag(PlayerFlags.IsOutOfBounds);

        // This must be called always even on Players with race != RACE_NIGHTELF in case of faction change
        RemoveAura(20584); // speed bonuses
        RemoveAura(8326);  // SPELL_AURA_GHOST

        if (Session.IsARecruiter || Session.RecruiterId != 0)
            SetDynamicFlag(UnitDynFlags.ReferAFriend);

        SetDeathState(DeathState.Alive);

        // add the Id to make sure opcode is always sent
        MovementInfo.AddMovementFlag(MovementFlag.WaterWalk);
        SetWaterWalking(false);

        if (!HasUnitState(UnitState.Stunned))
            SetRooted(false);

        DeathTimer = 0;

        // set health/powers (0- will be set in caller)
        if (restorePercent > 0.0f)
        {
            SetHealth(MaxHealth * restorePercent);
            SetPower(PowerType.Mana, (int)(GetMaxPower(PowerType.Mana) * restorePercent));
            SetPower(PowerType.Rage, 0);
            SetPower(PowerType.Energy, (int)(GetMaxPower(PowerType.Energy) * restorePercent));
            SetPower(PowerType.Focus, (int)(GetMaxPower(PowerType.Focus) * restorePercent));
            SetPower(PowerType.LunarPower, 0);
        }

        // trigger update zone for alive state zone updates
        UpdateZone(Location.Zone, Location.Area);
        OutdoorPvPManager.HandlePlayerResurrects(this, Location.Zone);

        if (InBattleground)
            Battleground?.HandlePlayerResurrect(this);

        // update visibility
        UpdateObjectVisibility();

        // recast lost by death auras of any items held in the inventory
        CastAllObtainSpells();

        if (!applySickness)
            return;

        //Characters from level 1-10 are not affected by resurrection sickness.
        //Characters from level 11-19 will suffer from one minute of sickness
        //for each level they are above 10.
        //Characters level 20 and up suffer from ten minutes of sickness.
        var startLevel = Configuration.GetDefaultValue("Death:SicknessLevel", 11);
        var raceEntry = CliDB.ChrRacesStorage.LookupByKey(Race);

        if (Level < startLevel)
            return;

        // set resurrection sickness
        SpellFactory.CastSpell(this, raceEntry.ResSicknessSpellID, true);

        // not full duration
        if (Level >= startLevel + 9)
            return;

        var delta = (int)(Level - startLevel + 1) * Time.MINUTE;
        var aur = GetAura(raceEntry.ResSicknessSpellID, GUID);

        aur?.SetDuration(delta * Time.IN_MILLISECONDS);
    }

    public void ResurrectUsingRequestData()
    {
        // Teleport before resurrecting by player, otherwise the player might get attacked from creatures near his corpse
        TeleportTo(_resurrectionData.Location);

        if (IsBeingTeleported)
        {
            ScheduleDelayedOperation(PlayerDelayedOperations.ResurrectPlayer);

            return;
        }

        ResurrectUsingRequestDataImpl();
    }

    // Calculates how many reputation points player gains in victim's enemy factions
    public void RewardReputation(Unit victim, float rate)
    {
        if (victim == null || victim.IsTypeId(TypeId.Player))
            return;

        if (victim.AsCreature.IsReputationGainDisabled)
            return;

        var rep = GameObjectManager.GetReputationOnKilEntry(victim.AsCreature.Template.Entry);

        if (rep == null)
            return;

        uint championingFaction = 0;

        if (GetChampioningFaction() != 0)
        {
            // support for: Championing - http://www.wowwiki.com/Championing
            var map = Location.Map;

            if (map.IsNonRaidDungeon)
            {
                var dungeon = DB2Manager.GetLfgDungeon(map.Id, map.DifficultyID);

                if (dungeon != null)
                {
                    var dungeonLevels = DB2Manager.GetContentTuningData(dungeon.ContentTuningID, PlayerData.CtrOptions.Value.ContentTuningConditionMask);

                    if (dungeonLevels.HasValue)
                        if (dungeonLevels.Value.TargetLevelMax == GameObjectManager.GetMaxLevelForExpansion(Expansion.WrathOfTheLichKing))
                            championingFaction = GetChampioningFaction();
                }
            }
        }

        var team = Team;

        if (rep.RepFaction1 != 0 && (!rep.TeamDependent || team == TeamFaction.Alliance))
        {
            var donerep1 = CalculateReputationGain(ReputationSource.Kill, victim.GetLevelForTarget(this), rep.RepValue1, (int)(championingFaction != 0 ? championingFaction : rep.RepFaction1));
            donerep1 = (int)(donerep1 * rate);

            var factionEntry1 = CliDB.FactionStorage.LookupByKey(championingFaction != 0 ? championingFaction : rep.RepFaction1);
            var currentReputationRank1 = ReputationMgr.GetRank(factionEntry1);

            if (factionEntry1 != null)
                ReputationMgr.ModifyReputation(factionEntry1, donerep1, (uint)currentReputationRank1 > rep.ReputationMaxCap1);
        }

        if (rep.RepFaction2 == 0 || (rep.TeamDependent && team != TeamFaction.Horde))
            return;

        var donerep2 = CalculateReputationGain(ReputationSource.Kill, victim.GetLevelForTarget(this), rep.RepValue2, (int)(championingFaction != 0 ? championingFaction : rep.RepFaction2));
        donerep2 = (int)(donerep2 * rate);

        var factionEntry2 = CliDB.FactionStorage.LookupByKey(championingFaction != 0 ? championingFaction : rep.RepFaction2);
        var currentReputationRank2 = ReputationMgr.GetRank(factionEntry2);

        if (factionEntry2 != null)
            ReputationMgr.ModifyReputation(factionEntry2, donerep2, (uint)currentReputationRank2 > rep.ReputationMaxCap2);
    }

    //Action Buttons - CUF Profile
    public void SaveCufProfile(byte id, CufProfile profile)
    {
        _cufProfiles[id] = profile;
    }

    public void SaveRecallPosition()
    {
        Recall = new WorldLocation(Location);

        if (Location.Map != null)
            _recallInstanceId = Location.Map.InstanceId;
    }

    public void SendAttackSwingCancelAttack()
    {
        SendPacket(new CancelCombat());
    }

    public void SendAttackSwingNotInRange()
    {
        SendPacket(new AttackSwingError(AttackSwingErr.NotInRange));
    }

    public void SendAutoRepeatCancel(Unit target)
    {
        CancelAutoRepeat cancelAutoRepeat = new()
        {
            Guid = target.GUID // may be it's target guid
        };

        SendMessageToSet(cancelAutoRepeat, true);
    }

    public void SendBindPointUpdate()
    {
        BindPointUpdate packet = new()
        {
            BindPosition = new Vector3(Homebind.X, Homebind.Y, Homebind.Z),
            BindMapID = Homebind.MapId,
            BindAreaID = _homebindAreaId
        };

        SendPacket(packet);
    }

    public void SendCinematicStart(uint cinematicSequenceId)
    {
        TriggerCinematic packet = new()
        {
            CinematicID = cinematicSequenceId
        };

        SendPacket(packet);

        if (CliDB.CinematicSequencesStorage.TryGetValue(cinematicSequenceId, out var sequence))
            CinematicMgr.BeginCinematic(sequence);
    }

    // This fuction Sends the current menu to show to client, a - NPCTEXTID(uint32), b - npc guid(uint64)
    public void SendGossipMenu(uint titleId, ObjectGuid objGUID)
    {
        PlayerTalkClass.SendGossipMenu(titleId, objGUID);
    }

    public void SendInitialPacketsAfterAddToMap()
    {
        UpdateVisibilityForPlayer();

        // update zone
        UpdateZone(Location.Zone, Location.Area); // also call SendInitWorldStates();

        Session.SendLoadCUFProfiles();

        SpellFactory.CastSpell(this, 836, true); // LOGINEFFECT

        // set some aura effects that send packet to player client after add player to map
        // SendMessageToSet not send it to player not it map, only for aura that not changed anything at re-apply
        // same auras state lost at far teleport, send it one more time in this case also
        AuraType[] auratypes =
        {
            AuraType.ModFear, AuraType.Transform, AuraType.WaterWalk, AuraType.FeatherFall, AuraType.Hover, AuraType.SafeFall, AuraType.Fly, AuraType.ModIncreaseMountedFlightSpeed, AuraType.None
        };

        foreach (var aura in auratypes)
        {
            var auraList = GetAuraEffectsByType(aura);

            if (!auraList.Empty())
                auraList.First().HandleEffect(this, AuraEffectHandleModes.SendForClient, true);
        }

        if (HasAuraType(AuraType.ModStun) || HasAuraType(AuraType.ModStunDisableGravity))
            SetRooted(true);

        MoveSetCompoundState setCompoundState = new();

        // manual send package (have code in HandleEffect(this, AURA_EFFECT_HANDLE_SEND_FOR_CLIENT, true); that must not be re-applied.
        if (HasAuraType(AuraType.ModRoot) || HasAuraType(AuraType.ModRoot2) || HasAuraType(AuraType.ModRootDisableGravity))
            setCompoundState.StateChanges.Add(new MoveSetCompoundState.MoveStateChange(ServerOpcodes.MoveRoot, MovementCounter++));

        if (HasAuraType(AuraType.FeatherFall))
            setCompoundState.StateChanges.Add(new MoveSetCompoundState.MoveStateChange(ServerOpcodes.MoveSetFeatherFall, MovementCounter++));

        if (HasAuraType(AuraType.WaterWalk))
            setCompoundState.StateChanges.Add(new MoveSetCompoundState.MoveStateChange(ServerOpcodes.MoveSetWaterWalk, MovementCounter++));

        if (HasAuraType(AuraType.Hover))
            setCompoundState.StateChanges.Add(new MoveSetCompoundState.MoveStateChange(ServerOpcodes.MoveSetHovering, MovementCounter++));

        if (HasAuraType(AuraType.ModRootDisableGravity) || HasAuraType(AuraType.ModStunDisableGravity))
            setCompoundState.StateChanges.Add(new MoveSetCompoundState.MoveStateChange(ServerOpcodes.MoveDisableGravity, MovementCounter++));

        if (HasAuraType(AuraType.CanTurnWhileFalling))
            setCompoundState.StateChanges.Add(new MoveSetCompoundState.MoveStateChange(ServerOpcodes.MoveSetCanTurnWhileFalling, MovementCounter++));

        if (HasAura(196055)) //DH DoubleJump
            setCompoundState.StateChanges.Add(new MoveSetCompoundState.MoveStateChange(ServerOpcodes.MoveEnableDoubleJump, MovementCounter++));

        if (HasAuraType(AuraType.IgnoreMovementForces))
            setCompoundState.StateChanges.Add(new MoveSetCompoundState.MoveStateChange(ServerOpcodes.MoveSetIgnoreMovementForces, MovementCounter++));

        if (HasAuraType(AuraType.DisableInertia))
            setCompoundState.StateChanges.Add(new MoveSetCompoundState.MoveStateChange(ServerOpcodes.MoveDisableInertia, MovementCounter++));

        if (!setCompoundState.StateChanges.Empty())
        {
            setCompoundState.MoverGUID = GUID;
            SendPacket(setCompoundState);
        }

        SendAurasForTarget(this);
        SendEnchantmentDurations(); // must be after add to map
        SendItemDurations();        // must be after add to map

        // raid downscaling - send difficulty to player
        if (Location.Map.IsRaid)
        {
            var mapDifficulty = Location.Map.DifficultyID;
            var difficulty = CliDB.DifficultyStorage.LookupByKey(mapDifficulty);
            SendRaidDifficulty((difficulty.Flags & DifficultyFlags.Legacy) != 0, (int)mapDifficulty);
        }
        else if (Location.Map.IsNonRaidDungeon)
            SendDungeonDifficulty((int)Location.Map.DifficultyID);

        PhasingHandler.OnMapChange(this);

        Garrison?.SendRemoteInfo();

        UpdateItemLevelAreaBasedScaling();

        if (!GetPlayerSharingQuest().IsEmpty)
        {
            var quest = GameObjectManager.GetQuestTemplate(GetSharedQuestID());

            if (quest != null)
                PlayerTalkClass.SendQuestGiverQuestDetails(quest, GUID, true, false);
            else
                ClearQuestSharingInfo();
        }

        SceneMgr.TriggerDelayedScenes();
    }

    public void SendInitialPacketsBeforeAddToMap()
    {
        if (!TeleportOptions.HasAnyFlag(TeleportToOptions.Seamless))
        {
            MovementCounter = 0;
            Session.ResetTimeSync();
        }

        Session.SendTimeSync();

        Social.SendSocialList(this, SocialFlag.All);

        // SMSG_BINDPOINTUPDATE
        SendBindPointUpdate();

        // SMSG_SET_PROFICIENCY
        // SMSG_SET_PCT_SPELL_MODIFIER
        // SMSG_SET_FLAT_SPELL_MODIFIER

        // SMSG_TALENTS_INFO
        SendTalentsInfoData();

        // SMSG_INITIAL_SPELLS
        SendKnownSpells();

        // SMSG_SEND_UNLEARN_SPELLS
        SendUnlearnSpells();

        // SMSG_SEND_SPELL_HISTORY
        SendSpellHistory sendSpellHistory = new();
        SpellHistory.WritePacket(sendSpellHistory);
        SendPacket(sendSpellHistory);

        // SMSG_SEND_SPELL_CHARGES
        SendSpellCharges sendSpellCharges = new();
        SpellHistory.WritePacket(sendSpellCharges);
        SendPacket(sendSpellCharges);

        ActiveGlyphs activeGlyphs = new();

        foreach (var glyphId in GetGlyphs(GetActiveTalentGroup()))
        {
            var bindableSpells = DB2Manager.GetGlyphBindableSpells(glyphId);

            foreach (var bindableSpell in bindableSpells)
                if (HasSpell(bindableSpell) && !_overrideSpells.ContainsKey(bindableSpell))
                    activeGlyphs.Glyphs.Add(new GlyphBinding(bindableSpell, (ushort)glyphId));
        }

        activeGlyphs.IsFullUpdate = true;
        SendPacket(activeGlyphs);

        // SMSG_ACTION_BUTTONS
        SendInitialActionButtons();

        // SMSG_INITIALIZE_FACTIONS
        ReputationMgr.SendInitialReputations();

        // SMSG_SETUP_CURRENCY
        SendCurrencies();

        // SMSG_EQUIPMENT_SET_LIST
        SendEquipmentSetList();

        _achievementSys.SendAllData(this);
        _questObjectiveCriteriaManager.SendAllData(this);

        // SMSG_LOGIN_SETTIMESPEED
        var timeSpeed = 0.01666667f;

        LoginSetTimeSpeed loginSetTimeSpeed = new()
        {
            NewSpeed = timeSpeed,
            GameTime = (uint)GameTime.CurrentTime,
            ServerTime = (uint)GameTime.CurrentTime,
            GameTimeHolidayOffset = 0,  // @todo
            ServerTimeHolidayOffset = 0 // @todo
        };

        SendPacket(loginSetTimeSpeed);

        // SMSG_WORLD_SERVER_INFO
        WorldServerInfo worldServerInfo = new()
        {
            InstanceGroupSize = Location.Map.MapDifficulty.MaxPlayers, // @todo
            IsTournamentRealm = false,                                 // @todo
            RestrictedAccountMaxLevel = null,                          // @todo
            RestrictedAccountMaxMoney = null,                          // @todo
            DifficultyID = (uint)Location.Map.DifficultyID
        };

        //worldServerInfo.XRealmPvpAlert; // @todo
        SendPacket(worldServerInfo);

        // Spell modifiers
        SendSpellModifiers();

        // SMSG_ACCOUNT_MOUNT_UPDATE
        AccountMountUpdate mountUpdate = new()
        {
            IsFullUpdate = true,
            Mounts = Session.CollectionMgr.AccountMounts
        };

        SendPacket(mountUpdate);

        // SMSG_ACCOUNT_TOYS_UPDATE
        AccountToyUpdate toyUpdate = new()
        {
            IsFullUpdate = true,
            Toys = Session.CollectionMgr.AccountToys
        };

        SendPacket(toyUpdate);

        // SMSG_ACCOUNT_HEIRLOOM_UPDATE
        AccountHeirloomUpdate heirloomUpdate = new()
        {
            IsFullUpdate = true,
            Heirlooms = Session.CollectionMgr.AccountHeirlooms
        };

        SendPacket(heirloomUpdate);

        Session.CollectionMgr.SendFavoriteAppearances();

        InitialSetup initialSetup = new()
        {
            ServerExpansionLevel = Configuration.GetDefaultValue("Expansion", (byte)Expansion.Dragonflight)
        };

        SendPacket(initialSetup);

        SetMovedUnit(this);
    }

    public void SendMailResult(ulong mailId, MailResponseType mailAction, MailResponseResult mailError, InventoryResult equipError = 0, ulong itemGuid = 0, uint itemCount = 0)
    {
        MailCommandResult result = new()
        {
            MailID = mailId,
            Command = (int)mailAction,
            ErrorCode = (int)mailError
        };

        if (mailError == MailResponseResult.EquipError)
            result.BagResult = (int)equipError;
        else if (mailAction == MailResponseType.ItemTaken)
        {
            result.AttachID = itemGuid;
            result.QtyInInventory = (int)itemCount;
        }

        SendPacket(result);
    }

    public void SendMovementSetCollisionHeight(float height, UpdateCollisionHeightReason reason)
    {
        MoveSetCollisionHeight setCollisionHeight = new()
        {
            MoverGUID = GUID,
            SequenceIndex = MovementCounter++,
            Height = height,
            Scale = ObjectScale,
            MountDisplayID = MountDisplayId,
            ScaleDuration = UnitData.ScaleDuration,
            Reason = reason
        };

        SendPacket(setCollisionHeight);

        MoveUpdateCollisionHeight updateCollisionHeight = new()
        {
            Status = MovementInfo,
            Height = height,
            Scale = ObjectScale
        };

        SendMessageToSet(updateCollisionHeight, false);
    }

    public void SendMovieStart(uint movieId)
    {
        Movie = movieId;

        TriggerMovie packet = new()
        {
            MovieID = movieId
        };

        SendPacket(packet);
    }

    public void SendOnCancelExpectedVehicleRideAura()
    {
        SendPacket(new OnCancelExpectedRideVehicleAura());
    }

    //Network
    public void SendPacket(ServerPacket data)
    {
        Session.SendPacket(data);
    }

    public void SendPlayerBound(ObjectGuid binderGuid, uint areaId)
    {
        PlayerBound packet = new(binderGuid, areaId);
        SendPacket(packet);
    }

    public void SendPlayerChoice(ObjectGuid sender, int choiceId)
    {
        var playerChoice = GameObjectManager.GetPlayerChoice(choiceId);

        if (playerChoice == null)
            return;

        var locale = Session.SessionDbLocaleIndex;
        var playerChoiceLocale = locale != Locale.enUS ? GameObjectManager.GetPlayerChoiceLocale(choiceId) : null;

        PlayerTalkClass.InteractionData.Reset();
        PlayerTalkClass.InteractionData.SourceGuid = sender;
        PlayerTalkClass.InteractionData.PlayerChoiceId = (uint)choiceId;

        DisplayPlayerChoice displayPlayerChoice = new()
        {
            SenderGUID = sender,
            ChoiceID = choiceId,
            UiTextureKitID = playerChoice.UiTextureKitId,
            SoundKitID = playerChoice.SoundKitId,
            Question = playerChoice.Question
        };

        if (playerChoiceLocale != null)
            GameObjectManager.GetLocaleString(playerChoiceLocale.Question, locale, ref displayPlayerChoice.Question);

        displayPlayerChoice.CloseChoiceFrame = false;
        displayPlayerChoice.HideWarboardHeader = playerChoice.HideWarboardHeader;
        displayPlayerChoice.KeepOpenAfterChoice = playerChoice.KeepOpenAfterChoice;

        for (var i = 0; i < playerChoice.Responses.Count; ++i)
        {
            var playerChoiceResponseTemplate = playerChoice.Responses[i];

            var playerChoiceResponse = new PlayerChoiceResponse
            {
                ResponseID = playerChoiceResponseTemplate.ResponseId,
                ResponseIdentifier = playerChoiceResponseTemplate.ResponseIdentifier,
                ChoiceArtFileID = playerChoiceResponseTemplate.ChoiceArtFileId,
                Flags = playerChoiceResponseTemplate.Flags,
                WidgetSetID = playerChoiceResponseTemplate.WidgetSetID,
                UiTextureAtlasElementID = playerChoiceResponseTemplate.UiTextureAtlasElementID,
                SoundKitID = playerChoiceResponseTemplate.SoundKitID,
                GroupID = playerChoiceResponseTemplate.GroupID,
                UiTextureKitID = playerChoiceResponseTemplate.UiTextureKitID,
                Answer = playerChoiceResponseTemplate.Answer,
                Header = playerChoiceResponseTemplate.Header,
                SubHeader = playerChoiceResponseTemplate.SubHeader,
                ButtonTooltip = playerChoiceResponseTemplate.ButtonTooltip,
                Description = playerChoiceResponseTemplate.Description,
                Confirmation = playerChoiceResponseTemplate.Confirmation
            };

            if (playerChoiceLocale?.Responses.TryGetValue(playerChoiceResponseTemplate.ResponseId, out var playerChoiceResponseLocale) == true)
            {
                GameObjectManager.GetLocaleString(playerChoiceResponseLocale.Answer, locale, ref playerChoiceResponse.Answer);
                GameObjectManager.GetLocaleString(playerChoiceResponseLocale.Header, locale, ref playerChoiceResponse.Header);
                GameObjectManager.GetLocaleString(playerChoiceResponseLocale.SubHeader, locale, ref playerChoiceResponse.SubHeader);
                GameObjectManager.GetLocaleString(playerChoiceResponseLocale.ButtonTooltip, locale, ref playerChoiceResponse.ButtonTooltip);
                GameObjectManager.GetLocaleString(playerChoiceResponseLocale.Description, locale, ref playerChoiceResponse.Description);
                GameObjectManager.GetLocaleString(playerChoiceResponseLocale.Confirmation, locale, ref playerChoiceResponse.Confirmation);
            }

            if (playerChoiceResponseTemplate.Reward == null)
                continue;

            var reward = new PlayerChoiceResponseReward
            {
                TitleID = playerChoiceResponseTemplate.Reward.TitleId,
                PackageID = playerChoiceResponseTemplate.Reward.PackageId,
                SkillLineID = playerChoiceResponseTemplate.Reward.SkillLineId,
                SkillPointCount = playerChoiceResponseTemplate.Reward.SkillPointCount,
                ArenaPointCount = playerChoiceResponseTemplate.Reward.ArenaPointCount,
                HonorPointCount = playerChoiceResponseTemplate.Reward.HonorPointCount,
                Money = playerChoiceResponseTemplate.Reward.Money,
                Xp = playerChoiceResponseTemplate.Reward.Xp
            };

            foreach (var rewardItem in playerChoiceResponseTemplate.Reward.Items)
            {
                var rewardEntry = new PlayerChoiceResponseRewardEntry
                {
                    Item =
                    {
                        ItemID = rewardItem.Id
                    },
                    Quantity = rewardItem.Quantity
                };

                if (!rewardItem.BonusListIDs.Empty())

                {
                    rewardEntry.Item.ItemBonus = new ItemBonuses
                    {
                        BonusListIDs = rewardItem.BonusListIDs
                    };

                    reward.Items.Add(rewardEntry);
                }

                foreach (var currency in playerChoiceResponseTemplate.Reward.Currency)
                {
                    rewardEntry = new PlayerChoiceResponseRewardEntry
                    {
                        Item =
                        {
                            ItemID = currency.Id
                        },
                        Quantity = currency.Quantity
                    };

                    reward.Items.Add(rewardEntry);
                }

                foreach (var faction in playerChoiceResponseTemplate.Reward.Faction)
                {
                    rewardEntry = new PlayerChoiceResponseRewardEntry
                    {
                        Item =

                        {
                            ItemID = faction.Id
                        },
                        Quantity = faction.Quantity
                    };

                    reward.Items.Add(rewardEntry);
                }

                foreach (var item in playerChoiceResponseTemplate.Reward.ItemChoices)
                {
                    rewardEntry = new PlayerChoiceResponseRewardEntry
                    {
                        Item =
                        {
                            ItemID = item.Id
                        },
                        Quantity = item.Quantity
                    };

                    if (!item.BonusListIDs.Empty())
                    {
                        rewardEntry.Item.ItemBonus = new ItemBonuses
                        {
                            BonusListIDs = item.BonusListIDs
                        };

                        reward.ItemChoices.Add(rewardEntry);
                    }

                    playerChoiceResponse.Reward = reward;
                    displayPlayerChoice.Responses[i] = playerChoiceResponse;
                }

                playerChoiceResponse.RewardQuestID = playerChoiceResponseTemplate.RewardQuestID;


                if (!playerChoiceResponseTemplate.MawPower.HasValue || playerChoiceResponse.MawPower == null)
                    continue;

                var mawPower = new PlayerChoiceResponseMawPower
                {
                    TypeArtFileID = playerChoiceResponse.MawPower.Value.TypeArtFileID,
                    Rarity = playerChoiceResponse.MawPower.Value.Rarity,
                    RarityColor = playerChoiceResponse.MawPower.Value.RarityColor,
                    SpellID = playerChoiceResponse.MawPower.Value.SpellID,
                    MaxStacks = playerChoiceResponse.MawPower.Value.MaxStacks
                };

                playerChoiceResponse.MawPower = mawPower;
            }

            SendPacket(displayPlayerChoice);
        }
    }

    public void SendPreparedGossip(WorldObject source)
    {
        if (source == null)
            return;

        if (source.IsTypeId(TypeId.Unit) || source.IsTypeId(TypeId.GameObject))
            if (PlayerTalkClass.GossipMenu.IsEmpty() && !PlayerTalkClass.QuestMenu.IsEmpty())
            {
                SendPreparedQuest(source);

                return;
            }

        // in case non empty gossip menu (that not included quests list size) show it
        // (quest entries from quest menu will be included in list)

        var textId = GetGossipTextId(source);
        var menuId = PlayerTalkClass.GossipMenu.MenuId;

        if (menuId != 0)
            textId = GetGossipTextId(menuId, source);

        PlayerTalkClass.SendGossipMenu(textId, source.GUID);
    }

    public void SendRemoveControlBar()
    {
        SendPacket(new PetSpells());
    }

    public void SendSummonRequestFrom(Unit summoner)
    {
        if (summoner == null)
            return;

        // Player already has active summon request
        if (HasSummonPending)
            return;

        // Evil Twin (ignore player summon, but hide this for summoner)
        if (HasAura(23445))
            return;

        _summonExpire = GameTime.CurrentTime + PlayerConst.MaxPlayerSummonDelay;
        _summonLocation = new WorldLocation(summoner.Location);

        if (summoner.Location.Map != null)
            _summonInstanceId = summoner.Location.Map.InstanceId;

        SummonRequest summonRequest = new()
        {
            SummonerGUID = summoner.GUID,
            SummonerVirtualRealmAddress = WorldManager.Realm.Id.VirtualRealmAddress,
            AreaID = (int)summoner.Location.Zone
        };

        SendPacket(summonRequest);

        var group = Group;

        if (group == null)
            return;

        BroadcastSummonCast summonCast = new()
        {
            Target = GUID
        };

        group.BroadcastPacket(summonCast, false);
    }

    public void SendTameFailure(PetTameResult result)
    {
        PetTameFailure petTameFailure = new()
        {
            Result = (byte)result
        };

        SendPacket(petTameFailure);
    }

    public void SendUpdateWorldState(WorldStates variable, uint value, bool hidden = false)
    {
        SendUpdateWorldState((uint)variable, value, hidden);
    }

    public void SendUpdateWorldState(uint variable, uint value, bool hidden = false)
    {
        UpdateWorldState worldstate = new()
        {
            VariableID = variable,
            Value = (int)value,
            Hidden = hidden
        };

        SendPacket(worldstate);
    }

    public void SetAcceptWhispers(bool on)
    {
        if (on)
            _extraFlags |= PlayerExtraFlags.AcceptWhispers;
        else
            _extraFlags &= ~PlayerExtraFlags.AcceptWhispers;
    }

    public void SetAdvancedCombatLogging(bool enabled)
    {
        IsAdvancedCombatLoggingEnabled = enabled;
    }

    public void SetArenaFaction(byte arenaFaction)
    {
        SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.ArenaFaction), arenaFaction);
    }

    public void SetAtLoginFlag(AtLoginFlags f)
    {
        LoginFlags |= f;
    }

    public void SetAverageItemLevelEquipped(float newItemLevel)
    {
        SetUpdateFieldValue(ref Values.ModifyValue(PlayerData).ModifyValue(PlayerData.AvgItemLevel, 1), newItemLevel);
    }

    public void SetAverageItemLevelTotal(float newItemLevel)
    {
        SetUpdateFieldValue(ref Values.ModifyValue(PlayerData).ModifyValue(PlayerData.AvgItemLevel, 0), newItemLevel);
    }

    public void SetBaseModFlatValue(BaseModGroup modGroup, double val)
    {
        if (_auraBaseFlatMod[(int)modGroup] == val)
            return;

        _auraBaseFlatMod[(int)modGroup] = val;
        UpdateBaseModGroup(modGroup);
    }

    public void SetBaseModPctValue(BaseModGroup modGroup, double val)
    {
        if (_auraBasePctMod[(int)modGroup] == val)
            return;

        _auraBasePctMod[(int)modGroup] = val;
        UpdateBaseModGroup(modGroup);
    }

    public void SetBattlePetData(BattlePet pet = null)
    {
        if (pet != null)
        {
            SetSummonedBattlePetGUID(pet.PacketInfo.Guid);
            SetCurrentBattlePetBreedQuality(pet.PacketInfo.Quality);
            BattlePetCompanionExperience = pet.PacketInfo.Exp;
            WildBattlePetLevel = pet.PacketInfo.Level;
        }
        else
        {
            SetSummonedBattlePetGUID(ObjectGuid.Empty);
            SetCurrentBattlePetBreedQuality((byte)BattlePetBreedQuality.Poor);
            BattlePetCompanionExperience = 0;
            WildBattlePetLevel = 0;
        }
    }

    public void SetBeenGrantedLevelsFromRaF()
    {
        _extraFlags |= PlayerExtraFlags.GrantedLevelsFromRaf;
    }

    public void SetBindPoint(ObjectGuid guid)
    {
        NPCInteractionOpenResult npcInteraction = new()
        {
            Npc = guid,
            InteractionType = PlayerInteractionType.Binder,
            Success = true
        };

        SendPacket(npcInteraction);
    }

    public void SetChampioningFaction(uint faction)
    {
        _championingFaction = faction;
    }

    public void SetChosenTitle(uint title)
    {
        SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.PlayerTitle), title);
    }

    public void SetClientControl(Unit target, bool allowMove)
    {
        // don't allow possession to be overridden
        if (target.HasUnitState(UnitState.Charmed) && GUID != target.CharmerGUID)
        {
            // this should never happen, otherwise m_unitBeingMoved might be left dangling!
            Log.Logger.Error($"Player '{GetName()}' attempt to client control '{target.GetName()}', which is charmed by GUID {target.CharmerGUID}");

            return;
        }

        // still affected by some aura that shouldn't allow control, only allow on last such aura to be removed
        if (target.HasUnitState(UnitState.Fleeing | UnitState.Confused))
            allowMove = false;

        ControlUpdate packet = new()
        {
            Guid = target.GUID,
            On = allowMove
        };

        SendPacket(packet);

        var viewpoint = Viewpoint;

        if (viewpoint == null)
            viewpoint = this;

        if (target != viewpoint)
        {
            if (viewpoint != this)
                SetViewpoint(viewpoint, false);

            if (target != this)
                SetViewpoint(target, true);
        }

        SetMovedUnit(target);
    }

    public void SetCommandStatusOff(PlayerCommandStates command)
    {
        _activeCheats &= ~command;
    }

    public void SetCommandStatusOn(PlayerCommandStates command)
    {
        _activeCheats |= command;
    }

    public void SetCovenant(sbyte covenantId)
    {
        // General Additions
        if (GetQuestStatus(CovenantQuests.ChoosingYourPurpose_fromOribos) == QuestStatus.Incomplete)
            CompleteQuest(CovenantQuests.ChoosingYourPurpose_fromOribos);

        if (GetQuestStatus(CovenantQuests.ChoosingYourPurpose_fromNathria) == QuestStatus.Incomplete)
            CompleteQuest(CovenantQuests.ChoosingYourPurpose_fromNathria);

        SpellFactory.CastSpell(this, CovenantSpells.Remove_TBYB_Auras, true);
        SpellFactory.CastSpell(this, CovenantSpells.Create_Covenant_Garrison, true);
        SpellFactory.CastSpell(this, CovenantSpells.Start_Oribos_Intro_Quests, true);
        SpellFactory.CastSpell(this, CovenantSpells.Create_Garrison_Artifact_296, true);
        SpellFactory.CastSpell(this, CovenantSpells.Create_Garrison_Artifact_299, true);

        // Specific Additions
        switch (covenantId)
        {
            case Covenant.Kyrian:
                SpellFactory.CastSpell(this, CovenantSpells.Become_A_Kyrian, true);
                LearnSpell(CovenantSpells.CA_Opening_Kyrian, true);
                LearnSpell(CovenantSpells.CA_Kyrian, true);

                break;

            case Covenant.Venthyr:
                SpellFactory.CastSpell(this, CovenantSpells.Become_A_Venthyr, true);
                LearnSpell(CovenantSpells.CA_Opening_Venthyr, true);
                LearnSpell(CovenantSpells.CA_Venthyr, true);

                break;

            case Covenant.NightFae:
                SpellFactory.CastSpell(this, CovenantSpells.Become_A_NightFae, true);
                LearnSpell(CovenantSpells.CA_Opening_NightFae, true);
                LearnSpell(CovenantSpells.CA_NightFae, true);

                break;

            case Covenant.Necrolord:
                SpellFactory.CastSpell(this, CovenantSpells.Become_A_Necrolord, true);
                LearnSpell(CovenantSpells.CA_Opening_Necrolord, true);
                LearnSpell(CovenantSpells.CA_Necrolord, true);

                break;
        }

        // TODO
        // Save to DB
        //ObjectGuid guid = GetGUID();
        //var stmt = CharacterDatabase.GetPreparedStatement(CHAR_UPD_COVENANT);
        //stmt.AddValue(0, covenantId);
        //stmt.AddValue(1, guid.GetCounter());
        //CharacterDatabase.Execute(stmt);

        // UpdateField
        SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.CovenantID), covenantId);
    }

    public void SetCurrentBattlePetBreedQuality(byte battlePetBreedQuality)
    {
        SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.CurrentBattlePetBreedQuality), battlePetBreedQuality);
    }

    public void SetCustomizations(List<ChrCustomizationChoice> customizations, bool markChanged = true)
    {
        if (markChanged)
            _customizationsChanged = true;

        ClearDynamicUpdateFieldValues(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.Customizations));

        foreach (var newChoice in customizations.Select(customization => new ChrCustomizationChoice
                 {
                     ChrCustomizationOptionID = customization.ChrCustomizationOptionID,
                     ChrCustomizationChoiceID = customization.ChrCustomizationChoiceID
                 }))
            AddDynamicUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.Customizations), newChoice);
    }

    public override void SetDeathState(DeathState s)
    {
        var oldIsAlive = IsAlive;

        if (s == DeathState.JustDied)
        {
            if (!oldIsAlive)
            {
                Log.Logger.Error("Player.setDeathState: Attempted to kill a dead player '{0}' ({1})", GetName(), GUID.ToString());

                return;
            }

            // drunken state is cleared on death
            SetDrunkValue(0);
            // lost combo points at any target (targeted combo points clear in Unit::setDeathState)
            ClearComboPoints();

            ClearResurrectRequestData();

            //FIXME: is pet dismissed at dying or releasing spirit? if second, add setDeathState(DEAD) to HandleRepopRequestOpcode and define pet unsummon here with (s == DEAD)
            RemovePet(null, PetSaveMode.NotInSlot, true);

            InitializeSelfResurrectionSpells();

            UpdateCriteria(CriteriaType.DieOnMap, 1);
            UpdateCriteria(CriteriaType.DieAnywhere, 1);
            UpdateCriteria(CriteriaType.DieInInstance, 1);

            // reset all death criterias
            ResetCriteria(CriteriaFailEvent.Death, 0);
        }

        base.SetDeathState(s);

        if (IsAlive && !oldIsAlive)
            //clear aura case after resurrection by another way (spells will be applied before next death)
            ClearSelfResSpell();
    }

    public void SetDeveloper(bool on)
    {
        if (on)
            SetPlayerFlag(PlayerFlags.Developer);
        else
            RemovePlayerFlag(PlayerFlags.Developer);
    }

    public void SetDrunkValue(byte newDrunkValue, uint itemId = 0)
    {
        var isSobering = newDrunkValue < DrunkValue;
        var oldDrunkenState = PlayerComputators.GetDrunkenstateByValue(DrunkValue);

        if (newDrunkValue > 100)
            newDrunkValue = 100;

        // select drunk percent or total SPELL_AURA_MOD_FAKE_INEBRIATE amount, whichever is higher for visibility updates
        var drunkPercent = Math.Max(newDrunkValue, GetTotalAuraModifier(AuraType.ModFakeInebriate));

        if (drunkPercent != 0)
        {
            Visibility.InvisibilityDetect.AddFlag(InvisibilityType.Drunk);
            Visibility.InvisibilityDetect.SetValue(InvisibilityType.Drunk, drunkPercent);
        }
        else if (!HasAuraType(AuraType.ModFakeInebriate) && newDrunkValue == 0)
            Visibility.InvisibilityDetect.DelFlag(InvisibilityType.Drunk);

        var newDrunkenState = PlayerComputators.GetDrunkenstateByValue(newDrunkValue);
        SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.Inebriation), newDrunkValue);
        UpdateObjectVisibility();

        if (!isSobering)
            _drunkTimer = 0; // reset sobering timer

        if (newDrunkenState == oldDrunkenState)
            return;

        CrossedInebriationThreshold data = new()
        {
            Guid = GUID,
            Threshold = (uint)newDrunkenState,
            ItemID = itemId
        };

        SendMessageToSet(data, true);
    }

    public void SetFactionForRace(Race race)
    {
        Team = TeamForRace(race, CliDB);

        var rEntry = CliDB.ChrRacesStorage.LookupByKey((uint)race);
        Faction = rEntry != null ? (uint)rEntry.FactionID : 0;
    }

    public void SetFallInformation(uint time, float z)
    {
        _lastFallTime = time;
        _lastFallZ = z;
    }

    public void SetFreePrimaryProfessions(uint profs)
    {
        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.CharacterPoints), profs);
    }

    public void SetGameMaster(bool on)
    {
        if (on)
        {
            _extraFlags |= PlayerExtraFlags.GMOn;
            Faction = 35;
            SetPlayerFlag(PlayerFlags.GM);
            SetUnitFlag2(UnitFlags2.AllowCheatSpells);

            var pet = CurrentPet;

            if (pet != null)
                pet.Faction = 35;

            RemovePvpFlag(UnitPVPStateFlags.FFAPvp);
            ResetContestedPvP();

            CombatStopWithPets();

            PhasingHandler.SetAlwaysVisible(this, true, false);
            Visibility.ServerSideVisibilityDetect.SetValue(ServerSideVisibilityType.GM, Session.Security);
        }
        else
        {
            PhasingHandler.SetAlwaysVisible(this, HasAuraType(AuraType.PhaseAlwaysVisible), false);

            _extraFlags &= ~PlayerExtraFlags.GMOn;
            RestoreFaction();
            RemovePlayerFlag(PlayerFlags.GM);
            RemoveUnitFlag2(UnitFlags2.AllowCheatSpells);

            var pet = CurrentPet;

            if (pet != null)
                pet.Faction = Faction;

            // restore FFA PvP Server state
            if (WorldMgr.IsFFAPvPRealm)
                SetPvpFlag(UnitPVPStateFlags.FFAPvp);

            // restore FFA PvP area state, remove not allowed for GM mounts
            UpdateArea(_areaUpdateId);

            Visibility.ServerSideVisibilityDetect.SetValue(ServerSideVisibilityType.GM, AccountTypes.Player);
        }

        UpdateObjectVisibility();
    }

    public void SetGMChat(bool on)
    {
        if (on)
            _extraFlags |= PlayerExtraFlags.GMChat;
        else
            _extraFlags &= ~PlayerExtraFlags.GMChat;
    }

    public void SetGMVisible(bool on)
    {
        if (on)
        {
            _extraFlags &= ~PlayerExtraFlags.GMInvisible; //remove Id
            Visibility.ServerSideVisibility.SetValue(ServerSideVisibilityType.GM, AccountTypes.Player);
        }
        else
        {
            _extraFlags |= PlayerExtraFlags.GMInvisible; //add Id

            SetAcceptWhispers(false);
            SetGameMaster(true);

            Visibility.ServerSideVisibility.SetValue(ServerSideVisibilityType.GM, Session.Security);
        }

        foreach (var channel in JoinedChannels)
            channel.SetInvisible(this, !on);
    }

    public void SetGuildRank(byte rankId)
    {
        SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.GuildRankID), rankId);
    }

    public void SetHasLevelBoosted()
    {
        _extraFlags |= PlayerExtraFlags.LevelBoosted;
    }

    public void SetHasRaceChanged()
    {
        _extraFlags |= PlayerExtraFlags.HasRaceChanged;
    }

    public void SetHeirloom(int slot, uint itemId)
    {
        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Heirlooms, slot), itemId);
    }

    public void SetHeirloomFlags(int slot, uint flags)
    {
        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.HeirloomFlags, slot), flags);
    }

    public void SetHomebind(WorldLocation loc, uint areaId)
    {
        Homebind.WorldRelocate(loc);
        _homebindAreaId = areaId;

        // update sql homebind
        var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_PLAYER_HOMEBIND);
        stmt.AddValue(0, Homebind.MapId);
        stmt.AddValue(1, _homebindAreaId);
        stmt.AddValue(2, Homebind.X);
        stmt.AddValue(3, Homebind.Y);
        stmt.AddValue(4, Homebind.Z);
        stmt.AddValue(5, Homebind.Orientation);
        stmt.AddValue(6, GUID.Counter);
        CharacterDatabase.Execute(stmt);
    }

    //Guild
    public void SetInGuild(ulong guildId)
    {
        if (guildId != 0)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.GuildGUID), ObjectGuid.Create(HighGuid.Guild, guildId));
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.GuildClubMemberID), GUID.Counter);
            SetPlayerFlag(PlayerFlags.GuildLevelEnabled);
        }
        else
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.GuildGUID), ObjectGuid.Empty);
            RemovePlayerFlag(PlayerFlags.GuildLevelEnabled);
        }

        CharacterCache.UpdateCharacterGuildId(GUID, guildId);
    }

    public void SetInvSlot(uint slot, ObjectGuid guid)
    {
        SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.InvSlots, (int)slot), guid);
    }

    public void SetKnownTitles(int index, ulong mask)
    {
        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.KnownTitles, index), mask);
    }

    public void SetMultiActionBars(byte mask)
    {
        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.MultiActionBars), mask);
    }

    public void SetNumRespecs(byte numRespecs)
    {
        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.NumRespecs), numRespecs);
    }

    public void SetPlayerFlag(PlayerFlags flags)
    {
        SetUpdateFieldFlagValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.PlayerFlags), (uint)flags);
    }

    public void SetPlayerFlagEx(PlayerFlagsEx flags)
    {
        SetUpdateFieldFlagValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.PlayerFlagsEx), (uint)flags);
    }

    public void SetPlayerLocalFlag(PlayerLocalFlags flags)
    {
        SetUpdateFieldFlagValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.LocalFlags), (uint)flags);
    }

    public void SetPvpTitle(byte pvpTitle)
    {
        SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.PvpTitle), pvpTitle);
    }

    public void SetRegenTimerCount(uint time)
    {
        _regenTimerCount = time;
    }

    public void SetReputation(uint factionentry, int value)
    {
        ReputationMgr.SetReputation(CliDB.FactionStorage.LookupByKey(factionentry), value);
    }

    public void SetRestState(RestTypes type, PlayerRestState state)
    {
        var restInfo = Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.RestInfo, (int)type);
        SetUpdateFieldValue(restInfo.ModifyValue(restInfo.StateID), (byte)state);
    }

    public void SetRestThreshold(RestTypes type, uint threshold)
    {
        var restInfo = Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.RestInfo, (int)type);
        SetUpdateFieldValue(restInfo.ModifyValue(restInfo.Threshold), threshold);
    }

    public void SetResurrectRequestData(WorldObject caster, uint health, uint mana, uint appliedAura)
    {
        _resurrectionData = new ResurrectionData
        {
            Guid = caster.GUID
        };

        _resurrectionData.Location.WorldRelocate(caster.Location);
        _resurrectionData.Health = health;
        _resurrectionData.Mana = mana;
        _resurrectionData.Aura = appliedAura;
    }

    public void SetSelection(ObjectGuid guid)
    {
        SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Target), guid);
    }

    public void SetSemaphoreTeleportFar(bool semphsetting)
    {
        IsBeingTeleportedFar = semphsetting;
    }

    public void SetSemaphoreTeleportNear(bool semphsetting)
    {
        IsBeingTeleportedNear = semphsetting;
    }

    public void SetSummonedBattlePetGUID(ObjectGuid guid)
    {
        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.SummonedBattlePetGUID), guid);
    }

    //Target
    // Used for serverside target changes, does not apply to players
    public override void SetTarget(ObjectGuid guid) { }

    public void SetTaxiCheater(bool on)
    {
        if (on)
            _extraFlags |= PlayerExtraFlags.TaxiCheat;
        else
            _extraFlags &= ~PlayerExtraFlags.TaxiCheat;
    }

    public void SetTitle(CharTitlesRecord title, bool lost = false)
    {
        var fieldIndexOffset = title.MaskID / 64;
        var flag = 1ul << (title.MaskID % 64);

        if (lost)
        {
            if (!HasTitle(title))
                return;

            RemoveUpdateFieldFlagValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.KnownTitles, fieldIndexOffset), flag);
        }
        else
        {
            if (HasTitle(title))
                return;

            SetUpdateFieldFlagValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.KnownTitles, fieldIndexOffset), flag);
        }

        TitleEarned packet = new(lost ? ServerOpcodes.TitleLost : ServerOpcodes.TitleEarned)
        {
            Index = title.MaskID
        };

        SendPacket(packet);
    }

    public void SetTrackCreatureFlag(uint flags)
    {
        SetUpdateFieldFlagValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.TrackCreatureMask), flags);
    }

    public void SetTransportServerTime(int transportServerTime)
    {
        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.TransportServerTime), transportServerTime);
    }

    public void SetVersatilityBonus(float value)
    {
        SetUpdateFieldStatValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.VersatilityBonus), value);
    }

    public void SetViewpoint(WorldObject target, bool apply)
    {
        if (apply)
        {
            Log.Logger.Debug("Player.CreateViewpoint: Player {0} create seer {1} (TypeId: {2}).", GetName(), target.Entry, target.TypeId);

            if (ActivePlayerData.FarsightObject != ObjectGuid.Empty)
            {
                Log.Logger.Fatal("Player.CreateViewpoint: Player {0} cannot add new viewpoint!", GetName());

                return;
            }

            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.FarsightObject), target.GUID);

            // farsight dynobj or puppet may be very far away
            UpdateVisibilityOf(target);

            if (target.IsTypeMask(TypeMask.Unit) && target != VehicleBase)
                target.AsUnit.AddPlayerToVision(this);

            SetSeer(target);
        }
        else
        {
            Log.Logger.Debug("Player.CreateViewpoint: Player {0} remove seer", GetName());

            if (target.GUID != ActivePlayerData.FarsightObject)
            {
                Log.Logger.Fatal("Player.CreateViewpoint: Player {0} cannot remove current viewpoint!", GetName());

                return;
            }

            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.FarsightObject), ObjectGuid.Empty);

            if (target.IsTypeMask(TypeMask.Unit) && target != VehicleBase)
                target.AsUnit.RemovePlayerFromVision(this);

            //must immediately set seer back otherwise may crash
            SetSeer(this);
        }
    }

    public void SetVirtualPlayerRealm(uint virtualRealmAddress)
    {
        SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.VirtualPlayerRealm), virtualRealmAddress);
    }

    public void SetWarModeDesired(bool enabled)
    {
        // Only allow to toggle on when in stormwind/orgrimmar, and to toggle off in any rested place.
        // Also disallow when in combat
        if (enabled == IsWarModeDesired || IsInCombat || !HasPlayerFlag(PlayerFlags.Resting))
            return;

        if (enabled && !CanEnableWarModeInArea())
            return;

        // Don't allow to chang when aura SPELL_PVP_RULES_ENABLED is on
        if (HasAura(PlayerConst.SpellPvpRulesEnabled))
            return;

        if (enabled)
        {
            SetPlayerFlag(PlayerFlags.WarModeDesired);
            SetPvP(true);
        }
        else
        {
            RemovePlayerFlag(PlayerFlags.WarModeDesired);
            SetPvP(false);
        }

        UpdateWarModeAuras();
    }

    public void SetWatchedFactionIndex(uint index)
    {
        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.WatchedFactionIndex), index);
    }

    public void SpawnCorpseBones(bool triggerSave = true)
    {
        CorpseLocation = new WorldLocation();

        if (Location.Map.ConvertCorpseToBones(GUID) == null)
            return;

        if (triggerSave && !Session.PlayerLogoutWithSave) // at logout we will already store the player
            SaveToDB();                                   // prevent loading as ghost without corpse
    }

    public void StopCastingCharm()
    {
        if (Charmed == null)
            return;

        if (Charmed.IsTypeId(TypeId.Unit))
        {
            if (Charmed.AsCreature.HasUnitTypeMask(UnitTypeMask.Puppet))
                ((Puppet)Charmed).UnSummon();
            else if (Charmed.IsVehicle)
            {
                ExitVehicle();

                // Temporary for issue https://github.com/TrinityCore/TrinityCore/issues/24876
                if (!CharmedGUID.IsEmpty && !Charmed.HasAuraTypeWithCaster(AuraType.ControlVehicle, GUID))
                {
                    Log.Logger.Fatal($"Player::StopCastingCharm Player '{GetName()}' ({GUID}) is not able to uncharm vehicle ({CharmedGUID}) because of missing SPELL_AURA_CONTROL_VEHICLE");

                    // attempt to recover from missing HandleAuraControlVehicle unapply handling
                    // THIS IS A HACK, NEED TO FIND HOW IS IT EVEN POSSBLE TO NOT HAVE THE AURA
                    _ExitVehicle();
                }
            }
        }

        if (!CharmedGUID.IsEmpty)
            Charmed.RemoveCharmAuras();

        if (CharmedGUID.IsEmpty)
            return;

        Log.Logger.Fatal("Player {0} (GUID: {1} is not able to uncharm unit (GUID: {2} Entry: {3}, Type: {4})", GetName(), GUID, CharmedGUID, Charmed.Entry, Charmed.TypeId);

        if (!Charmed.CharmerGUID.IsEmpty)
            Log.Logger.Fatal($"Player::StopCastingCharm: Charmed unit has charmer {Charmed.CharmerGUID}\nPlayer debug info: {GetDebugInfo()}\nCharm debug info: {Charmed.GetDebugInfo()}");
        else
            SetCharm(Charmed, false);
    }

    public void StopMirrorTimers()
    {
        StopMirrorTimer(MirrorTimerType.Fatigue);
        StopMirrorTimer(MirrorTimerType.Breath);
        StopMirrorTimer(MirrorTimerType.Fire);
    }

    public void SummonIfPossible(bool agree)
    {
        void BroadcastSummonResponse(bool accepted)
        {
            var group = Group;

            if (group == null)
                return;

            BroadcastSummonResponse summonResponse = new()
            {
                Target = GUID,
                Accepted = accepted
            };

            group.BroadcastPacket(summonResponse, false);
        }

        if (!agree)
        {
            _summonExpire = 0;
            BroadcastSummonResponse(false);

            return;
        }

        // expire and auto declined
        if (_summonExpire < GameTime.CurrentTime)
        {
            BroadcastSummonResponse(false);

            return;
        }

        // stop taxi flight at summon
        FinishTaxiFlight();

        // drop Id at summon
        // this code can be reached only when GM is summoning player who carries Id, because player should be immune to summoning spells when he carries Id
        Battleground?.EventPlayerDroppedFlag(this);

        _summonExpire = 0;

        UpdateCriteria(CriteriaType.AcceptSummon, 1);
        RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Summon);

        TeleportTo(_summonLocation, 0, _summonInstanceId);

        BroadcastSummonResponse(true);
    }

    public Pet SummonPet(uint entry, PetSaveMode? slot, Position pos, uint duration)
    {
        return SummonPet(entry, slot, pos, duration, out _);
    }

    public Pet SummonPet(uint entry, PetSaveMode? slot, Position pos, uint duration, out bool isNew)
    {
        isNew = false;

        var petStable = PetStable;

        Pet pet = new(this, PetType.Summon);

        if (pet.LoadPetFromDB(this, entry, 0, false, slot))
        {
            if (duration > 0)
                pet.SetDuration(duration);

            return null;
        }

        // petentry == 0 for hunter "call pet" (current pet summoned if any)
        if (entry == 0)
            return null;

        // only SUMMON_PET are handled here
        Location.UpdateAllowedPositionZ(pos);

        if (!pet.Location.IsPositionValid)
        {
            Log.Logger.Error("Pet (guidlow {0}, entry {1}) not summoned. Suggested coordinates isn't valid (X: {2} Y: {3})",
                             pet.GUID.ToString(),
                             pet.Entry,
                             pet.Location.X,
                             pet.Location.Y);

            return null;
        }

        var map = Location.Map;
        var petNumber = GameObjectManager.GeneratePetNumber();

        if (!pet.Create(map.GenerateLowGuid(HighGuid.Pet), map, entry, petNumber))
        {
            Log.Logger.Error("no such creature entry {0}", entry);

            return null;
        }

        if (petStable.GetCurrentPet() != null)
            RemovePet(null, PetSaveMode.NotInSlot);

        PhasingHandler.InheritPhaseShift(pet, this);

        pet.SetCreatorGUID(GUID);
        pet.Faction = Faction;
        pet.ReplaceAllNpcFlags(NPCFlags.None);
        pet.ReplaceAllNpcFlags2(NPCFlags2.None);
        pet.InitStatsForLevel(Level);

        SetMinion(pet, true);

        // this enables pet details window (Shift+P)
        pet.GetCharmInfo().SetPetNumber(petNumber, true);
        pet.Class = PlayerClass.Mage;
        pet.SetPetExperience(0);
        pet.SetPetNextLevelExperience(1000);
        pet.SetFullHealth();
        pet.SetFullPower(PowerType.Mana);
        pet.SetPetNameTimestamp((uint)GameTime.CurrentTime);

        map.AddToMap(pet.AsCreature);

        petStable.SetCurrentUnslottedPetIndex((uint)petStable.UnslottedPets.Count);
        PetStable.PetInfo petInfo = new();
        pet.FillPetInfo(petInfo);
        petStable.UnslottedPets.Add(petInfo);

        pet.InitPetCreateSpells();
        pet.SavePetToDB(PetSaveMode.AsCurrent);
        PetSpellInitialize();

        if (duration > 0)
            pet.SetDuration(duration);

        //ObjectAccessor.UpdateObjectVisibility(pet);

        isNew = true;
        pet.Location.WorldRelocate(pet.OwnerUnit.Location.MapId, pos);
        pet.NearTeleportTo(pos);

        return pet;
    }

    public bool TeleportTo(WorldLocation loc, TeleportToOptions options = 0, uint? instanceId = null)
    {
        return TeleportTo(loc.MapId, loc.X, loc.Y, loc.Z, loc.Orientation, options, instanceId);
    }

    public bool TeleportTo(uint mapid, Position loc, TeleportToOptions options = 0, uint? instanceId = null)
    {
        return TeleportTo(mapid, loc.X, loc.Y, loc.Z, loc.Orientation, options, instanceId);
    }

    public bool TeleportTo(uint mapid, float x, float y, float z, float orientation, TeleportToOptions options = 0, uint? instanceId = null)
    {
        if (!GridDefines.IsValidMapCoord(mapid, x, y, z, orientation))
        {
            Log.Logger.Error("TeleportTo: invalid map ({0}) or invalid coordinates (X: {1}, Y: {2}, Z: {3}, O: {4}) given when teleporting player (GUID: {5}, name: {6}, map: {7}, {8}).",
                             mapid,
                             x,
                             y,
                             z,
                             orientation,
                             GUID.ToString(),
                             GetName(),
                             Location.MapId,
                             Location.ToString());

            return false;
        }

        if (!Session.HasPermission(RBACPermissions.SkipCheckDisableMap) && DisableManager.IsDisabledFor(DisableType.Map, mapid, this))
        {
            Log.Logger.Error("Player (GUID: {0}, name: {1}) tried to enter a forbidden map {2}", GUID.ToString(), GetName(), mapid);
            SendTransferAborted(mapid, TransferAbortReason.MapNotAllowed);

            return false;
        }

        // preparing unsummon pet if lost (we must get pet before teleportation or will not find it later)
        var pet = CurrentPet;

        var mEntry = CliDB.MapStorage.LookupByKey(mapid);

        // don't let enter Battlegrounds without assigned Battlegroundid (for example through areatrigger)...
        // don't let gm level > 1 either
        if (!InBattleground && mEntry.IsBattlegroundOrArena())
            return false;

        // client without expansion support
        if (Session.Expansion < mEntry.Expansion())
        {
            Log.Logger.Debug("Player {0} using client without required expansion tried teleport to non accessible map {1}", GetName(), mapid);

            var transport = Transport;

            if (transport != null)
            {
                transport.RemovePassenger(this);
                RepopAtGraveyard(); // teleport to near graveyard if on transport, looks blizz like :)
            }

            SendTransferAborted(mapid, TransferAbortReason.InsufExpanLvl, (byte)mEntry.Expansion());

            return false; // normal client can't teleport to this map...
        }

        Log.Logger.Debug("Player {0} is being teleported to map {1}", GetName(), mapid);

        if (Vehicle != null)
            ExitVehicle();

        // reset movement flags at teleport, because player will continue move with these flags after teleport
        SetUnitMovementFlags(MovementInfo.MovementFlags & MovementFlag.MaskHasPlayerStatusOpcode);
        MovementInfo.ResetJump();
        DisableSpline();
        MotionMaster.Remove(MovementGeneratorType.Effect);

        if (Transport != null)
            if (!options.HasAnyFlag(TeleportToOptions.NotLeaveTransport))
                Transport.RemovePassenger(this);

        // The player was ported to another map and loses the duel immediately.
        // We have to perform this check before the teleport, otherwise the
        // ObjectAccessor won't find the Id.
        if (Duel != null && Location.MapId != mapid && Location.Map.GetGameObject(PlayerData.DuelArbiter) != null)
            DuelComplete(DuelCompleteType.Fled);

        if (Location.MapId == mapid && (!instanceId.HasValue || Location.Map?.InstanceId == instanceId))
        {
            //lets reset far teleport Id if it wasn't reset during chained teleports
            SetSemaphoreTeleportFar(false);
            //setup delayed teleport Id
            SetDelayedTeleportFlag(IsCanDelayTeleport);

            //if teleport spell is casted in Unit.Update() func
            //then we need to delay it until update process will be finished
            if (IsHasDelayedTeleport)
            {
                SetSemaphoreTeleportNear(true);
                //lets save teleport destination for player
                TeleportDest = new WorldLocation(mapid, x, y, z, orientation);
                TeleportDestInstanceId = null;
                TeleportOptions = options;

                return true;
            }

            if (!options.HasAnyFlag(TeleportToOptions.NotUnSummonPet))
                //same map, only remove pet if out of range for new position
                if (pet != null && !pet.Location.IsWithinDist3d(x, y, z, Location.Map.VisibilityRange))
                    UnsummonPetTemporaryIfAny();

            if (!IsAlive && options.HasAnyFlag(TeleportToOptions.ReviveAtTeleport))
                ResurrectPlayer(0.5f);

            if (!options.HasAnyFlag(TeleportToOptions.NotLeaveCombat))
                CombatStop();

            // this will be used instead of the current location in SaveToDB
            TeleportDest = new WorldLocation(mapid, x, y, z, orientation);
            TeleportDestInstanceId = null;
            TeleportOptions = options;
            SetFallInformation(0, Location.Z);

            // code for finish transfer called in WorldSession.HandleMovementOpcodes()
            // at client packet CMSG_MOVE_TELEPORT_ACK
            SetSemaphoreTeleportNear(true);

            // near teleport, triggering send CMSG_MOVE_TELEPORT_ACK from client at landing
            if (!Session.PlayerLogout)
                SendTeleportPacket(TeleportDest);
        }
        else
        {
            if (Class == PlayerClass.Deathknight && Location.MapId == 609 && !IsGameMaster && !HasSpell(50977))
            {
                SendTransferAborted(mapid, TransferAbortReason.UniqueMessage, 1);

                return false;
            }

            // far teleport to another map
            var oldmap = Location.IsInWorld ? Location.Map : null;
            // check if we can enter before stopping combat / removing pet / totems / interrupting spells

            // Check enter rights before map getting to avoid creating instance copy for player
            // this check not dependent from map instance copy and same for all instance copies of selected map
            var abortParams = Location.PlayerCannotEnter(mapid, this);

            if (abortParams != null)
            {
                SendTransferAborted(mapid, abortParams.Reason, abortParams.Arg, abortParams.MapDifficultyXConditionId);

                return false;
            }

            // Seamless teleport can happen only if cosmetic maps match
            if (oldmap != null &&
                oldmap.Entry.CosmeticParentMapID != mapid &&
                Location.MapId != mEntry.CosmeticParentMapID &&
                !((oldmap.Entry.CosmeticParentMapID != -1) ^ (oldmap.Entry.CosmeticParentMapID != mEntry.CosmeticParentMapID)))
                options &= ~TeleportToOptions.Seamless;

            //lets reset near teleport Id if it wasn't reset during chained teleports
            SetSemaphoreTeleportNear(false);
            //setup delayed teleport Id
            SetDelayedTeleportFlag(IsCanDelayTeleport);

            //if teleport spell is cast in Unit::Update() func
            //then we need to delay it until update process will be finished
            if (IsHasDelayedTeleport)
            {
                SetSemaphoreTeleportFar(true);
                //lets save teleport destination for player
                TeleportDest = new WorldLocation(mapid, x, y, z, orientation);
                TeleportDestInstanceId = instanceId;
                TeleportOptions = options;

                return true;
            }

            SetSelection(ObjectGuid.Empty);

            CombatStop();

            ResetContestedPvP();

            // remove player from Battlegroundon far teleport (when changing maps)

            if (Battleground != null)
                // Note: at Battlegroundjoin Battlegroundid set before teleport
                // and we already will found "current" Battleground
                // just need check that this is targeted map or leave
                if (Battleground.MapId != mapid)
                    LeaveBattleground(false); // don't teleport to entry point

            // remove arena spell coldowns/buffs now to also remove pet's cooldowns before it's temporarily unsummoned
            if (mEntry.IsBattleArena() && !IsGameMaster)
            {
                RemoveArenaSpellCooldowns(true);
                RemoveArenaAuras();
                pet?.RemoveArenaAuras();
            }

            // remove pet on map change
            UnsummonPetTemporaryIfAny();

            // remove all dyn objects
            RemoveAllDynObjects();

            // remove all areatriggers entities
            RemoveAllAreaTriggers();

            // stop spellcasting
            // not attempt interrupt teleportation spell at caster teleport
            if (!options.HasAnyFlag(TeleportToOptions.Spell))
                if (IsNonMeleeSpellCast(true))
                    InterruptNonMeleeSpells(true);

            //remove auras before removing from map...
            RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Moving | SpellAuraInterruptFlags.Turning);

            if (!Session.PlayerLogout && !options.HasAnyFlag(TeleportToOptions.Seamless))
            {
                // send transfer packets
                TransferPending transferPending = new()
                {
                    MapID = (int)mapid,
                    OldMapPosition = Location
                };

                var transport1 = (Transport)Transport;

                if (transport1 != null)
                {
                    TransferPending.ShipTransferPending shipTransferPending = new()
                    {
                        Id = transport1.Entry,
                        OriginMapID = (int)Location.MapId
                    };

                    transferPending.Ship = shipTransferPending;
                }

                SendPacket(transferPending);
            }

            // remove from old map now
            oldmap?.RemovePlayerFromMap(this, false);

            TeleportDest = new WorldLocation(mapid, x, y, z, orientation);
            TeleportDestInstanceId = instanceId;
            TeleportOptions = options;
            SetFallInformation(0, Location.Z);
            // if the player is saved before worldportack (at logout for example)
            // this will be used instead of the current location in SaveToDB

            if (!Session.PlayerLogout)
            {
                SuspendToken suspendToken = new()
                {
                    SequenceIndex = MovementCounter, // not incrementing
                    Reason = options.HasAnyFlag(TeleportToOptions.Seamless) ? 2 : 1u
                };

                SendPacket(suspendToken);
            }

            // move packet sent by client always after far teleport
            // code for finish transfer to new map called in WorldSession.HandleMoveWorldportAckOpcode at client packet
            SetSemaphoreTeleportFar(true);
        }

        return true;
    }

    public bool TeleportToBGEntryPoint()
    {
        if (_bgData.JoinPos.MapId == 0xFFFFFFFF)
            return false;

        ScheduleDelayedOperation(PlayerDelayedOperations.BGMountRestore);
        ScheduleDelayedOperation(PlayerDelayedOperations.BGTaxiRestore);
        ScheduleDelayedOperation(PlayerDelayedOperations.BGGroupRestore);

        return TeleportTo(_bgData.JoinPos);
    }

    public void ToggleAfk()
    {
        if (IsAfk)
            RemovePlayerFlag(PlayerFlags.AFK);
        else
            SetPlayerFlag(PlayerFlags.AFK);

        // afk player not allowed in Battleground
        if (!IsGameMaster && IsAfk && InBattleground && !InArena)
            LeaveBattleground();
    }

    public void ToggleDnd()
    {
        if (IsDnd)
            RemovePlayerFlag(PlayerFlags.DND);
        else
            SetPlayerFlag(PlayerFlags.DND);
    }

    public bool TryGetPet(out Pet pet)
    {
        pet = CurrentPet;

        return pet != null;
    }

    public void UnlockReagentBank()
    {
        SetPlayerFlagEx(PlayerFlagsEx.ReagentBankUnlocked);
    }

    public void UnsummonPetTemporaryIfAny()
    {
        var pet = CurrentPet;

        if (pet == null)
            return;

        if (TemporaryUnsummonedPetNumber == 0 && pet.IsControlled && !pet.IsTemporarySummoned)
        {
            TemporaryUnsummonedPetNumber = pet.GetCharmInfo().GetPetNumber();
            _oldpetspell = pet.UnitData.CreatedBySpell;
        }

        RemovePet(pet, PetSaveMode.AsCurrent);
    }

    public override void Update(uint diff)
    {
        if (!Location.IsInWorld)
            return;

        // undelivered mail
        if (_nextMailDelivereTime != 0 && _nextMailDelivereTime <= GameTime.CurrentTime)
        {
            SendNewMail();
            ++UnReadMails;

            // It will be recalculate at mailbox open (for unReadMails important non-0 until mailbox open, it also will be recalculated)
            _nextMailDelivereTime = 0;
        }

        // Update cinematic location, if 500ms have passed and we're doing a cinematic now.
        CinematicMgr.CinematicDiff += diff;

        if (CinematicMgr.CinematicCamera != null && CinematicMgr.ActiveCinematic != null && Time.GetMSTimeDiffToNow(CinematicMgr.LastCinematicCheck) > 500)
        {
            CinematicMgr.LastCinematicCheck = GameTime.CurrentTimeMS;
            CinematicMgr.UpdateCinematicLocation(diff);
        }

        //used to implement delayed far teleports
        SetCanDelayTeleport(true);
        base.Update(diff);
        SetCanDelayTeleport(false);

        var now = GameTime.CurrentTime;

        UpdatePvPFlag(now);

        UpdateContestedPvP(diff);

        UpdateDuelFlag(now);

        CheckDuelDistance(now);

        UpdateAfkReport(now);

        if (CombatManager.HasPvPCombat()) // Only set when in pvp combat
        {
            var aura = GetAura(PlayerConst.SpellPvpRulesEnabled);

            if (aura is { IsPermanent: false })
                aura.SetDuration(aura.SpellInfo.MaxDuration);
        }

        AIUpdateTick(diff);

        // Update items that have just a limited lifetime
        if (now > _lastTick)
            UpdateItemDuration((uint)(now - _lastTick));

        // check every second
        if (now > _lastTick + 1)
            UpdateSoulboundTradeItems();

        // If mute expired, remove it from the DB
        if (Session.MuteTime != 0 && Session.MuteTime < now)
        {
            Session.MuteTime = 0;
            var stmt = LoginDatabase.GetPreparedStatement(LoginStatements.UPD_MUTE_TIME);
            stmt.AddValue(0, 0); // Set the mute time to 0
            stmt.AddValue(1, "");
            stmt.AddValue(2, "");
            stmt.AddValue(3, Session.AccountId);
            LoginDatabase.Execute(stmt);
        }

        if (!_timedquests.Empty())
            foreach (var id in _timedquests)
            {
                var qStatus = _mQuestStatus[id];

                if (qStatus.Timer <= diff)
                    FailQuest(id);
                else
                {
                    qStatus.Timer -= diff;
                    _questStatusSave[id] = QuestSaveType.Default;
                }
            }

        _achievementSys.UpdateTimedCriteria(diff);

        if (HasUnitState(UnitState.MeleeAttacking) && !HasUnitState(UnitState.Casting | UnitState.Charging))
        {
            var victim = Victim;

            if (victim != null)
            {
                // default combat reach 10
                // TODO add weapon, skill check

                if (IsAttackReady())
                {
                    if (!IsWithinMeleeRange(victim))
                    {
                        SetAttackTimer(WeaponAttackType.BaseAttack, 100);

                        if (_swingErrorMsg != 1) // send single time (client auto repeat)
                        {
                            SendAttackSwingNotInRange();
                            _swingErrorMsg = 1;
                        }
                    }
                    //120 degrees of radiant range, if player is not in boundary radius
                    else if (!IsWithinBoundaryRadius(victim) && !Location.HasInArc(2 * MathFunctions.PI / 3, victim.Location))
                    {
                        SetAttackTimer(WeaponAttackType.BaseAttack, 100);

                        if (_swingErrorMsg != 2) // send single time (client auto repeat)
                        {
                            SendAttackSwingBadFacingAttack();
                            _swingErrorMsg = 2;
                        }
                    }
                    else
                    {
                        _swingErrorMsg = 0; // reset swing error state

                        // prevent base and off attack in same time, delay attack at 0.2 sec
                        if (HasOffhandWeapon)
                            if (GetAttackTimer(WeaponAttackType.OffAttack) < SharedConst.AttackDisplayDelay)
                                SetAttackTimer(WeaponAttackType.OffAttack, SharedConst.AttackDisplayDelay);

                        // do attack
                        AttackerStateUpdate(victim);
                        ResetAttackTimer();
                    }
                }

                if (!IsInFeralForm && HasOffhandWeapon && IsAttackReady(WeaponAttackType.OffAttack))
                {
                    if (!IsWithinMeleeRange(victim))
                        SetAttackTimer(WeaponAttackType.OffAttack, 100);
                    else if (!IsWithinBoundaryRadius(victim) && !Location.HasInArc(2 * MathFunctions.PI / 3, victim.Location))
                        SetAttackTimer(WeaponAttackType.BaseAttack, 100);
                    else
                    {
                        // prevent base and off attack in same time, delay attack at 0.2 sec
                        if (GetAttackTimer(WeaponAttackType.BaseAttack) < SharedConst.AttackDisplayDelay)
                            SetAttackTimer(WeaponAttackType.BaseAttack, SharedConst.AttackDisplayDelay);

                        // do attack
                        AttackerStateUpdate(victim, WeaponAttackType.OffAttack);
                        ResetAttackTimer(WeaponAttackType.OffAttack);
                    }
                }
            }
        }

        if (HasPlayerFlag(PlayerFlags.Resting))
            RestMgr.Update(diff);

        if (_weaponChangeTimer > 0)
        {
            if (diff >= _weaponChangeTimer)
                _weaponChangeTimer = 0;
            else
                _weaponChangeTimer -= diff;
        }

        if (_zoneUpdateTimer > 0)
        {
            if (diff >= _zoneUpdateTimer)
            {
                // On zone update tick check if we are still in an inn if we are supposed to be in one
                if (RestMgr.HasRestFlag(RestFlag.Tavern))
                {
                    var atEntry = CliDB.AreaTriggerStorage.LookupByKey(RestMgr.InnTriggerId);

                    if (atEntry == null || !IsInAreaTriggerRadius(atEntry))
                        RestMgr.RemoveRestFlag(RestFlag.Tavern);
                }

                if (_zoneUpdateId != Location.Zone)
                    UpdateZone(Location.Zone, Location.Area); // also update area
                else
                {
                    // use area updates as well
                    // needed for free far all arenas for example
                    if (_areaUpdateId != Location.Area)
                        UpdateArea(Location.Area);

                    _zoneUpdateTimer = 1 * Time.IN_MILLISECONDS;
                }
            }
            else
                _zoneUpdateTimer -= diff;
        }

        if (IsAlive)
        {
            RegenTimer += diff;
            RegenerateAll();
        }

        if (DeathState == DeathState.JustDied)
            KillPlayer();

        if (SaveTimer > 0)
        {
            if (diff >= SaveTimer)
            {
                // m_nextSave reset in SaveToDB call
                ScriptManager.ForEach<IPlayerOnSave>(p => p.OnSave(this));
                SaveToDB();
                Log.Logger.Debug("Player '{0}' (GUID: {1}) saved", GetName(), GUID.ToString());
            }
            else
                SaveTimer -= diff;
        }

        //Handle Water/drowning
        HandleDrowning(diff);

        // Played time
        if (now > _lastTick)
        {
            var elapsed = (uint)(now - _lastTick);
            TotalPlayedTime += elapsed;
            LevelPlayedTime += elapsed;
            _lastTick = now;
        }

        if (DrunkValue != 0)
        {
            _drunkTimer += diff;

            if (_drunkTimer > 9 * Time.IN_MILLISECONDS)
                HandleSobering();
        }

        if (HasPendingBind)
        {
            if (_pendingBindTimer <= diff)
            {
                // Player left the instance
                if (_pendingBindId == Location.Map?.InstanceId)
                    ConfirmPendingBind();

                SetPendingBind(0, 0);
            }
            else
                _pendingBindTimer -= diff;
        }

        // not auto-free ghost from body in instances
        if (DeathTimer > 0 && !Location.Map.Instanceable && !HasAuraType(AuraType.PreventResurrection))
        {
            if (diff >= DeathTimer)
            {
                DeathTimer = 0;
                BuildPlayerRepop();
                RepopAtGraveyard();
            }
            else
                DeathTimer -= diff;
        }

        UpdateEnchantTime(diff);
        UpdateHomebindTime(diff);

        if (!_instanceResetTimes.Empty())
            foreach (var instance in _instanceResetTimes.ToList())
                if (instance.Value < now)
                    _instanceResetTimes.Remove(instance.Key);

        // group update
        _groupUpdateTimer.Update(diff);

        if (_groupUpdateTimer.Passed)
        {
            SendUpdateToOutOfRangeGroupMembers();
            _groupUpdateTimer.Reset(5000);
        }

        var pet = CurrentPet;

        if (pet != null && !pet.Location.IsWithinDistInMap(this, Location.Map.VisibilityRange) && !pet.IsPossessed)
            RemovePet(pet, PetSaveMode.NotInSlot, true);

        if (IsAlive)
        {
            if (_hostileReferenceCheckTimer <= diff)
            {
                _hostileReferenceCheckTimer = 15 * Time.IN_MILLISECONDS;

                if (!Location.Map.IsDungeon)
                    CombatManager.EndCombatBeyondRange(Visibility.VisibilityRange, true);
            }
            else
                _hostileReferenceCheckTimer -= diff;
        }

        //we should execute delayed teleports only for alive(!) players
        //because we don't want player's ghost teleported from graveyard
        if (IsHasDelayedTeleport && IsAlive)
            TeleportTo(TeleportDest, TeleportOptions);
    }

    public override void UpdateDamageDoneMods(WeaponAttackType attackType, int skipEnchantSlot = -1)
    {
        base.UpdateDamageDoneMods(attackType, skipEnchantSlot);

        var unitMod = attackType switch
        {
            WeaponAttackType.BaseAttack   => UnitMods.DamageMainHand,
            WeaponAttackType.OffAttack    => UnitMods.DamageOffHand,
            WeaponAttackType.RangedAttack => UnitMods.DamageRanged,
            _                             => throw new NotImplementedException(),
        };

        var amount = 0.0f;
        var item = GetWeaponForAttack(attackType, true);

        if (item == null)
            return;

        for (var slot = EnchantmentSlot.Perm; slot < EnchantmentSlot.Max; ++slot)
        {
            if (skipEnchantSlot == (int)slot)
                continue;

            if (!CliDB.SpellItemEnchantmentStorage.TryGetValue(item.GetEnchantmentId(slot), out var enchantmentEntry))
                continue;

            for (byte i = 0; i < ItemConst.MaxItemEnchantmentEffects; ++i)
                switch (enchantmentEntry.Effect[i])
                {
                    case ItemEnchantmentType.Damage:
                        amount += enchantmentEntry.EffectScalingPoints[i];

                        break;

                    case ItemEnchantmentType.Totem:
                        if (Class == PlayerClass.Shaman)
                            amount += enchantmentEntry.EffectScalingPoints[i] * item.Template.Delay / 1000.0f;

                        break;
                }
        }

        HandleStatFlatModifier(unitMod, UnitModifierFlatType.Total, amount, true);
    }

    public void UpdateFallInformationIfNeed(MovementInfo minfo, ClientOpcodes opcode)
    {
        if (_lastFallTime >= MovementInfo.Jump.FallTime || _lastFallZ <= MovementInfo.Pos.Z || opcode == ClientOpcodes.MoveFallLand)
            SetFallInformation(MovementInfo.Jump.FallTime, MovementInfo.Pos.Z);
    }

    public void UpdateMirrorTimers()
    {
        // Desync flags for update on next HandleDrowning
        if (_mirrorTimerFlags != 0)
            _mirrorTimerFlagsLast = ~_mirrorTimerFlags;
    }

    public void UpdateNextMailTimeAndUnreads()
    {
        // calculate next delivery time (min. from non-delivered mails
        // and recalculate unReadMail
        var cTime = GameTime.CurrentTime;
        _nextMailDelivereTime = 0;
        UnReadMails = 0;

        foreach (var mail in Mails)
            if (mail.DeliverTime > cTime)
            {
                if (_nextMailDelivereTime == 0 || _nextMailDelivereTime > mail.DeliverTime)
                    _nextMailDelivereTime = mail.DeliverTime;
            }
            else if ((mail.CheckMask & MailCheckMask.Read) == 0)
                ++UnReadMails;
    }

    public void UpdateTriggerVisibility()
    {
        if (ClientGuiDs.Empty())
            return;

        if (!Location.IsInWorld)
            return;

        UpdateData udata = new(Location.MapId);

        lock (ClientGuiDs)
            foreach (var guid in ClientGuiDs)
                if (guid.IsCreatureOrVehicle)
                {
                    var creature = Location.Map.GetCreature(guid);

                    // Update fields of triggers, transformed units or unselectable units (values dependent on GM state)
                    if (creature == null || (!creature.IsTrigger && !creature.HasAuraType(AuraType.Transform) && !creature.HasUnitFlag(UnitFlags.Uninteractible)))
                        continue;

                    creature.Values.ModifyValue(UnitData).ModifyValue(UnitData.DisplayID);
                    creature.Values.ModifyValue(UnitData).ModifyValue(UnitData.Flags);
                    creature.ForceUpdateFieldChange();
                    creature.BuildValuesUpdateBlockForPlayer(udata, this);
                }
                else if (guid.IsAnyTypeGameObject)
                {
                    var go = Location.Map.GetGameObject(guid);

                    if (go == null)
                        continue;

                    go.Values.ModifyValue(ObjectData).ModifyValue(ObjectData.DynamicFlags);
                    go.ForceUpdateFieldChange();
                    go.BuildValuesUpdateBlockForPlayer(udata, this);
                }

        if (!udata.HasData())
            return;

        udata.BuildPacket(out var packet);
        SendPacket(packet);
    }

    public void ValidateMovementInfo(MovementInfo mi)
    {
        var removeViolatingFlags = new Action<bool, MovementFlag>((check, maskToRemove) =>
        {
            Log.Logger.Debug("Player.ValidateMovementInfo: Violation of MovementFlags found ({0}). MovementFlags: {1}, MovementFlags2: {2} for player {3}. Mask {4} will be removed.",
                             check,
                             mi.MovementFlags,
                             mi.MovementFlags2,
                             GUID.ToString(),
                             maskToRemove);

            if (!check)
                return;

            mi.RemoveMovementFlag(maskToRemove);
        });

        if (UnitMovedByMe.VehicleBase == null || !UnitMovedByMe.Vehicle.GetVehicleInfo().Flags.HasAnyFlag(VehicleFlags.FixedPosition))
            removeViolatingFlags(mi.HasMovementFlag(MovementFlag.Root), MovementFlag.Root);

        /*! This must be a packet spoofing attempt. MOVEMENTFLAG_ROOT sent from the client is not valid
            in conjunction with any of the moving movement flags such as MOVEMENTFLAG_FORWARD.
            It will freeze clients that receive this player's movement info.
        */
        removeViolatingFlags(mi.HasMovementFlag(MovementFlag.Root) && mi.HasMovementFlag(MovementFlag.MaskMoving), MovementFlag.MaskMoving);

        //! Cannot hover without SPELL_AURA_HOVER
        removeViolatingFlags(mi.HasMovementFlag(MovementFlag.Hover) && !UnitMovedByMe.HasAuraType(AuraType.Hover),
                             MovementFlag.Hover);

        //! Cannot ascend and descend at the same time
        removeViolatingFlags(mi.HasMovementFlag(MovementFlag.Ascending) && mi.HasMovementFlag(MovementFlag.Descending),
                             MovementFlag.Ascending | MovementFlag.Descending);

        //! Cannot move left and right at the same time
        removeViolatingFlags(mi.HasMovementFlag(MovementFlag.Left) && mi.HasMovementFlag(MovementFlag.Right),
                             MovementFlag.Left | MovementFlag.Right);

        //! Cannot strafe left and right at the same time
        removeViolatingFlags(mi.HasMovementFlag(MovementFlag.StrafeLeft) && mi.HasMovementFlag(MovementFlag.StrafeRight),
                             MovementFlag.StrafeLeft | MovementFlag.StrafeRight);

        //! Cannot pitch up and down at the same time
        removeViolatingFlags(mi.HasMovementFlag(MovementFlag.PitchUp) && mi.HasMovementFlag(MovementFlag.PitchDown),
                             MovementFlag.PitchUp | MovementFlag.PitchDown);

        //! Cannot move forwards and backwards at the same time
        removeViolatingFlags(mi.HasMovementFlag(MovementFlag.Forward) && mi.HasMovementFlag(MovementFlag.Backward),
                             MovementFlag.Forward | MovementFlag.Backward);

        //! Cannot walk on water without SPELL_AURA_WATER_WALK except for ghosts
        removeViolatingFlags(mi.HasMovementFlag(MovementFlag.WaterWalk) &&
                             !UnitMovedByMe.HasAuraType(AuraType.WaterWalk) &&
                             !UnitMovedByMe.HasAuraType(AuraType.Ghost),
                             MovementFlag.WaterWalk);

        //! Cannot feather fall without SPELL_AURA_FEATHER_FALL
        removeViolatingFlags(mi.HasMovementFlag(MovementFlag.FallingSlow) && !UnitMovedByMe.HasAuraType(AuraType.FeatherFall),
                             MovementFlag.FallingSlow);

        /*! Cannot fly if no fly auras present. Exception is being a GM.
            Note that we check for account level instead of Player.IsGameMaster() because in some
            situations it may be feasable to use .gm fly on as a GM without having .gm on,
            e.g. aerial combat.
        */

        removeViolatingFlags(mi.HasMovementFlag(MovementFlag.Flying | MovementFlag.CanFly) &&
                             Session.Security == AccountTypes.Player &&
                             !UnitMovedByMe.HasAuraType(AuraType.Fly) &&
                             !UnitMovedByMe.HasAuraType(AuraType.ModIncreaseMountedFlightSpeed),
                             MovementFlag.Flying | MovementFlag.CanFly);

        removeViolatingFlags(mi.HasMovementFlag(MovementFlag.DisableGravity | MovementFlag.CanFly) && mi.HasMovementFlag(MovementFlag.Falling),
                             MovementFlag.Falling);

        removeViolatingFlags(mi.HasMovementFlag(MovementFlag.SplineElevation) && MathFunctions.fuzzyEq(mi.StepUpStartElevation, 0.0f), MovementFlag.SplineElevation);

        // Client first checks if spline elevation != 0, then verifies Id presence
        if (MathFunctions.fuzzyNe(mi.StepUpStartElevation, 0.0f))
            mi.AddMovementFlag(MovementFlag.SplineElevation);
    }

    public void VehicleSpellInitialize()
    {
        var vehicle = VehicleCreatureBase;

        if (vehicle == null)
            return;

        PetSpells petSpells = new()
        {
            PetGUID = vehicle.GUID,
            CreatureFamily = 0, // Pet Family (0 for all vehicles)
            Specialization = 0,
            TimeLimit = vehicle.IsSummon ? vehicle.ToTempSummon().Timer : 0,
            ReactState = vehicle.ReactState,
            CommandState = CommandStates.Follow,
            Flag = 0x8
        };

        for (uint i = 0; i < SharedConst.MaxSpellControlBar; ++i)
            petSpells.ActionButtons[i] = UnitActionBarEntry.MAKE_UNIT_ACTION_BUTTON(0, i + 8);

        for (uint i = 0; i < SharedConst.MaxCreatureSpells; ++i)
        {
            var spellId = vehicle.Spells[i];
            var spellInfo = SpellManager.GetSpellInfo(spellId, Location.Map.DifficultyID);

            if (spellInfo == null)
                continue;

            if (spellInfo.HasAttribute(SpellAttr5.NotAvailableWhileCharmed))
                continue;

            if (!ConditionManager.IsObjectMeetingVehicleSpellConditions(vehicle.Entry, spellId, this, vehicle))
            {
                Log.Logger.Debug("VehicleSpellInitialize: conditions not met for Vehicle entry {0} spell {1}", vehicle.AsCreature.Entry, spellId);

                continue;
            }

            if (spellInfo.IsPassive)
                vehicle.SpellFactory.CastSpell(vehicle, spellInfo.Id, true);

            petSpells.ActionButtons[i] = UnitActionBarEntry.MAKE_UNIT_ACTION_BUTTON(spellId, i + 8);
        }

        // Cooldowns
        vehicle.
            // Cooldowns
            SpellHistory.WritePacket(petSpells);

        SendPacket(petSpells);
    }

    private void ApplyCustomConfigs()
    {
        // Adds the extra bag slots for having an authenticator.
        if (Configuration.GetDefaultValue("player:enableExtaBagSlots", false) && !HasPlayerLocalFlag(PlayerLocalFlags.AccountSecured))
            SetPlayerLocalFlag(PlayerLocalFlags.AccountSecured);

        if (Configuration.GetDefaultValue("player:addHearthstoneToCollection", false))
            Session.CollectionMgr.AddToy(193588, true, true);

        if (!Configuration.TryGetIfNotDefaultValue("AutoJoinChatChannel", "", out var chatChannel))
            return;

        var channelMgr = ChannelManagerFactory.ForTeam(Team);

        var channel = channelMgr.GetCustomChannel(chatChannel);

        if (channel != null)
            channel.JoinChannel(this);
        else
        {
            channel = channelMgr.CreateCustomChannel(chatChannel);

            channel?.JoinChannel(this);
        }
    }

    private void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedUnitMask, UpdateMask requestedPlayerMask, UpdateMask requestedActivePlayerMask, Player target)
    {
        var flags = GetUpdateFieldFlagsFor(target);
        UpdateMask valuesMask = new((int)TypeId.Max);

        if (requestedObjectMask.IsAnySet())
            valuesMask.Set((int)TypeId.Object);

        UnitData.FilterDisallowedFieldsMaskForFlag(requestedUnitMask, flags);

        if (requestedUnitMask.IsAnySet())
            valuesMask.Set((int)TypeId.Unit);

        PlayerData.FilterDisallowedFieldsMaskForFlag(requestedPlayerMask, flags);

        if (requestedPlayerMask.IsAnySet())
            valuesMask.Set((int)TypeId.Player);

        if (target == this && requestedActivePlayerMask.IsAnySet())
            valuesMask.Set((int)TypeId.ActivePlayer);

        WorldPacket buffer = new();
        buffer.WriteUInt32(valuesMask.GetBlock(0));

        if (valuesMask[(int)TypeId.Object])
            ObjectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

        if (valuesMask[(int)TypeId.Unit])
            UnitData.WriteUpdate(buffer, requestedUnitMask, true, this, target);

        if (valuesMask[(int)TypeId.Player])
            PlayerData.WriteUpdate(buffer, requestedPlayerMask, true, this, target);

        if (valuesMask[(int)TypeId.ActivePlayer])
            ActivePlayerData.WriteUpdate(buffer, requestedActivePlayerMask, true, this, target);

        WorldPacket buffer1 = new();
        buffer1.WriteUInt8((byte)UpdateType.Values);
        buffer1.WritePackedGuid(GUID);
        buffer1.WriteUInt32(buffer.GetSize());
        buffer1.WriteBytes(buffer.GetData());

        data.AddUpdateBlock(buffer1);
    }

    private int CalculateCorpseReclaimDelay(bool load = false)
    {
        var corpse = Corpse;

        if (load && corpse == null)
            return -1;

        var pvp = corpse != null ? corpse.GetCorpseType() == CorpseType.ResurrectablePVP : (_extraFlags & PlayerExtraFlags.PVPDeath) != 0;

        uint delay;

        if (load)
        {
            if (corpse.GetGhostTime() > _deathExpireTime)
                return -1;

            ulong count = 0;

            if ((pvp && Configuration.GetDefaultValue("Death:CorpseReclaimDelay:PvP", true)) ||
                (!pvp && Configuration.GetDefaultValue("Death:CorpseReclaimDelay:PvE", true)))
            {
                count = (ulong)(_deathExpireTime - corpse.GetGhostTime()) / PlayerConst.DeathExpireStep;

                if (count >= PlayerConst.MaxDeathCount)
                    count = PlayerConst.MaxDeathCount - 1;
            }

            var expectedTime = corpse.GetGhostTime() + PlayerConst.copseReclaimDelay[count];
            var now = GameTime.CurrentTime;

            if (now >= expectedTime)
                return -1;

            delay = (uint)(expectedTime - now);
        }
        else
            delay = GetCorpseReclaimDelay(pvp);

        return (int)(delay * Time.IN_MILLISECONDS);
    }

    private Corpse CreateCorpse()
    {
        // prevent existence 2 corpse for player
        SpawnCorpseBones();

        Corpse corpse = new(Convert.ToBoolean(_extraFlags & PlayerExtraFlags.PVPDeath) ? CorpseType.ResurrectablePVP : CorpseType.ResurrectablePVE);
        SetPvPDeath(false);

        if (!corpse.Create(Location.Map.GenerateLowGuid(HighGuid.Corpse), this))
            return null;

        CorpseLocation = new WorldLocation(Location);

        CorpseFlags flags = 0;

        if (HasPvpFlag(UnitPVPStateFlags.PvP))
            flags |= CorpseFlags.PvP;

        if (InBattleground && !InArena)
            flags |= CorpseFlags.Skinnable; // to be able to remove insignia

        if (HasPvpFlag(UnitPVPStateFlags.FFAPvp))
            flags |= CorpseFlags.FFAPvP;

        corpse.SetRace((byte)Race);
        corpse.SetSex((byte)NativeGender);
        corpse.SetClass((byte)Class);
        corpse.SetCustomizations(PlayerData.Customizations);
        corpse.ReplaceAllFlags(flags);
        corpse.SetDisplayId(NativeDisplayId);
        corpse.SetFactionTemplate(CliDB.ChrRacesStorage.LookupByKey(Race).FactionID);

        for (var i = EquipmentSlot.Start; i < EquipmentSlot.End; i++)
            if (_items[i] != null)
            {
                var itemDisplayId = _items[i].GetDisplayId(this);
                uint itemInventoryType;

                if (CliDB.ItemStorage.TryGetValue(_items[i].GetVisibleEntry(this), out var itemEntry))
                    itemInventoryType = (uint)itemEntry.inventoryType;
                else
                    itemInventoryType = (uint)_items[i].Template.InventoryType;

                corpse.SetItem(i, itemDisplayId | (itemInventoryType << 24));
            }

        // register for player, but not show
        Location.Map.AddCorpse(corpse);

        corpse.Location.UpdatePositionData();
        corpse.Location.SetZoneScript();

        // we do not need to save corpses for instances
        if (!Location.Map.Instanceable)
            corpse.SaveToDB();

        return corpse;
    }

    private void DeleteGarrison()
    {
        if (Garrison == null)
            return;

        Garrison.Delete();
        Garrison = null;
    }

    private double GetBaseModValue(BaseModGroup modGroup, BaseModType modType)
    {
        if (modGroup < BaseModGroup.End && modType < BaseModType.End)
            return modType == BaseModType.FlatMod ? _auraBaseFlatMod[(int)modGroup] : _auraBasePctMod[(int)modGroup];

        Log.Logger.Error($"Player.GetBaseModValue: Invalid BaseModGroup/BaseModType ({modGroup}/{modType}) for player '{GetName()}' ({GUID})");

        return 0.0f;
    }

    private uint GetChampioningFaction()
    {
        return _championingFaction;
    }

    private uint GetCurrencyIncreasedCapQuantity(uint id)
    {
        var playerCurrency = _currencyStorage.LookupByKey(id);

        return playerCurrency?.IncreasedCapQuantity ?? 0;
    }

    private uint GetCurrencyWeeklyCap(CurrencyTypesRecord currency)
    {
        // TODO: CurrencyTypeFlags::ComputedWeeklyMaximum
        return currency.MaxEarnablePerWeek;
    }

    private SpellSchoolMask GetEnviormentDamageType(EnviromentalDamage dmgType)
    {
        return dmgType switch
        {
            EnviromentalDamage.Lava  => SpellSchoolMask.Fire,
            EnviromentalDamage.Fire  => SpellSchoolMask.Fire,
            EnviromentalDamage.Slime => SpellSchoolMask.Nature,
            _                        => SpellSchoolMask.Normal
        };
    }

    private int GetMaxTimer(MirrorTimerType timer)
    {
        switch (timer)
        {
            case MirrorTimerType.Fatigue:
                return Time.MINUTE * Time.IN_MILLISECONDS;

            case MirrorTimerType.Breath:
            {
                if (!IsAlive || HasAuraType(AuraType.WaterBreathing) || Session.Security >= (AccountTypes)Configuration.GetDefaultValue("DisableWaterBreath", (int)AccountTypes.Console))
                    return -1;

                var underWaterTime = 3 * Time.MINUTE * Time.IN_MILLISECONDS;
                underWaterTime *= (int)GetTotalAuraMultiplier(AuraType.ModWaterBreathing);

                return underWaterTime;
            }
            case MirrorTimerType.Fire:
            {
                if (!IsAlive)
                    return -1;

                return 1 * Time.IN_MILLISECONDS;
            }
            default:
                return 0;
        }
    }

    private double GetTotalBaseModValue(BaseModGroup modGroup)
    {
        if (modGroup < BaseModGroup.End)
            return _auraBaseFlatMod[(int)modGroup] * _auraBasePctMod[(int)modGroup];

        Log.Logger.Error($"Player.GetTotalBaseModValue: Invalid BaseModGroup ({modGroup}) for player '{GetName()}' ({GUID})");

        return 0.0f;
    }

    private void HandleDrowning(uint timeDiff)
    {
        if (_mirrorTimerFlags == 0)
            return;

        var breathTimer = (int)MirrorTimerType.Breath;
        var fatigueTimer = (int)MirrorTimerType.Fatigue;
        var fireTimer = (int)MirrorTimerType.Fire;

        // In water
        if (_mirrorTimerFlags.HasAnyFlag(PlayerUnderwaterState.InWater))
        {
            // Breath timer not activated - activate it
            if (_mirrorTimer[breathTimer] == -1)
            {
                _mirrorTimer[breathTimer] = GetMaxTimer(MirrorTimerType.Breath);
                SendMirrorTimer(MirrorTimerType.Breath, _mirrorTimer[breathTimer], _mirrorTimer[breathTimer], -1);
            }
            else // If activated - do tick
            {
                _mirrorTimer[breathTimer] -= (int)timeDiff;

                // Timer limit - need deal damage
                if (_mirrorTimer[breathTimer] < 0)
                {
                    _mirrorTimer[breathTimer] += 1 * Time.IN_MILLISECONDS;
                    // Calculate and deal damage
                    // @todo Check this formula
                    var damage = (uint)(MaxHealth / 5 + RandomHelper.URand(0, Level - 1));
                    EnvironmentalDamage(EnviromentalDamage.Drowning, damage);
                }
                else if (!_mirrorTimerFlagsLast.HasAnyFlag(PlayerUnderwaterState.InWater)) // Update time in client if need
                    SendMirrorTimer(MirrorTimerType.Breath, GetMaxTimer(MirrorTimerType.Breath), _mirrorTimer[breathTimer], -1);
            }
        }
        else if (_mirrorTimer[breathTimer] != -1) // Regen timer
        {
            var underWaterTime = GetMaxTimer(MirrorTimerType.Breath);
            // Need breath regen
            _mirrorTimer[breathTimer] += (int)(10 * timeDiff);

            if (_mirrorTimer[breathTimer] >= underWaterTime || !IsAlive)
                StopMirrorTimer(MirrorTimerType.Breath);
            else if (_mirrorTimerFlagsLast.HasAnyFlag(PlayerUnderwaterState.InWater))
                SendMirrorTimer(MirrorTimerType.Breath, underWaterTime, _mirrorTimer[breathTimer], 10);
        }

        // In dark water
        if (_mirrorTimerFlags.HasAnyFlag(PlayerUnderwaterState.InDarkWater))
        {
            // Fatigue timer not activated - activate it
            if (_mirrorTimer[fatigueTimer] == -1)
            {
                _mirrorTimer[fatigueTimer] = GetMaxTimer(MirrorTimerType.Fatigue);
                SendMirrorTimer(MirrorTimerType.Fatigue, _mirrorTimer[fatigueTimer], _mirrorTimer[fatigueTimer], -1);
            }
            else
            {
                _mirrorTimer[fatigueTimer] -= (int)timeDiff;

                // Timer limit - need deal damage or teleport ghost to graveyard
                if (_mirrorTimer[fatigueTimer] < 0)
                {
                    _mirrorTimer[fatigueTimer] += 1 * Time.IN_MILLISECONDS;

                    if (IsAlive) // Calculate and deal damage
                    {
                        var damage = (uint)(MaxHealth / 5 + RandomHelper.URand(0, Level - 1));
                        EnvironmentalDamage(EnviromentalDamage.Exhausted, damage);
                    }
                    else if (HasPlayerFlag(PlayerFlags.Ghost)) // Teleport ghost to graveyard
                        RepopAtGraveyard();
                }
                else if (!_mirrorTimerFlagsLast.HasAnyFlag(PlayerUnderwaterState.InDarkWater))
                    SendMirrorTimer(MirrorTimerType.Fatigue, GetMaxTimer(MirrorTimerType.Fatigue), _mirrorTimer[fatigueTimer], -1);
            }
        }
        else if (_mirrorTimer[fatigueTimer] != -1) // Regen timer
        {
            var darkWaterTime = GetMaxTimer(MirrorTimerType.Fatigue);
            _mirrorTimer[fatigueTimer] += (int)(10 * timeDiff);

            if (_mirrorTimer[fatigueTimer] >= darkWaterTime || !IsAlive)
                StopMirrorTimer(MirrorTimerType.Fatigue);
            else if (_mirrorTimerFlagsLast.HasAnyFlag(PlayerUnderwaterState.InDarkWater))
                SendMirrorTimer(MirrorTimerType.Fatigue, darkWaterTime, _mirrorTimer[fatigueTimer], 10);
        }

        if (_mirrorTimerFlags.HasAnyFlag(PlayerUnderwaterState.InLava) && !(LastLiquid != null && LastLiquid.SpellID != 0))
        {
            // Breath timer not activated - activate it
            if (_mirrorTimer[fireTimer] == -1)
                _mirrorTimer[fireTimer] = GetMaxTimer(MirrorTimerType.Fire);
            else
            {
                _mirrorTimer[fireTimer] -= (int)timeDiff;

                if (_mirrorTimer[fireTimer] < 0)
                {
                    _mirrorTimer[fireTimer] += 1 * Time.IN_MILLISECONDS;
                    // Calculate and deal damage
                    // @todo Check this formula
                    var damage = RandomHelper.URand(600, 700);

                    if (_mirrorTimerFlags.HasAnyFlag(PlayerUnderwaterState.InLava))
                        EnvironmentalDamage(EnviromentalDamage.Lava, damage);
                    // need to skip Slime damage in Undercity,
                    // maybe someone can find better way to handle environmental damage
                    //else if (m_zoneUpdateId != 1497)
                    //    EnvironmentalDamage(DAMAGE_SLIME, damage);
                }
            }
        }
        else
            _mirrorTimer[fireTimer] = -1;

        // Recheck timers Id
        _mirrorTimerFlags &= ~PlayerUnderwaterState.ExistTimers;

        for (byte i = 0; i < (int)MirrorTimerType.Max; ++i)
            if (_mirrorTimer[i] != -1)
            {
                _mirrorTimerFlags |= PlayerUnderwaterState.ExistTimers;

                break;
            }

        _mirrorTimerFlagsLast = _mirrorTimerFlags;
    }

    private void HandleSobering()
    {
        _drunkTimer = 0;

        var currentDrunkValue = DrunkValue;
        var drunk = (byte)(currentDrunkValue != 0 ? --currentDrunkValue : 0);
        SetDrunkValue(drunk);
    }

    private void InitPrimaryProfessions()
    {
        SetFreePrimaryProfessions(Configuration.GetDefaultValue("MaxPrimaryTradeSkill", 2u));
    }

    private bool IsActionButtonDataValid(byte button, ulong action, uint type)
    {
        if (button >= PlayerConst.MaxActionButtons)
        {
            Log.Logger.Error($"Player::IsActionButtonDataValid: Action {action} not added into button {button} for player {GetName()} ({GUID}): button must be < {PlayerConst.MaxActionButtons}");

            return false;
        }

        if (action >= PlayerConst.MaxActionButtonActionValue)
        {
            Log.Logger.Error($"Player::IsActionButtonDataValid: Action {action} not added into button {button} for player {GetName()} ({GUID}): action must be < {PlayerConst.MaxActionButtonActionValue}");

            return false;
        }

        switch ((ActionButtonType)type)
        {
            case ActionButtonType.Spell:
                if (!SpellManager.HasSpellInfo((uint)action))
                {
                    Log.Logger.Error($"Player::IsActionButtonDataValid: Spell action {action} not added into button {button} for player {GetName()} ({GUID}): spell not exist");

                    return false;
                }

                break;

            case ActionButtonType.Item:
                if (GameObjectManager.GetItemTemplate((uint)action) == null)
                {
                    Log.Logger.Error($"Player::IsActionButtonDataValid: Item action {action} not added into button {button} for player {GetName()} ({GUID}): item not exist");

                    return false;
                }

                break;

            case ActionButtonType.Companion:
            {
                if (Session.BattlePetMgr.GetPet(ObjectGuid.Create(HighGuid.BattlePet, action)) == null)
                {
                    Log.Logger.Error($"Player::IsActionButtonDataValid: Companion action {action} not added into button {button} for player {GetName()} ({GUID}): companion does not exist");

                    return false;
                }

                break;
            }
            case ActionButtonType.Mount:
                if (!CliDB.MountStorage.TryGetValue((uint)action, out var mount))
                {
                    Log.Logger.Error($"Player::IsActionButtonDataValid: Mount action {action} not added into button {button} for player {GetName()} ({GUID}): mount does not exist");

                    return false;
                }

                if (!HasSpell(mount.SourceSpellID))
                {
                    Log.Logger.Error($"Player::IsActionButtonDataValid: Mount action {action} not added into button {button} for player {GetName()} ({GUID}): Player does not know this mount");

                    return false;
                }

                break;

            case ActionButtonType.C:
            case ActionButtonType.CMacro:
            case ActionButtonType.Macro:
            case ActionButtonType.Eqset:
                break;

            default:
                Log.Logger.Error($"Unknown action type {type}");

                return false; // other cases not checked at this moment
        }

        return true;
    }

    private bool IsAtRecruitAFriendDistance(WorldObject pOther)
    {
        if (pOther == null || !Location.IsInMap(pOther))
            return false;

        WorldObject player = Corpse;

        if (player == null || IsAlive)
            player = this;

        return pOther.Location.GetDistance(player) <= Configuration.GetDefaultValue("MaxRecruitAFriendBonusDistance", 100.0f);
    }

    private bool IsFriendlyArea(AreaTableRecord areaEntry)
    {
        var factionTemplate = WorldObjectCombat.GetFactionTemplateEntry();

        if (factionTemplate == null)
            return false;

        return (factionTemplate.FriendGroup & areaEntry.FactionGroupMask) != 0;
    }

    private bool IsImmuneToEnvironmentalDamage()
    {
        // check for GM and death state included in isAttackableByAOE
        return !IsTargetableForAttack(false);
    }

    private void Regenerate(PowerType power)
    {
        // Skip regeneration for power type we cannot have
        var powerIndex = GetPowerIndex(power);

        if (powerIndex is (int)PowerType.Max or >= (int)PowerType.MaxPerClass)
            return;

        // @todo possible use of miscvalueb instead of amount
        if (HasAuraTypeWithValue(AuraType.PreventRegeneratePower, (int)power))
            return;

        var curValue = GetPower(power);

        // TODO: updating haste should update UNIT_FIELD_POWER_REGEN_FLAT_MODIFIER for certain power types
        var powerType = DB2Manager.GetPowerTypeEntry(power);

        if (powerType == null)
            return;

        double addvalue;

        if (!IsInCombat)
        {
            if (powerType.RegenInterruptTimeMS != 0 && Time.GetMSTimeDiffToNow(_combatExitTime) < powerType.RegenInterruptTimeMS)
                return;

            addvalue = (powerType.RegenPeace + UnitData.PowerRegenFlatModifier[(int)powerIndex]) * 0.001f * RegenTimer;
        }
        else
            addvalue = (powerType.RegenCombat + UnitData.PowerRegenInterruptedFlatModifier[(int)powerIndex]) * 0.001f * RegenTimer;

        string[] ratesForPower =
        {
            "Rate.Mana", "Rate.Rage.Loss", "Rate.Focus", "Rate.Energy", "Rate.ComboPoints.Loss", "Rate.RunicPower.Gain", // runes
            "Rate.RunicPower.Loss", "Rate.SoulShards.Loss", "Rate.LunarPower.Loss", "Rate.HolyPower.Loss", "0",          // alternate
            "Rate.Maelstrom.Loss", "Rate.Chi.Loss", "Rate.Insanity.Loss", "0",                                           // burning embers, unused
            "0",                                                                                                         // demonic fury, unused
            "Rate.ArcaneCharges.Loss", "Rate.Fury.Loss", "Rate.Pain.Loss", "0"                                           // todo add config for Essence power
        };

        if (ratesForPower[(int)power] != "0")
        {
            var rate = Configuration.GetDefaultValue(ratesForPower[(int)power], 0f);

            if (rate != 0)
                addvalue *= rate;
        }

        // Mana regen calculated in Player.UpdateManaRegen()
        if (power != PowerType.Mana)
        {
            addvalue *= GetTotalAuraMultiplierByMiscValue(AuraType.ModPowerRegenPercent, (int)power);
            addvalue += GetTotalAuraModifierByMiscValue(AuraType.ModPowerRegen, (int)power) * (power != PowerType.Energy ? _regenTimerCount : RegenTimer) / (5 * Time.IN_MILLISECONDS);
        }

        var minPower = powerType.MinPower;
        var maxPower = GetMaxPower(power);

        if (powerType.CenterPower != 0)
        {
            if (curValue > powerType.CenterPower)
            {
                addvalue = -Math.Abs(addvalue);
                minPower = powerType.CenterPower;
            }
            else if (curValue < powerType.CenterPower)
            {
                addvalue = Math.Abs(addvalue);
                maxPower = powerType.CenterPower;
            }
            else
                return;
        }

        addvalue += _powerFraction[powerIndex];
        var integerValue = (int)Math.Abs(addvalue);

        var forcesSetPower = false;

        if (addvalue < 0.0f)
        {
            if (curValue <= minPower)
                return;
        }
        else if (addvalue > 0.0f)
        {
            if (curValue >= maxPower)
                return;
        }
        else
            return;

        if (addvalue < 0.0f)
        {
            if (curValue > minPower + integerValue)
            {
                curValue -= integerValue;
                _powerFraction[powerIndex] = addvalue + integerValue;
            }
            else
            {
                curValue = minPower;
                _powerFraction[powerIndex] = 0;
                forcesSetPower = true;
            }
        }
        else
        {
            if (curValue + integerValue <= maxPower)
            {
                curValue += integerValue;
                _powerFraction[powerIndex] = addvalue - integerValue;
            }
            else
            {
                curValue = maxPower;
                _powerFraction[powerIndex] = 0;
                forcesSetPower = true;
            }
        }

        if (GetCommandStatus(PlayerCommandStates.Power))
            curValue = maxPower;

        if (_regenTimerCount >= 2000 || forcesSetPower)
            SetPower(power, curValue, true, true);
        else
            // throttle packet sending
            DoWithSuppressingObjectUpdates(() =>
            {
                SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.Power, (int)powerIndex), curValue);
                UnitData.ClearChanged(UnitData.Power, (int)powerIndex);
            });
    }

    private void RegenerateAll()
    {
        _regenTimerCount += RegenTimer;
        _foodEmoteTimerCount += RegenTimer;

        for (var power = PowerType.Mana; power < PowerType.Max; power++) // = power + 1)
            if (power != PowerType.Runes)
                Regenerate(power);

        // Runes act as cooldowns, and they don't need to send any data
        if (Class == PlayerClass.Deathknight)
        {
            uint regeneratedRunes = 0;
            var regenIndex = 0;

            while (regeneratedRunes < PlayerConst.MaxRechargingRunes && _runes.CooldownOrder.Count > regenIndex)
            {
                var runeToRegen = _runes.CooldownOrder[regenIndex];
                var runeCooldown = GetRuneCooldown(runeToRegen);

                if (runeCooldown > RegenTimer)
                {
                    SetRuneCooldown(runeToRegen, runeCooldown - RegenTimer);
                    ++regenIndex;
                }
                else
                    SetRuneCooldown(runeToRegen, 0);

                ++regeneratedRunes;
            }
        }

        if (_regenTimerCount >= 2000)
        {
            // Not in combat or they have regeneration
            if (!IsInCombat || IsPolymorphed || _baseHealthRegen != 0 || HasAuraType(AuraType.ModRegenDuringCombat) || HasAuraType(AuraType.ModHealthRegenInCombat))
                RegenerateHealth();

            _regenTimerCount -= 2000;
        }

        RegenTimer = 0;

        // Handles the emotes for drinking and eating.
        // According to sniffs there is a background timer going on that repeats independed from the time window where the aura applies.
        // That's why we dont need to reset the timer on apply. In sniffs I have seen that the first call for the spell visual is totally random, then after
        // 5 seconds over and over again which confirms my theory that we have a independed timer.
        if (_foodEmoteTimerCount >= 5000)
        {
            var auraList = GetAuraEffectsByType(AuraType.ModRegen);
            auraList.AddRange(GetAuraEffectsByType(AuraType.ModPowerRegen));

            foreach (var auraEffect in auraList)
                // Food emote comes above drinking emote if we have to decide (mage regen food for example)
                if (auraEffect.Base.HasEffectType(AuraType.ModRegen) && auraEffect.SpellInfo.HasAuraInterruptFlag(SpellAuraInterruptFlags.Standing))
                {
                    WorldObjectCombat.SendPlaySpellVisualKit(SpellConst.VisualKitFood, 0, 0);

                    break;
                }
                else if (auraEffect.Base.HasEffectType(AuraType.ModPowerRegen) && auraEffect.SpellInfo.HasAuraInterruptFlag(SpellAuraInterruptFlags.Standing))
                {
                    WorldObjectCombat.SendPlaySpellVisualKit(SpellConst.VisualKitDrink, 0, 0);

                    break;
                }

            _foodEmoteTimerCount -= 5000;
        }
    }

    private void RegenerateHealth()
    {
        var curValue = Health;
        var maxValue = MaxHealth;

        if (curValue >= maxValue)
            return;

        var healthIncreaseRate = Configuration.GetDefaultValue("Rate:Health", 1.0f);
        double addValue = 0.0f;

        // polymorphed case
        if (IsPolymorphed)
            addValue = MaxHealth / 3f;
        // normal regen case (maybe partly in combat case)
        else if (!IsInCombat || HasAuraType(AuraType.ModRegenDuringCombat))
        {
            addValue = healthIncreaseRate;

            if (!IsInCombat)
            {
                if (Level < 15)
                    addValue = 0.20f * MaxHealth / Level * healthIncreaseRate;
                else
                    addValue = 0.015f * MaxHealth * healthIncreaseRate;

                addValue *= GetTotalAuraMultiplier(AuraType.ModHealthRegenPercent);
                addValue += GetTotalAuraModifier(AuraType.ModRegen) * 2 * Time.IN_MILLISECONDS / (5 * Time.IN_MILLISECONDS);
            }
            else if (HasAuraType(AuraType.ModRegenDuringCombat))
                MathFunctions.ApplyPct(ref addValue, GetTotalAuraModifier(AuraType.ModRegenDuringCombat));

            if (!IsStandState)
                addValue *= 1.5f;
        }

        // always regeneration bonus (including combat)
        addValue += GetTotalAuraModifier(AuraType.ModHealthRegenInCombat);
        addValue += _baseHealthRegen / 2.5f;

        if (addValue < 0)
            addValue = 0;

        ModifyHealth(addValue);
    }

    private void ResurrectUsingRequestDataImpl()
    {
        // save health and mana before resurrecting, _resurrectionData can be erased
        var resurrectHealth = _resurrectionData.Health;
        var resurrectMana = _resurrectionData.Mana;
        var resurrectAura = _resurrectionData.Aura;
        var resurrectGUID = _resurrectionData.Guid;

        ResurrectPlayer(0.0f);

        SetHealth(resurrectHealth);
        SetPower(PowerType.Mana, (int)resurrectMana);

        SetPower(PowerType.Rage, 0);
        SetFullPower(PowerType.Energy);
        SetFullPower(PowerType.Focus);
        SetPower(PowerType.LunarPower, 0);

        if (resurrectAura != 0)
            SpellFactory.CastSpell(this, resurrectAura, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCaster(resurrectGUID));

        SpawnCorpseBones();
    }

    // Calculate how many reputation points player gain with the quest
    private void RewardReputation(Quest.Quest quest)
    {
        for (byte i = 0; i < SharedConst.QuestRewardReputationsCount; ++i)
        {
            if (quest.RewardFactionId[i] == 0)
                continue;

            if (!CliDB.FactionStorage.TryGetValue(quest.RewardFactionId[i], out var factionEntry))
                continue;

            var rep = 0;
            var noQuestBonus = false;

            if (quest.RewardFactionOverride[i] != 0)
            {
                rep = quest.RewardFactionOverride[i] / 100;
                noQuestBonus = true;
            }
            else
            {
                var row = (uint)(quest.RewardFactionValue[i] < 0 ? 1 : 0) + 1;

                if (CliDB.QuestFactionRewardStorage.TryGetValue(row, out var questFactionRewEntry))
                {
                    var field = (uint)Math.Abs(quest.RewardFactionValue[i]);
                    rep = questFactionRewEntry.Difficulty[field];
                }
            }

            if (rep == 0)
                continue;

            if (quest.RewardFactionCapIn[i] != 0 && rep > 0 && (int)ReputationMgr.GetRank(factionEntry) >= quest.RewardFactionCapIn[i])
                continue;

            if (quest.IsDaily)
                rep = CalculateReputationGain(ReputationSource.DailyQuest, (uint)GetQuestLevel(quest), rep, (int)quest.RewardFactionId[i], noQuestBonus);
            else if (quest.IsWeekly)
                rep = CalculateReputationGain(ReputationSource.WeeklyQuest, (uint)GetQuestLevel(quest), rep, (int)quest.RewardFactionId[i], noQuestBonus);
            else if (quest.IsMonthly)
                rep = CalculateReputationGain(ReputationSource.MonthlyQuest, (uint)GetQuestLevel(quest), rep, (int)quest.RewardFactionId[i], noQuestBonus);
            else if (quest.IsRepeatable)
                rep = CalculateReputationGain(ReputationSource.RepeatableQuest, (uint)GetQuestLevel(quest), rep, (int)quest.RewardFactionId[i], noQuestBonus);
            else
                rep = CalculateReputationGain(ReputationSource.Quest, (uint)GetQuestLevel(quest), rep, (int)quest.RewardFactionId[i], noQuestBonus);

            var noSpillover = Convert.ToBoolean(quest.RewardReputationMask & (1 << i));
            ReputationMgr.ModifyReputation(factionEntry, rep, false, noSpillover);
        }
    }

    private void ScheduleDelayedOperation(PlayerDelayedOperations operation)
    {
        if (operation < PlayerDelayedOperations.End)
            _delayedOperations |= operation;
    }

    private void SendActionButtons(uint state)
    {
        UpdateActionButtons packet = new();

        foreach (var pair in _actionButtons)
            if (pair.Value.UState != ActionButtonUpdateState.Deleted && pair.Key < packet.ActionButtons.Length)
                packet.ActionButtons[pair.Key] = pair.Value.PackedData;

        packet.Reason = (byte)state;
        SendPacket(packet);
    }

    private void SendAttackSwingBadFacingAttack()
    {
        SendPacket(new AttackSwingError(AttackSwingErr.BadFacing));
    }

    private void SendAurasForTarget(Unit target)
    {
        if (target == null || target.VisibleAuras.Empty()) // speedup things
            return;

        var visibleAuras = target.VisibleAuras;

        AuraUpdate update = new()
        {
            UpdateAll = true,
            UnitGUID = target.GUID
        };

        foreach (var auraApp in visibleAuras.ToList())
        {
            AuraInfo auraInfo = new();
            auraApp.BuildUpdatePacket(ref auraInfo, false);
            update.Auras.Add(auraInfo);
        }

        SendPacket(update);
    }

    private void SendCorpseReclaimDelay(int delay)
    {
        CorpseReclaimDelay packet = new()
        {
            Remaining = (uint)delay
        };

        SendPacket(packet);
    }

    private void SendInitialActionButtons()
    {
        SendActionButtons(0);
    }

    private void SendInitWorldStates(uint zoneId, uint areaId)
    {
        // data depends on zoneid/mapId..
        var mapid = Location.MapId;

        InitWorldStates packet = new()
        {
            MapID = mapid,
            AreaID = zoneId,
            SubareaID = areaId
        };

        WorldStateManager.FillInitialWorldStates(packet, Location.Map, areaId);

        SendPacket(packet);
    }

    private void SendMirrorTimer(MirrorTimerType type, int maxValue, int currentValue, int regen)
    {
        if (maxValue == -1)
        {
            if (currentValue != -1)
                StopMirrorTimer(type);

            return;
        }

        SendPacket(new StartMirrorTimer(type, currentValue, maxValue, regen, 0, false));
    }

    private void SendNewMail()
    {
        SendPacket(new NotifyReceivedMail());
    }

    private void SetActiveCombatTraitConfigID(int traitConfigId)
    {
        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ActiveCombatTraitConfigID), (uint)traitConfigId);
    }

    private void SetCanDelayTeleport(bool setting)
    {
        IsCanDelayTeleport = setting;
    }

    private void SetDelayedTeleportFlag(bool setting)
    {
        IsHasDelayedTeleport = setting;
    }

    private void SetWarModeLocal(bool enabled)
    {
        if (enabled)
            SetPlayerLocalFlag(PlayerLocalFlags.WarMode);
        else
            RemovePlayerLocalFlag(PlayerLocalFlags.WarMode);
    }

    private void StopMirrorTimer(MirrorTimerType type)
    {
        _mirrorTimer[(int)type] = -1;
        SendPacket(new StopMirrorTimer(type));
    }

    private void UpdateBaseModGroup(BaseModGroup modGroup)
    {
        if (!CanModifyStats())
            return;

        switch (modGroup)
        {
            case BaseModGroup.CritPercentage:
                UpdateCritPercentage(WeaponAttackType.BaseAttack);

                break;

            case BaseModGroup.RangedCritPercentage:
                UpdateCritPercentage(WeaponAttackType.RangedAttack);

                break;

            case BaseModGroup.OffhandCritPercentage:
                UpdateCritPercentage(WeaponAttackType.OffAttack);

                break;
        }
    }

    private void UpdateCorpseReclaimDelay()
    {
        var pvp = _extraFlags.HasAnyFlag(PlayerExtraFlags.PVPDeath);

        if ((pvp && !Configuration.GetDefaultValue("Death:CorpseReclaimDelay:PvP", true)) ||
            (!pvp && !Configuration.GetDefaultValue("Death:CorpseReclaimDelay:PvE", true)))
            return;

        var now = GameTime.CurrentTime;

        if (now < _deathExpireTime)
        {
            // full and partly periods 1..3
            var count = (ulong)(_deathExpireTime - now) / PlayerConst.DeathExpireStep + 1;

            if (count < PlayerConst.MaxDeathCount)
                _deathExpireTime = now + (long)(count + 1) * PlayerConst.DeathExpireStep;
            else
                _deathExpireTime = now + PlayerConst.MaxDeathCount * PlayerConst.DeathExpireStep;
        }
        else
            _deathExpireTime = now + PlayerConst.DeathExpireStep;
    }

    private void UpdateHomebindTime(uint time)
    {
        // GMs never get homebind timer online
        if (InstanceValid || IsGameMaster)
        {
            if (_homebindTimer != 0) // instance valid, but timer not reset
                SendRaidGroupOnlyMessage(RaidGroupReason.None, 0);

            // instance is valid, reset homebind timer
            _homebindTimer = 0;
        }
        else if (_homebindTimer > 0)
        {
            if (time >= _homebindTimer)
                // teleport to nearest graveyard
                RepopAtGraveyard();
            else
                _homebindTimer -= time;
        }
        else
        {
            // instance is invalid, start homebind timer
            _homebindTimer = 60000;
            // send message to player
            SendRaidGroupOnlyMessage(RaidGroupReason.RequirementsUnmatch, (int)_homebindTimer);
            Log.Logger.Debug("PLAYER: Player '{0}' (GUID: {1}) will be teleported to homebind in 60 seconds", GetName(), GUID.ToString());
        }
    }

    private void UpdateLocalChannels(uint newZone)
    {
        if (Session.PlayerLoading && !IsBeingTeleportedFar)
            return; // The client handles it automatically after loading, but not after teleporting

        if (!CliDB.AreaTableStorage.TryGetValue(newZone, out var currentZone))
            return;

        var cMgr = ChannelManagerFactory.ForTeam(Team);

        if (cMgr == null)
            return;

        foreach (var channelEntry in CliDB.ChatChannelsStorage.Values)
        {
            if (!channelEntry.Flags.HasAnyFlag(ChannelDBCFlags.Initial))
                continue;

            Channel usedChannel = null;

            foreach (var channel in JoinedChannels)
                if (channel.ChannelId == channelEntry.Id)
                {
                    usedChannel = channel;

                    break;
                }

            Channel removeChannel = null;
            Channel joinChannel = null;
            var sendRemove = true;

            if (CanJoinConstantChannelInZone(channelEntry, currentZone))
            {
                if (!channelEntry.Flags.HasAnyFlag(ChannelDBCFlags.Global))
                {
                    if (channelEntry.Flags.HasAnyFlag(ChannelDBCFlags.CityOnly) && usedChannel != null)
                        continue; // Already on the channel, as city channel names are not changing

                    joinChannel = cMgr.GetSystemChannel(channelEntry.Id, currentZone);

                    if (usedChannel != null)
                    {
                        if (joinChannel != usedChannel)
                        {
                            removeChannel = usedChannel;
                            sendRemove = false; // Do not send leave channel, it already replaced at client
                        }
                        else
                            joinChannel = null;
                    }
                }
                else
                    joinChannel = cMgr.GetSystemChannel(channelEntry.Id);
            }
            else
                removeChannel = usedChannel;

            joinChannel?.JoinChannel(this); // Changed Channel: ... or Joined Channel: ...

            if (removeChannel == null)
                continue;

            removeChannel.LeaveChannel(this, sendRemove, true); // Leave old channel

            LeftChannel(removeChannel);                                         // Remove from player's channel list
            cMgr.LeftChannel(removeChannel.ChannelId, removeChannel.ZoneEntry); // Delete if empty
        }
    }

    private void UpdateWarModeAuras()
    {
        uint auraInside = 282559;
        var auraOutside = PlayerConst.WarmodeEnlistedSpellOutside;

        if (IsWarModeDesired)
        {
            if (CanEnableWarModeInArea())
            {
                RemovePlayerFlag(PlayerFlags.WarModeActive);
                SpellFactory.CastSpell(this, auraInside, true);
                RemoveAura(auraOutside);
            }
            else
            {
                SetPlayerFlag(PlayerFlags.WarModeActive);
                SpellFactory.CastSpell(this, auraOutside, true);
                RemoveAura(auraInside);
            }

            SetWarModeLocal(true);
            SetPvpFlag(UnitPVPStateFlags.PvP);
        }
        else
        {
            SetWarModeLocal(false);
            RemoveAura(auraOutside);
            RemoveAura(auraInside);
            RemovePlayerFlag(PlayerFlags.WarModeActive);
            RemovePvpFlag(UnitPVPStateFlags.PvP);
        }
    }

    #region Sends / Updates

    public void AddExploredZones(uint pos, ulong mask)
    {
        SetUpdateFieldFlagValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ExploredZones, (int)pos), mask);
    }

    public void RemoveExploredZones(uint pos, ulong mask)
    {
        RemoveUpdateFieldFlagValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ExploredZones, (int)pos), mask);
    }

    public void ResetCurrencyWeekCap()
    {
        for (byte arenaSlot = 0; arenaSlot < 3; arenaSlot++)
        {
            var arenaTeamId = GetArenaTeamId(arenaSlot);

            if (arenaTeamId == 0)
                continue;

            var arenaTeam = ArenaTeamManager.GetArenaTeamById(arenaTeamId);
            arenaTeam.FinishWeek();         // set played this week etc values to 0 in memory, too
            arenaTeam.SaveToDB();           // save changes
            arenaTeam.NotifyStatsChanged(); // notify the players of the changes
        }

        foreach (var currency in _currencyStorage.Values)
        {
            currency.WeeklyQuantity = 0;
            currency.State = PlayerCurrencyState.Changed;
        }

        SendPacket(new ResetWeeklyCurrency());
    }

    public void SendBuyError(BuyResult msg, Creature creature, uint item)
    {
        SendPacket(new BuyFailed
        {
            VendorGUID = creature?.GUID ?? ObjectGuid.Empty,
            Muid = item,
            Reason = msg
        });
    }

    public void SendInitialVisiblePackets(Unit target)
    {
        SendAurasForTarget(target);

        if (target.IsAlive)
            if (target.HasUnitState(UnitState.MeleeAttacking) && target.Victim != null)
                target.SendMeleeAttackStart(target.Victim);
    }

    public override void SendMessageToSet(ServerPacket data, Player skippedRcvr)
    {
        if (skippedRcvr != this)
            SendPacket(data);

        // we use World.GetMaxVisibleDistance() because i cannot see why not use a distance
        // update: replaced by GetMap().GetVisibilityDistance()
        PacketSenderRef sender = new(data);
        var notifier = new MessageDistDeliverer<PacketSenderRef>(this, sender, Visibility.VisibilityRange, false, skippedRcvr);
        CellCalculator.VisitGrid(this, notifier, Visibility.VisibilityRange);
    }

    public override void SendMessageToSet(ServerPacket data, bool self)
    {
        SendMessageToSetInRange(data, Visibility.VisibilityRange, self);
    }

    public override void SendMessageToSetInRange(ServerPacket data, float dist, bool self)
    {
        if (self)
            SendPacket(data);

        PacketSenderRef sender = new(data);
        var notifier = new MessageDistDeliverer<PacketSenderRef>(this, sender, dist);
        CellCalculator.VisitGrid(this, notifier, dist);
    }

    public void SendSellError(SellResult msg, Creature creature, ObjectGuid guid)
    {
        SendPacket(new SellResponse
        {
            VendorGUID = creature?.GUID ?? ObjectGuid.Empty,
            ItemGUID = guid,
            Reason = msg
        });
    }

    public void SendSysMessage(CypherStrings str, params object[] args)
    {
        SendSysMessage((uint)str, args);
    }

    public void SendSysMessage(uint str, params object[] args)
    {
        var input = GameObjectManager.GetCypherString(str);
        var pattern = @"%(\d+(\.\d+)?)?(d|f|s|u)";

        var count = 0;
        var result = Regex.Replace(input, pattern, _ => string.Concat("{", count++, "}"));

        SendSysMessage(result, args);
    }

    public void SendSysMessage(string str, params object[] args)
    {
        new CommandHandler(ClassFactory, Session).SendSysMessage(string.Format(str, args));
    }

    public void SetSeer(WorldObject target)
    {
        SeerView = target;
    }

    public override void UpdateObjectVisibility(bool forced = true)
    {
        // Prevent updating visibility if player is not in world (example: LoadFromDB sets drunkstate which updates invisibility while player is not in map)
        if (!Location.IsInWorld)
            return;

        if (!forced)
            AddToNotify(NotifyFlags.VisibilityChanged);
        else
        {
            base.UpdateObjectVisibility();
            UpdateVisibilityForPlayer();
        }
    }

    public override bool UpdatePosition(Position pos, bool teleport = false)
    {
        return UpdatePosition(pos.X, pos.Y, pos.Z, pos.Orientation, teleport);
    }

    public override bool UpdatePosition(float x, float y, float z, float orientation, bool teleport = false)
    {
        if (!base.UpdatePosition(x, y, z, orientation, teleport))
            return false;

        // group update
        if (Group != null)
            SetGroupUpdateFlag(GroupUpdateFlags.Position);

        if (Trader != null && !Location.IsWithinDistInMap(Trader, SharedConst.InteractionDistance))
            Session.SendCancelTrade();

        CheckAreaExploreAndOutdoor();

        return true;
    }

    public void UpdateVisibilityForPlayer()
    {
        // updates visibility of all objects around point of view for current player
        var notifier = new VisibleNotifier(this, GridType.All, ObjectAccessor);
        CellCalculator.VisitGrid(SeerView, notifier, Visibility.GetSightRange());
        notifier.SendToSelf(); // send gathered data
    }

    public void UpdateVisibilityOf(ICollection<WorldObject> targets)
    {
        if (targets.Empty())
            return;

        UpdateData udata = new(Location.MapId);
        List<Unit> newVisibleUnits = new();

        foreach (var target in targets)
        {
            if (target == this)
                continue;

            switch (target.TypeId)
            {
                case TypeId.Unit:
                    UpdateVisibilityOf(target.AsCreature, udata, newVisibleUnits);

                    break;

                case TypeId.Player:
                    UpdateVisibilityOf(target.AsPlayer, udata, newVisibleUnits);

                    break;

                case TypeId.GameObject:
                    UpdateVisibilityOf(target.AsGameObject, udata, newVisibleUnits);

                    break;

                case TypeId.DynamicObject:
                    UpdateVisibilityOf(target.AsDynamicObject, udata, newVisibleUnits);

                    break;

                case TypeId.Corpse:
                    UpdateVisibilityOf(target.AsCorpse, udata, newVisibleUnits);

                    break;

                case TypeId.AreaTrigger:
                    UpdateVisibilityOf(target.AsAreaTrigger, udata, newVisibleUnits);

                    break;

                case TypeId.SceneObject:
                    UpdateVisibilityOf(target.AsSceneObject, udata, newVisibleUnits);

                    break;

                case TypeId.Conversation:
                    UpdateVisibilityOf(target.AsConversation, udata, newVisibleUnits);

                    break;
            }
        }

        if (!udata.HasData())
            return;

        udata.BuildPacket(out var packet);
        SendPacket(packet);

        foreach (var visibleUnit in newVisibleUnits)
            SendInitialVisiblePackets(visibleUnit);
    }

    public void UpdateVisibilityOf(WorldObject target)
    {
        if (HaveAtClient(target))
        {
            if (!Visibility.CanSeeOrDetect(target, false, true))
            {
                if (target.IsTypeId(TypeId.Unit))
                    BeforeVisibilityDestroy(target.AsCreature, this);

                if (!target.IsDestroyedObject)
                    target.SendOutOfRangeForPlayer(this);
                else
                    target.DestroyForPlayer(this);

                lock (ClientGuiDs)
                    ClientGuiDs.Remove(target.GUID);
            }
        }
        else
        {
            if (Visibility.CanSeeOrDetect(target, false, true))
            {
                target.SendUpdateToPlayer(this);

                lock (ClientGuiDs)
                    ClientGuiDs.Add(target.GUID);

                // target aura duration for caster show only if target exist at caster client
                // send data at target visibility change (adding to client)
                if (target.IsTypeMask(TypeMask.Unit))
                    SendInitialVisiblePackets(target.AsUnit);
            }
        }
    }

    public void UpdateVisibilityOf<T>(T target, UpdateData data, List<Unit> visibleNow) where T : WorldObject
    {
        if (HaveAtClient(target))
        {
            if (!Visibility.CanSeeOrDetect(target, false, true))
            {
                BeforeVisibilityDestroy(target, this);

                if (!target.IsDestroyedObject)
                    target.BuildOutOfRangeUpdateBlock(data);
                else
                    target.BuildDestroyUpdateBlock(data);

                ClientGuiDs.Remove(target.GUID);
            }
        }
        else
        {
            if (Visibility.CanSeeOrDetect(target, false, true))
            {
                target.BuildCreateUpdateBlockForPlayer(data, this);
                UpdateVisibilityOf_helper(ClientGuiDs, target, visibleNow);
            }
        }
    }

    private void BeforeVisibilityDestroy(WorldObject obj, Player p)
    {
        if (!obj.IsTypeId(TypeId.Unit))
            return;

        if (p.PetGUID == obj.GUID && obj.AsCreature.IsPet)
            ((Pet)obj).Remove(PetSaveMode.NotInSlot, true);
    }

    private void CheckAreaExploreAndOutdoor()
    {
        if (!IsAlive)
            return;

        if (IsInFlight)
            return;

        if (Configuration.GetDefaultValue("vmap:EnableIndoorCheck", false))
            RemoveAurasWithAttribute(Location.IsOutdoors ? SpellAttr0.OnlyIndoors : SpellAttr0.OnlyOutdoors);

        var areaId = Location.Area;

        if (areaId == 0)
            return;

        if (!CliDB.AreaTableStorage.TryGetValue(areaId, out var areaEntry))
        {
            Log.Logger.Error("Player '{0}' ({1}) discovered unknown area (x: {2} y: {3} z: {4} map: {5})",
                             GetName(),
                             GUID.ToString(),
                             Location.X,
                             Location.Y,
                             Location.Z,
                             Location.MapId);

            return;
        }

        var offset = areaEntry.AreaBit / ActivePlayerData.ExploredZonesBits;

        if (offset >= PlayerConst.ExploredZonesSize)
        {
            Log.Logger.Error("Wrong area Id {0} in map data for (X: {1} Y: {2}) point to field PLAYER_EXPLORED_ZONES_1 + {3} ( {4} must be < {5} ).",
                             areaId,
                             Location.X,
                             Location.Y,
                             offset,
                             offset,
                             PlayerConst.ExploredZonesSize);

            return;
        }

        var val = 1ul << (areaEntry.AreaBit % ActivePlayerData.ExploredZonesBits);
        var currFields = ActivePlayerData.ExploredZones[offset];

        if (Convert.ToBoolean(currFields & val))
            return;

        SetUpdateFieldFlagValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ExploredZones, offset), val);

        UpdateCriteria(CriteriaType.RevealWorldMapOverlay, Location.Area);

        var areaLevels = DB2Manager.GetContentTuningData(areaEntry.ContentTuningID, PlayerData.CtrOptions.Value.ContentTuningConditionMask);

        if (!areaLevels.HasValue)
            return;

        if (IsMaxLevel)
            SendExplorationExperience(areaId, 0);
        else
        {
            var areaLevel = (ushort)Math.Min(Math.Max((ushort)Level, areaLevels.Value.MinLevel), areaLevels.Value.MaxLevel);
            var diff = (int)Level - areaLevel;
            uint xp;

            if (diff < -5)
                xp = (uint)(GameObjectManager.GetBaseXP(Level + 5) * Configuration.GetDefaultValue("Rate:XP:Explore", 1.0f));
            else if (diff > 5)
            {
                var explorationPercent = 100 - (diff - 5) * 5;

                if (explorationPercent < 0)
                    explorationPercent = 0;

                xp = (uint)(GameObjectManager.GetBaseXP(areaLevel) * explorationPercent / 100f * Configuration.GetDefaultValue("Rate:XP:Explore", 1.0f));
            }
            else
                xp = (uint)(GameObjectManager.GetBaseXP(areaLevel) * Configuration.GetDefaultValue("Rate:XP:Explore", 1.0f));

            if (Configuration.GetDefaultValue("MinDiscoveredScaledXPRatio", 0) != 0)
            {
                var minScaledXP = (uint)(GameObjectManager.GetBaseXP(areaLevel) * Configuration.GetDefaultValue("Rate:XP:Explore", 1.0f)) * Configuration.GetDefaultValue("MinDiscoveredScaledXPRatio", 0u) / 100;
                xp = Math.Max(minScaledXP, xp);
            }

            GiveXP(xp, null);
            SendExplorationExperience(areaId, xp);
        }

        Log.Logger.Information("Player {0} discovered a new area: {1}", GUID.ToString(), areaId);
    }

    private void SendCurrencies()
    {
        SetupCurrency packet = new();

        foreach (var (id, currency) in _currencyStorage)
        {
            if (!CliDB.CurrencyTypesStorage.TryGetValue(id, out var currencyRecord))
                continue;

            // Check faction
            if ((currencyRecord.IsAlliance() && Team != TeamFaction.Alliance) ||
                (currencyRecord.IsHorde() && Team != TeamFaction.Horde))
                continue;

            // Check award condition
            if (currencyRecord.AwardConditionID != 0)
            {
                var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(currencyRecord.AwardConditionID);

                if (playerCondition != null && !ConditionManager.IsPlayerMeetingCondition(this, playerCondition))
                    continue;
            }

            SetupCurrency.Record record = new()
            {
                Type = currencyRecord.Id,
                Quantity = currency.Quantity
            };

            if (currency.WeeklyQuantity / currencyRecord.GetScaler() > 0)
                record.WeeklyQuantity = currency.WeeklyQuantity;

            if (currencyRecord.HasMaxEarnablePerWeek())
                record.MaxWeeklyQuantity = GetCurrencyWeeklyCap(currencyRecord);

            if (currencyRecord.IsTrackingQuantity())
                record.TrackedQuantity = currency.TrackedQuantity;

            if (currencyRecord.HasTotalEarned())
                record.TotalEarned = (int)currency.EarnedQuantity;

            if (currencyRecord.HasMaxQuantity(true))
                record.MaxQuantity = (int)GetCurrencyMaxQuantity(currencyRecord, true);

            record.Flags = (byte)currency.Flags;
            record.Flags = (byte)(record.Flags & ~(int)CurrencyDbFlags.UnusedFlags);

            packet.Data.Add(record);
        }

        SendPacket(packet);
    }

    private void SendExplorationExperience(uint area, uint experience)
    {
        SendPacket(new ExplorationExperience(experience, area));
    }

    private void SendMessageToSetInRange(ServerPacket data, float dist, bool self, bool ownTeamOnly, bool required3dDist = false)
    {
        if (self)
            SendPacket(data);

        PacketSenderRef sender = new(data);
        var notifier = new MessageDistDeliverer<PacketSenderRef>(this, sender, dist, ownTeamOnly, null, required3dDist);
        CellCalculator.VisitGrid(this, notifier, dist);
    }

    private void UpdateVisibilityOf_helper<T>(List<ObjectGuid> s64, T target, List<Unit> v) where T : WorldObject
    {
        s64.Add(target.GUID);

        switch (target.TypeId)
        {
            case TypeId.Unit:
                v.Add(target.AsCreature);

                break;

            case TypeId.Player:
                v.Add(target.AsPlayer);

                break;
        }
    }

    #endregion Sends / Updates

    #region Chat

    public bool CanUnderstandLanguage(Language language)
    {
        if (IsGameMaster)
            return true;

        return LanguageManager.GetLanguageDescById(language).Any(languageDesc => languageDesc.SkillId != 0 && HasSkill((SkillType)languageDesc.SkillId)) || HasAuraTypeWithMiscvalue(AuraType.ComprehendLanguage, (int)language);
    }

    public override void Say(string text, Language language, WorldObject obj = null)
    {
        ScriptManager.OnPlayerChat(this, ChatMsg.Say, language, text);

        SendChatMessageToSetInRange(ChatMsg.Say, language, text, Configuration.GetDefaultValue("ListenRange:Say", 25.0f));
    }

    public override void Say(uint textId, WorldObject target = null)
    {
        Talk(textId, ChatMsg.Say, Configuration.GetDefaultValue("ListenRange:Say", 25.0f), target);
    }

    public override void TextEmote(string text, WorldObject obj = null, bool something = false)
    {
        ScriptManager.OnPlayerChat(this, ChatMsg.Emote, Language.Universal, text);

        ChatPkt data = new();
        data.Initialize(ChatMsg.Emote, Language.Universal, this, this, text);
        SendMessageToSetInRange(data, Configuration.GetDefaultValue("ListenRange:TextEmote", 25.0f), true, !Session.HasPermission(RBACPermissions.TwoSideInteractionChat), true);
    }

    public override void TextEmote(uint textId, WorldObject target = null, bool isBossEmote = false)
    {
        Talk(textId, ChatMsg.Emote, Configuration.GetDefaultValue("ListenRange:TextEmote", 25.0f), target);
    }

    public override void Whisper(string text, Language language, Player target, bool something = false)
    {
        var isAddonMessage = language == Language.Addon;

        if (!isAddonMessage)               // if not addon data
            language = Language.Universal; // whispers should always be readable

        //Player rPlayer = ObjectAccessor.FindPlayer(receiver);

        ScriptManager.OnPlayerChat(this, ChatMsg.Whisper, language, text, target);

        ChatPkt data = new();
        data.Initialize(ChatMsg.Whisper, language, this, this, text);
        target.SendPacket(data);

        // rest stuff shouldn't happen in case of addon message
        if (isAddonMessage)
            return;

        data.Initialize(ChatMsg.WhisperInform, language, target, target, text);
        SendPacket(data);

        if (!IsAcceptWhispers && !IsGameMaster && !target.IsGameMaster)
        {
            SetAcceptWhispers(true);
            SendSysMessage(CypherStrings.CommandWhisperon);
        }

        // announce afk or dnd message
        if (target.IsAfk)
            SendSysMessage(CypherStrings.PlayerAfk, target.GetName(), target.AutoReplyMsg);
        else if (target.IsDnd)
            SendSysMessage(CypherStrings.PlayerDnd, target.GetName(), target.AutoReplyMsg);
    }

    public override void Whisper(uint textId, Player target, bool isBossWhisper = false)
    {
        if (target == null)
            return;

        if (!CliDB.BroadcastTextStorage.TryGetValue(textId, out var bct))
        {
            Log.Logger.Error("WorldObject.Whisper: `broadcast_text` was not {0} found", textId);

            return;
        }

        var locale = target.Session.SessionDbLocaleIndex;
        ChatPkt packet = new();
        packet.Initialize(ChatMsg.Whisper, Language.Universal, this, target, DB2Manager.GetBroadcastTextValue(bct, locale, Gender));
        target.SendPacket(packet);
    }

    public void WhisperAddon(string text, string prefix, bool isLogged, Player receiver)
    {
        ScriptManager.OnPlayerChat(this, ChatMsg.Whisper, isLogged ? Language.AddonLogged : Language.Addon, text, receiver);

        if (!receiver.Session.IsAddonRegistered(prefix))
            return;

        ChatPkt data = new();
        data.Initialize(ChatMsg.Whisper, isLogged ? Language.AddonLogged : Language.Addon, this, this, text, 0, "", Locale.enUS, prefix);
        receiver.SendPacket(data);
    }

    public override void Yell(string text, Language language = Language.Universal, WorldObject obj = null)
    {
        ScriptManager.OnPlayerChat(this, ChatMsg.Yell, language, text);

        ChatPkt data = new();
        data.Initialize(ChatMsg.Yell, language, this, this, text);
        SendMessageToSetInRange(data, Configuration.GetDefaultValue("ListenRange:Yell", 300.0f), true);
    }

    public override void Yell(uint textId, WorldObject target = null)
    {
        Talk(textId, ChatMsg.Yell, Configuration.GetDefaultValue("ListenRange:Yell", 300.0f), target);
    }

    private void SendChatMessageToSetInRange(ChatMsg chatMsg, Language language, string text, float range)
    {
        CustomChatTextBuilder builder = new(this, chatMsg, text, language, this);
        LocalizedDo localizer = new(builder);

        // Send to self
        localizer.Invoke(this);

        // Send to players
        MessageDistDeliverer<LocalizedDo> notifier = new(this, localizer, range, false, null, true);
        CellCalculator.VisitGrid(this, notifier, range);
    }

    #endregion Chat

    //public sbyte GetCovenant()
    //{
    //    ObjectGuid guid = GetGUID();

    //    var stmt = CharacterDatabase.GetPreparedStatement(CHAR_SEL_COVENANT);
    //    stmt.AddValue(0, guid.GetCounter());
    //    var covenant = CharacterDatabase.Query(stmt);

    //    if (covenant == null)
    //    {
    //        return 0;
    //    }
    //    Field[] fields = covenant.Fetch();
    //    ushort _covenantId = fields[0].GetUInt16();

    //    return (sbyte)_covenantId;
    //}
}
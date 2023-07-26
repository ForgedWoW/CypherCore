// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Chat;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.Networking.Packets.Bpay;
using Forged.MapServer.OpCodeHandlers;
using Forged.MapServer.Server;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Battlepay;

public class BattlepayManager
{
    private readonly BattlePayDataStoreMgr _battlePayDataStoreMgr;
    private readonly ObjectGuidGeneratorFactory _objectGuidGeneratorFactory;
    private readonly BattlePetData _battlePet;
    private readonly IConfiguration _configuration;
    private readonly LoginDatabase _loginDatabase;
    private readonly WorldSession _session;
    private ulong _distributionIDCount;
    private ulong _purchaseIDCount;

    public BattlepayManager(WorldSession session, BattlePetData battlePet, LoginDatabase loginDatabase, IConfiguration configuration, BattlePayDataStoreMgr battlePayDataStoreMgr,
                            ObjectGuidGeneratorFactory objectGuidGeneratorFactory)
    {
        _session = session;
        _battlePet = battlePet;
        _loginDatabase = loginDatabase;
        _configuration = configuration;
        _battlePayDataStoreMgr = battlePayDataStoreMgr;
        _objectGuidGeneratorFactory = objectGuidGeneratorFactory;
        _purchaseIDCount = 0;
        _distributionIDCount = 0;
        WalletName = "Credits";
    }

    public BattlepayHandler BattlepayHandler { get; set; }
    public Purchase CurrentTransaction { get; private set; } = new();

    public bool IsAvailable => _configuration.GetDefaultValue("FeatureSystem:BpayStore:Enabled", false);

    public uint ShopCurrency => _configuration.GetDefaultValue("FeatureSystem:BpayStore:Currency", 1u);

    public string WalletName { get; }

    public void AddBattlePetFromBpayShop(uint battlePetCreatureID)
    {
        var speciesEntry = _battlePet.GetBattlePetSpeciesByCreature(battlePetCreatureID);

        if (_battlePet.GetBattlePetSpeciesByCreature(battlePetCreatureID) == null)
            return;

        _session.BattlePetMgr.AddPet(speciesEntry.Id, _battlePet.SelectPetDisplay(speciesEntry), _battlePet.RollPetBreed(speciesEntry.Id), _battlePet.GetDefaultPetQuality(speciesEntry.Id));

        //it gives back false information need to get the pet guid from the add pet method somehow
        SendBattlePayBattlePetDelivered(ObjectGuid.Create(HighGuid.BattlePet, _objectGuidGeneratorFactory.GetGenerator(HighGuid.BattlePet).Generate()), speciesEntry.CreatureID);
    }

    public bool AlreadyOwnProduct(uint itemId)
    {
        if (_session.Player == null)
            return false;

        var itemTemplate = _session.Player.GameObjectManager.GetItemTemplate(itemId);

        if (itemTemplate == null)
            return true;

        if (itemTemplate.Effects.Any(itr => itr.TriggerType == ItemSpelltriggerType.OnLearn && _session.Player.HasSpell((uint)itr.SpellID)))
            return true;

        return _session.Player.GetItemCount(itemId) != 0;
    }

    public void AssignDistributionToCharacter(in ObjectGuid targetCharGuid, ulong distributionId, uint productId, ushort specializationID, ushort choiceID)
    {
        var upgrade = new UpgradeStarted
        {
            CharacterGUID = targetCharGuid
        };

        _session.SendPacket(upgrade);

        //new BattlePayStartDistributionAssignToTargetResponse
        //{
        //    DistributionID = distributionId,
        //    unkint1 = 0,
        //    unkint2 = 0
        //};

        _session.SendPacket(upgrade);

        var purchase = CurrentTransaction;
        purchase.Status = (ushort)BpayDistributionStatus.AddToProcess; // DistributionStatus.Globals.BATTLE_PAY_DIST_STATUS_ADD_TO_PROCESS;

        SendBattlePayDistribution(productId, purchase.Status, distributionId, targetCharGuid);
    }

    public ulong GenerateNewDistributionId()
    {
        return 0x1E77800000000000 | ++_distributionIDCount;
    }

    public ulong GenerateNewPurchaseID()
    {
        return 0x1E77800000000000 | ++_purchaseIDCount;
    }

    public uint GetBattlePayCredits()
    {
        var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.LOGIN_SEL_BATTLE_PAY_ACCOUNT_CREDITS);

        stmt.AddValue(0, _session.BattlenetAccountId);

        var resultDon = _loginDatabase.Query(stmt);

        if (resultDon == null)
            return 0;

        var fields = resultDon.GetFields();
        var credits = fields.Read<uint>(0);

        return credits * 10000u; // currency precision .. in retail it like gold and copper .. 10 usd is 100000 battlepay credit
    }

    public bool HasBattlePayCredits(uint count)
    {
        if (GetBattlePayCredits() >= count)
            return true;

        _session.Player.SendSysMessage(20000, count);

        return false;
    }

    public bool ModifyBattlePayCredits(uint credits)
    {
        var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.LOGIN_UPD_BATTLE_PAY_ACCOUNT_CREDITS);
        stmt.AddValue(0, credits);
        stmt.AddValue(1, _session.BattlenetAccountId);
        _loginDatabase.Execute(stmt);
        SendBattlePayMessage(3, "", credits);

        return true;
    }

    public void ProcessDelivery(Purchase purchase)
    {
        var player = _session.Player;

        if (player == null)
            return;

        var productInfo = _battlePayDataStoreMgr.GetProductInfoForProduct(purchase.ProductID);
        var itemstosendinmail = new List<uint>();

        foreach (var productId in productInfo.ProductIds)
        {
            var product = _battlePayDataStoreMgr.GetProduct(productId);
            var itemTemplate = _session.Player.GameObjectManager.GetItemTemplate(product.Flags);
            var itemsToSendIfInventoryFull = new List<uint>();

            switch ((ProductType)product.Type)
            {
                case ProductType.Item: // 0
                    itemsToSendIfInventoryFull.Clear();

                    if (itemTemplate != null)
                    {
                        if (player.GetFreeInventorySpace() > product.Unk1)
                            player.AddItemWithToast(product.Flags, (ushort)product.Unk1, 0);
                        else
                            player.SendABunchOfItemsInMail(new List<uint>
                                                           {
                                                               product.Flags
                                                           },
                                                           "Ingame Shop item delivery");
                    }
                    else
                        BattlepayHandler.SendStartPurchaseResponse(CurrentTransaction, BpayError.PurchaseDenied);

                    foreach (var item in _battlePayDataStoreMgr.GetItemsOfProduct(product.ProductId).Where(item => _session.Player.GameObjectManager.GetItemTemplate(item.ItemID) != null))
                        if (player.GetFreeInventorySpace() > item.Quantity)
                            player.AddItemWithToast(item.ItemID, (ushort)item.Quantity, 0);
                        else
                            itemsToSendIfInventoryFull.Add(item.ItemID); // problem if the quantity > 0

                    if (itemsToSendIfInventoryFull.Count > 0)
                        player.SendABunchOfItemsInMail(itemsToSendIfInventoryFull, "Ingame Shop Item Delivery");

                    break;

                case ProductType.LevelBoost: // 1
                    switch (product.ProductId)
                    {
                        // level 50 boost
                        case 572:
                            player.SetLevel(50);

                            player.GearUpByLoadout(9,
                                                   new List<uint>
                                                   {
                                                       6771
                                                   });

                            player.InitTalentForLevel();
                            player.InitStatsForLevel();
                            player.UpdateSkillsForLevel();
                            player.LearnDefaultSkills();
                            player.LearnSpecializationSpells();
                            player.UpdateAllStats();
                            player.SetFullHealth();
                            player.SetFullPower(PowerType.Mana);

                            break;
                        // level 60 boost
                        case 630:
                            player.SetLevel(60);

                            player.GearUpByLoadout(9,
                                                   new List<uint>
                                                   {
                                                       6771
                                                   });

                            player.InitTalentForLevel();
                            player.InitStatsForLevel();
                            player.UpdateSkillsForLevel();
                            player.LearnDefaultSkills();
                            player.LearnSpecializationSpells();
                            player.UpdateAllStats();
                            player.SetFullHealth();
                            player.SetFullPower(PowerType.Mana);

                            break;
                    }

                    break;

                case ProductType.Pet: // 2
                    if (!player.Session.IsLogingOut)       // if logged in
                        player.Session.BattlePayMgr.AddBattlePetFromBpayShop(product.ItemId);
                    else
                        BattlepayHandler.SendStartPurchaseResponse(CurrentTransaction, BpayError.PurchaseDenied);

                    break;

                case ProductType.Mount: // 3
                    _session.CollectionMgr.AddMount(product.DisplayId, MountStatusFlags.None);

                    break;

                case ProductType.WoWToken: // 4
                    if (itemTemplate != null && !player.Session.IsLogingOut)
                    {
                        if (player.GetFreeInventorySpace() > product.Unk1)
                            player.AddItemWithToast(product.Flags, (ushort)product.Unk1, 0);
                        else
                            player.SendABunchOfItemsInMail(new List<uint>
                                                           {
                                                               product.Flags
                                                           },
                                                           "Ingame Shop - WoW Token Delivery");
                    }
                    else
                        BattlepayHandler.SendStartPurchaseResponse(CurrentTransaction, BpayError.PurchaseDenied);

                    break;

                case ProductType.NameChange:         // 5
                    if (!player.Session.IsLogingOut) // if logged in
                        player.SetAtLoginFlag(AtLoginFlags.Rename);
                    else
                        BattlepayHandler.SendStartPurchaseResponse(CurrentTransaction, BpayError.PurchaseDenied);

                    break;

                case ProductType.FactionChange:                            // 6
                    if (!player.Session.IsLogingOut)                       // if logged in
                        player.SetAtLoginFlag(AtLoginFlags.ChangeFaction); // not ok for 6 or 3 faction change - only does once yet
                    else
                        BattlepayHandler.SendStartPurchaseResponse(CurrentTransaction, BpayError.PurchaseDenied);

                    break;

                case ProductType.RaceChange:                            // 8
                    if (!player.Session.IsLogingOut)                    // if logged in
                        player.SetAtLoginFlag(AtLoginFlags.ChangeRace); // not ok for 6 or 3 faction change - only does once yet
                    else
                        BattlepayHandler.SendStartPurchaseResponse(CurrentTransaction, BpayError.PurchaseDenied);

                    break;

                case ProductType.CharacterTransfer: // 11
                    // if u have multiple realms u have to implement this xD otherwise it sends error
                    BattlepayHandler.SendStartPurchaseResponse(CurrentTransaction, BpayError.PurchaseDenied);

                    break;

                case ProductType.Toy: // 14
                    if (bool.TryParse(product.Unk1.ToString(), out var fan))
                        _session.CollectionMgr.AddToy(product.Flags, false, fan);

                    break;

                case ProductType.Expansion:          // 18
                    if (!player.Session.IsLogingOut) // if logged in
                        //player->SendMovieStart(936); // Play SL Intro - xD what else in a private server we don't sell expansions
                        player.SendMovieStart(957); // Play SL Outro - we are preparing for dragonflight xD

                    break;

                case ProductType.GameTime: // 20
                    if (itemTemplate != null && !player.Session.IsLogingOut)
                    {
                        if (player.GetFreeInventorySpace() > product.Unk1)
                            player.AddItemWithToast(product.Flags, (ushort)product.Unk1, 0);
                        else
                            player.SendABunchOfItemsInMail(new List<uint>
                                                           {
                                                               product.Flags
                                                           },
                                                           "Ingame Shop - WoW Token Delivery");
                    }
                    else
                        BattlepayHandler.SendStartPurchaseResponse(CurrentTransaction, BpayError.PurchaseDenied);

                    break;

                case ProductType.GuildNameChange:     // 21
                case ProductType.GuildFactionChange:  // 22
                case ProductType.GuildTransfer:       // 23
                case ProductType.GuildFactionTranfer: // 24
                    // Not implemented yet - need some more guild functions e.g.: getmembers
                    BattlepayHandler.SendStartPurchaseResponse(CurrentTransaction, BpayError.PurchaseDenied);

                    break;

                case ProductType.TransmogAppearance: // 26
                    _session.CollectionMgr.AddTransmogSet(product.Unk7);

                    break;

                // Customs:
                case ProductType.ItemSet:
                {
                    var its = _session.Player.GameObjectManager.GetItemTemplates();

                    //C++ TO C# CONVERTER NOTE: 'auto' variable declarations are not supported in C#:
                    //ORIGINAL LINE: for (auto const& itemTemplatePair : its)
                    foreach (var itemTemplatePair in its)
                    {
                        if (itemTemplatePair.Value.ItemSet != product.Flags)
                            continue;

                        var dest = new List<ItemPosCount>();
                        var msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, itemTemplatePair.Key, 1);

                        if (msg == InventoryResult.Ok)
                        {
                            var newItem = player.StoreNewItem(dest, itemTemplatePair.Key, true);

                            player.SendNewItem(newItem, 1, true, false);
                        }
                        else
                            itemstosendinmail.Add(itemTemplatePair.Value.Id);
                    }

                    if (itemstosendinmail.Count > 0)
                        player.SendABunchOfItemsInMail(itemstosendinmail, "Ingame Shop - You bought an item set!");
                }

                break;

                case ProductType.Gold: // 30
                    if (!player.Session.IsLogingOut)
                        player.ModifyMoney(product.Unk7);

                    break;

                case ProductType.Currency: // 31
                    if (!player.Session.IsLogingOut)
                        player.ModifyCurrency(product.Flags, (int)product.Unk1); // implement currencyID in DB

                    break;
                /*
                                case Battlepay::CharacterCustomization:
                                    if (player)
                                        player->SetAtLoginFlag(AT_LOGIN_CUSTOMIZE);
                                    break;

                                // Script by Legolast++
                                case Battlepay::ProfPriAlchemy:

                                    player->HasSkill(SKILL_ALCHEMY);
                                    player->HasSkill(SKILL_SHADOWLANDS_ALCHEMY);
                                    LearnAllRecipesInProfession(player, SKILL_ALCHEMY);
                                    break;

                                case Battlepay::ProfPriSastre:

                                    player->HasSkill(SKILL_TAILORING);
                                    player->HasSkill(SKILL_SHADOWLANDS_TAILORING);
                                    LearnAllRecipesInProfession(player, SKILL_TAILORING);
                                    break;

                                case Battlepay::ProfPriJoye:

                                    player->HasSkill(SKILL_JEWELCRAFTING);
                                    player->HasSkill(SKILL_SHADOWLANDS_JEWELCRAFTING);
                                    LearnAllRecipesInProfession(player, SKILL_JEWELCRAFTING);
                                    break;

                                case Battlepay::ProfPriHerre:

                                    player->HasSkill(SKILL_BLACKSMITHING);
                                    player->HasSkill(SKILL_SHADOWLANDS_BLACKSMITHING);
                                    LearnAllRecipesInProfession(player, SKILL_BLACKSMITHING);
                                    break;

                                case Battlepay::ProfPriPele:

                                    player->HasSkill(SKILL_LEATHERWORKING);
                                    player->HasSkill(SKILL_SHADOWLANDS_LEATHERWORKING);
                                    LearnAllRecipesInProfession(player, SKILL_LEATHERWORKING);
                                    break;

                                case Battlepay::ProfPriInge:

                                    player->HasSkill(SKILL_ENGINEERING);
                                    player->HasSkill(SKILL_SHADOWLANDS_ENGINEERING);
                                    LearnAllRecipesInProfession(player, SKILL_ENGINEERING);
                                    break;

                                case Battlepay::ProfPriInsc:

                                    player->HasSkill(SKILL_INSCRIPTION);
                                    player->HasSkill(SKILL_SHADOWLANDS_INSCRIPTION);
                                    LearnAllRecipesInProfession(player, SKILL_INSCRIPTION);
                                    break;

                                case Battlepay::ProfPriEncha:

                                    player->HasSkill(SKILL_ENCHANTING);
                                    player->HasSkill(SKILL_SHADOWLANDS_ENCHANTING);
                                    LearnAllRecipesInProfession(player, SKILL_ENCHANTING);
                                    break;

                                case Battlepay::ProfPriDesu:

                                    player->HasSkill(SKILL_SKINNING);
                                    player->HasSkill(SKILL_SHADOWLANDS_SKINNING);
                                    LearnAllRecipesInProfession(player, SKILL_SKINNING);
                                    break;

                                case Battlepay::ProfPriMing:

                                    player->HasSkill(SKILL_MINING);
                                    player->HasSkill(SKILL_SHADOWLANDS_MINING);
                                    LearnAllRecipesInProfession(player, SKILL_MINING);
                                    break;

                                case Battlepay::ProfPriHerb:

                                    player->HasSkill(SKILL_HERBALISM);
                                    player->HasSkill(SKILL_SHADOWLANDS_HERBALISM);
                                    LearnAllRecipesInProfession(player, SKILL_HERBALISM);
                                    break;

                                case Battlepay::ProfSecCoci:

                                    player->HasSkill(SKILL_COOKING);
                                    player->HasSkill(SKILL_SHADOWLANDS_COOKING);
                                    LearnAllRecipesInProfession(player, SKILL_COOKING);
                                    break;

                                case Battlepay::Promo:
                                    if (!player)
                                        // Ridding
                                    player->LearnSpell(33388, true);
                                    player->LearnSpell(33391, true);
                                    player->LearnSpell(34090, true);
                                    player->LearnSpell(34091, true);
                                    player->LearnSpell(90265, true);
                                    player->LearnSpell(54197, true);
                                    player->LearnSpell(90267, true);
                                    // Mounts
                                    player->LearnSpell(63956, true);

                                    break;

                                case Battlepay::RepClassic:
                                    if (!player)
                                        player->SetReputation(21, 42000);
                                    player->SetReputation(576, 42000);
                                    player->SetReputation(87, 42000);
                                    player->SetReputation(92, 42000);
                                    player->SetReputation(93, 42000);
                                    player->SetReputation(609, 42000);
                                    player->SetReputation(529, 42000);
                                    player->SetReputation(909, 42000);
                                    player->SetReputation(369, 42000);
                                    player->SetReputation(59, 42000);
                                    player->SetReputation(910, 42000);
                                    player->SetReputation(349, 42000);
                                    player->SetReputation(809, 42000);
                                    player->SetReputation(749, 42000);
                                    player->SetReputation(270, 42000);
                                    player->SetReputation(470, 42000);
                                    player->SetReputation(577, 42000);
                                    player->SetReputation(70, 42000);
                                    player->SetReputation(1357, 42000);
                                    player->SetReputation(1975, 42000);

                                    if (player->GetTeam() == ALLIANCE)
                                    {
                                        player->SetReputation(890, 42000);
                                        player->SetReputation(1691, 42000);
                                        player->SetReputation(1419, 42000);
                                        player->SetReputation(69, 42000);
                                        player->SetReputation(930, 42000);
                                        player->SetReputation(47, 42000);
                                        player->SetReputation(1134, 42000);
                                        player->SetReputation(54, 42000);
                                        player->SetReputation(730, 42000);
                                        player->SetReputation(509, 42000);
                                        player->SetReputation(1353, 42000);
                                        player->SetReputation(72, 42000);
                                        player->SetReputation(589, 42000);
                                    }
                                    else // Repu Horda
                                    {
                                        player->SetReputation(1690, 42000);
                                        player->SetReputation(1374, 42000);
                                        player->SetReputation(1133, 42000);
                                        player->SetReputation(81, 42000);
                                        player->SetReputation(729, 42000);
                                        player->SetReputation(68, 42000);
                                        player->SetReputation(889, 42000);
                                        player->SetReputation(510, 42000);
                                        player->SetReputation(911, 42000);
                                        player->SetReputation(76, 42000);
                                        player->SetReputation(1352, 42000);
                                        player->SetReputation(530, 42000);
                                    }
                                    player->GetSession()->SendNotification("|cff00FF00Se ha aumentado todas las Reputaciones Clasicas!");
                                    return;
                                    break;

                                case Battlepay::RepBurnig:
                                    if (!player)
                                        player->SetReputation(1015, 42000);
                                    player->SetReputation(1011, 42000);
                                    player->SetReputation(933, 42000);
                                    player->SetReputation(967, 42000);
                                    player->SetReputation(970, 42000);
                                    player->SetReputation(942, 42000);
                                    player->SetReputation(1031, 42000);
                                    player->SetReputation(1012, 42000);
                                    player->SetReputation(990, 42000);
                                    player->SetReputation(932, 42000);
                                    player->SetReputation(934, 42000);
                                    player->SetReputation(935, 42000);
                                    player->SetReputation(1077, 42000);
                                    player->SetReputation(1038, 42000);
                                    player->SetReputation(989, 42000);

                                    if (player->GetTeam() == ALLIANCE)
                                    {
                                        player->SetReputation(946, 42000);
                                        player->SetReputation(978, 42000);
                                    }
                                    else // Repu Horda
                                    {
                                        player->SetReputation(941, 42000);
                                        player->SetReputation(947, 42000);
                                        player->SetReputation(922, 42000);
                                    }
                                    player->GetSession()->SendNotification("|cff00FF00Se ha aumentado todas las Reputaciones Burning Crusade!");
                                    return;
                                    break;

                                case Battlepay::RepTLK:
                                    if (!player)
                                        player->SetReputation(1242, 42000);
                                    player->SetReputation(1376, 42000);
                                    player->SetReputation(1387, 42000);
                                    player->SetReputation(1135, 42000);
                                    player->SetReputation(1158, 42000);
                                    player->SetReputation(1173, 42000);
                                    player->SetReputation(1171, 42000);
                                    player->SetReputation(1204, 42000);
                                    if (player->GetTeam() == ALLIANCE)
                                    {
                                        player->SetReputation(1177, 42000);
                                        player->SetReputation(1174, 42000);
                                    }
                                    else // Repu Horda
                                    {
                                        player->SetReputation(1172, 42000);
                                        player->SetReputation(1178, 42000);
                                    }
                                    player->SetReputation(529, 42000);
                                    player->GetSession()->SendNotification("|cff00FF00Se ha aumentado todas las Reputaciones The Lich King!");
                                    return;
                                    break;

                                case Battlepay::RepCata:
                                    if (!player)
                                        player->SetReputation(1091, 42000);
                                    player->SetReputation(1098, 42000);
                                    player->SetReputation(1106, 42000);
                                    player->SetReputation(1156, 42000);
                                    player->SetReputation(1090, 42000);
                                    player->SetReputation(1119, 42000);
                                    player->SetReputation(1073, 42000);
                                    player->SetReputation(1105, 42000);
                                    player->SetReputation(1104, 42000);

                                    if (player->GetTeam() == ALLIANCE)
                                    {
                                        player->SetReputation(1094, 42000);
                                        player->SetReputation(1050, 42000);
                                        player->SetReputation(1068, 42000);
                                        player->SetReputation(1126, 42000);
                                        player->SetReputation(1037, 42000);
                                    }
                                    else // Repu Horda
                                    {
                                        player->SetReputation(1052, 42000);
                                        player->SetReputation(1067, 42000);
                                        player->SetReputation(1124, 42000);
                                        player->SetReputation(1064, 42000);
                                        player->SetReputation(1085, 42000);
                                    }
                                    player->GetSession()->SendNotification("|cff00FF00Se ha aumentado todas las Reputaciones Cataclismo!");
                                    return;
                                    break;

                                case Battlepay::RepPanda:
                                    if (!player)
                                        player->SetReputation(1216, 42000);
                                    player->SetReputation(1435, 42000);
                                    player->SetReputation(1277, 42000);
                                    player->SetReputation(1359, 42000);
                                    player->SetReputation(1275, 42000);
                                    player->SetReputation(1492, 42000);
                                    player->SetReputation(1281, 42000);
                                    player->SetReputation(1283, 42000);
                                    player->SetReputation(1279, 42000);
                                    player->SetReputation(1273, 42000);
                                    player->SetReputation(1341, 42000);
                                    player->SetReputation(1345, 42000);
                                    player->SetReputation(1337, 42000);
                                    player->SetReputation(1272, 42000);
                                    player->SetReputation(1351, 42000);
                                    player->SetReputation(1302, 42000);
                                    player->SetReputation(1269, 42000);
                                    player->SetReputation(1358, 42000);
                                    player->SetReputation(1271, 42000);
                                    player->SetReputation(1282, 42000);
                                    player->SetReputation(1440, 42000);
                                    player->SetReputation(1270, 42000);
                                    player->SetReputation(1278, 42000);
                                    player->SetReputation(1280, 42000);
                                    player->SetReputation(1276, 42000);

                                    if (player->GetTeam() == ALLIANCE)
                                    {
                                        player->SetReputation(1242, 42000);
                                        player->SetReputation(1376, 42000);
                                        player->SetReputation(1387, 42000);
                                    }
                                    else // Repu Horda
                                    {
                                        player->SetReputation(1388, 42000);
                                        player->SetReputation(1228, 42000);
                                        player->SetReputation(1375, 42000);
                                    }
                                    player->GetSession()->SendNotification("|cff00FF00Se ha aumentado todas las Reputaciones de Pandaria!");
                                    return;
                                    break;

                                case Battlepay::RepDraenor:
                                    if (!player)
                                        player->SetReputation(1850, 42000);
                                    player->SetReputation(1515, 42000);
                                    player->SetReputation(1520, 42000);
                                    player->SetReputation(1732, 42000);
                                    player->SetReputation(1735, 42000);
                                    player->SetReputation(1741, 42000);
                                    player->SetReputation(1849, 42000);
                                    player->SetReputation(1737, 42000);
                                    player->SetReputation(1711, 42000);
                                    player->SetReputation(1736, 42000);
                                    // Repu Alianza
                                    if (player->GetTeam() == ALLIANCE)
                                    {
                                        player->SetReputation(1731, 42000);
                                        player->SetReputation(1710, 42000);
                                        player->SetReputation(1738, 42000);
                                        player->SetReputation(1733, 42000);
                                        player->SetReputation(1847, 42000);
                                        player->SetReputation(1682, 42000);
                                    }
                                    else // Repu Horda
                                    {
                                        player->SetReputation(1740, 42000);
                                        player->SetReputation(1681, 42000);
                                        player->SetReputation(1445, 42000);
                                        player->SetReputation(1708, 42000);
                                        player->SetReputation(1848, 42000);
                                        player->SetReputation(1739, 42000);
                                    }
                                    player->GetSession()->SendNotification("|cff00FF00Se ha aumentado todas las Reputaciones de Draenor!");
                                    return;
                                    break;

                                case Battlepay::RepLegion:
                                    if (!player)
                                        player->SetReputation(1919, 42000);
                                    player->SetReputation(1859, 42000);
                                    player->SetReputation(1900, 42000);
                                    player->SetReputation(1899, 42000);
                                    player->SetReputation(1989, 42000);
                                    player->SetReputation(1947, 42000);
                                    player->SetReputation(1894, 42000);
                                    player->SetReputation(1984, 42000);
                                    player->SetReputation(1862, 42000);
                                    player->SetReputation(1861, 42000);
                                    player->SetReputation(1860, 42000);
                                    player->SetReputation(1815, 42000);
                                    player->SetReputation(1883, 42000);
                                    player->SetReputation(1828, 42000);
                                    player->SetReputation(1948, 42000);
                                    player->SetReputation(2018, 42000);
                                    player->SetReputation(1888, 42000);
                                    player->SetReputation(2045, 42000);
                                    player->SetReputation(2170, 42000);
                                    player->SetReputation(2165, 42000);
                                    player->GetSession()->SendNotification("|cff00FF00Se ha aumentado todas las Reputaciones de Legion!");
                                    return;
                                    break;
                */
                case ProductType.PremadePve:
                    if (!player.Session.IsLogingOut) // Bags
                        for (var slot = InventorySlots.BagStart; slot < InventorySlots.BagEnd; slot++)
                            player.EquipNewItem(slot, 142075, ItemContext.None, true);

                    player.GiveLevel(60);
                    player.InitTalentForLevel();
                    player.ModifyMoney(200000000);
                    player.LearnSpell(33388, true); // Equitacion
                    player.LearnSpell(33391, true);
                    player.LearnSpell(34090, true);
                    player.LearnSpell(34091, true);
                    player.LearnSpell(90265, true);
                    player.LearnSpell(54197, true);
                    player.LearnSpell(90267, true);
                    player.LearnSpell(115913, true);
                    player.LearnSpell(110406, true);
                    player.LearnSpell(104381, true);

                    if (player.Class == PlayerClass.Shaman)
                    {
                        player.EquipNewItem(EquipmentSlot.Head, 199444, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Wrist, 199448, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Waist, 199447, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Hands, 199443, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Chest, 199441, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Legs, 199445, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Shoulders, 199446, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Feet, 199442, ItemContext.None, true);
                    }

                    if (player.Class == PlayerClass.Hunter)
                    {
                        player.EquipNewItem(EquipmentSlot.Head, 198592, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Wrist, 198596, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Waist, 198595, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Hands, 198591, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Chest, 198589, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Legs, 198593, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Shoulders, 198594, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Feet, 198590, ItemContext.None, true);
                    }

                    if (player.Class == PlayerClass.Mage)
                    {
                        player.EquipNewItem(EquipmentSlot.Head, 198568, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Wrist, 198571, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Waist, 198570, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Hands, 198567, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Chest, 198565, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Legs, 198569, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Shoulders, 198572, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Feet, 198566, ItemContext.None, true);
                    }

                    if (player.Class == PlayerClass.Priest)
                    {
                        player.EquipNewItem(EquipmentSlot.Head, 199420, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Wrist, 199423, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Waist, 199422, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Hands, 199419, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Chest, 199417, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Legs, 199421, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Shoulders, 19942, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Feet, 199418, ItemContext.None, true);
                    }

                    if (player.Class == PlayerClass.Warlock)
                    {
                        player.EquipNewItem(EquipmentSlot.Head, 199420, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Wrist, 199423, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Waist, 199422, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Hands, 199419, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Chest, 199417, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Legs, 199421, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Shoulders, 19942, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Feet, 199418, ItemContext.None, true);
                    }

                    if (player.Class == PlayerClass.DemonHunter)
                    {
                        player.EquipNewItem(EquipmentSlot.Head, 198575, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Wrist, 198578, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Waist, 198577, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Hands, 198574, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Chest, 198579, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Legs, 198576, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Shoulders, 198580, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Feet, 198573, ItemContext.None, true);
                    }

                    if (player.Class == PlayerClass.Rogue)
                    {
                        player.EquipNewItem(EquipmentSlot.Head, 199427, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Wrist, 199430, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Waist, 199429, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Hands, 199426, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Chest, 199431, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Legs, 199428, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Shoulders, 199432, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Feet, 199425, ItemContext.None, true);
                    }

                    if (player.Class == PlayerClass.Monk)
                    {
                        player.EquipNewItem(EquipmentSlot.Head, 198575, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Wrist, 198578, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Waist, 198577, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Hands, 198574, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Chest, 198579, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Legs, 198576, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Shoulders, 198580, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Feet, 198573, ItemContext.None, true);
                    }

                    if (player.Class == PlayerClass.Druid)
                    {
                        player.EquipNewItem(EquipmentSlot.Head, 199427, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Wrist, 199430, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Waist, 199429, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Hands, 199426, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Chest, 199431, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Legs, 199428, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Shoulders, 199432, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Feet, 199425, ItemContext.None, true);
                    }

                    if (player.Class == PlayerClass.Warrior)
                    {
                        player.EquipNewItem(EquipmentSlot.Head, 199433, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Wrist, 199440, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Waist, 199439, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Hands, 199436, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Chest, 199434, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Legs, 199437, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Shoulders, 199438, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Feet, 199435, ItemContext.None, true);
                    }

                    if (player.Class == PlayerClass.Paladin)
                    {
                        player.EquipNewItem(EquipmentSlot.Head, 199433, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Wrist, 199440, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Waist, 199439, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Hands, 199436, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Chest, 199434, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Legs, 199437, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Shoulders, 199438, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Feet, 199435, ItemContext.None, true);
                    }

                    if (player.Class == PlayerClass.Deathknight)
                    {
                        var quest = _session.Player.GameObjectManager.GetQuestTemplate(12801);

                        if (_session.Player.GameObjectManager.GetQuestTemplate(12801) != null)
                        {
                            player.AddQuest(quest, null);
                            player.CompleteQuest(quest.Id);
                            player.RewardQuest(quest, LootItemType.Item, 0, null, false);
                        }

                        if (player.TeamId == TeamIds.Alliance)
                            player.TeleportTo(0, -8829.8710f, 625.3872f, 94.1712f, 3.808243f);
                        else
                            player.TeleportTo(1, 1570.6693f, -4399.3388f, 16.0058f, 3.382241f);

                        player.LearnSpell(53428, true); // runeforging
                        player.LearnSpell(53441, true); // runeforging
                        player.LearnSpell(54586, true); // runeforging credit
                        player.LearnSpell(48778, true); //acherus deathcharger
                        player.LearnSkillRewardedSpells(776, 375, Race.None);
                        player.LearnSkillRewardedSpells(960, 375, Race.None);

                        player.EquipNewItem(EquipmentSlot.Head, 198581, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Wrist, 198587, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Waist, 198588, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Hands, 198584, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Chest, 198582, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Legs, 198585, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Shoulders, 198586, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Feet, 198583, ItemContext.None, true);
                    }

                    // DRACTHYR DF
                    if (player.Class == PlayerClass.Evoker)
                    {
                        player.EquipNewItem(EquipmentSlot.Head, 199444, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Wrist, 199448, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Waist, 199447, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Hands, 199443, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Chest, 199441, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Legs, 199445, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Shoulders, 199446, ItemContext.None, true);
                        player.EquipNewItem(EquipmentSlot.Feet, 199442, ItemContext.None, true);
                    }

                    break;
                    /*
                case Battlepay::VueloDL:
                    if (!player)
                        player->AddItem(128706, 1);
                    player->CompletedAchievement(sAchievementStore.LookupEntry(10018));
                    player->CompletedAchievement(sAchievementStore.LookupEntry(11190));
                    player->CompletedAchievement(sAchievementStore.LookupEntry(11446));
                    player->GetSession()->SendNotification("|cff00FF00Has aprendido poder volar en las Islas Abruptas, Costas Abruptas y Draenor");
                    break;
                    //
                        */
            }
        }
        /*
        if (!product->ScriptName.empty())
            sScriptMgr->OnBattlePayProductDelivery(_session, product);
            */
    }

    public void RegisterStartPurchase(Purchase purchase)
    {
        CurrentTransaction = purchase;
    }

    public void SavePurchase(Purchase purchase)
    {
        var productInfo = _battlePayDataStoreMgr.GetProductInfoForProduct(purchase.ProductID);
        var displayInfo = _battlePayDataStoreMgr.GetDisplayInfo(productInfo.Entry);
        var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.LOGIN_INS_PURCHASE);
        stmt.AddValue(0, _session.AccountId);
        stmt.AddValue(1, WorldManager.Realm.Id.VirtualRealmAddress);
        stmt.AddValue(2, _session.Player != null ? _session.Player.GUID.Counter : 0);
        stmt.AddValue(3, purchase.ProductID);
        stmt.AddValue(4, displayInfo.Name1);
        stmt.AddValue(5, purchase.CurrentPrice);
        stmt.AddValue(6, _session.RemoteAddress);
        _loginDatabase.Execute(stmt);
    }

    public void SendAccountCredits()
    {
        //    auto sessionId = _session->GetAccountId();
        //
        //    LoginDatabasePreparedStatement* stmt = _loginDatabase.GetPreparedStatement(LOGIN_SEL_BATTLE_PAY_ACCOUNT_CREDITS);
        //    stmt->setUInt32(0, _session->GetAccountId());
        //    PreparedQueryResult result = _loginDatabase.Query(stmt);
        //
        //    auto sSession = sWorld->FindSession(sessionId);
        //    if (!sSession)
        //        return;
        //
        //    uint64 balance = 0;
        //    if (result)
        //    {
        //        auto fields = result->Fetch();
        //        if (auto balanceStr = fields[0].GetCString())
        //            balance = atoi(balanceStr);
        //    }
        //
        //    auto player = sSession->GetPlayer();
        //    if (!player)
        //        return;
        //
        //    SendBattlePayMessage(2, "");
    }

    public void SendBattlePayBattlePetDelivered(ObjectGuid petguid, uint creatureID)
    {
        var response = new BattlePayBattlePetDelivered
        {
            DisplayID = creatureID,
            BattlePetGuid = petguid
        };

        _session.SendPacket(response);
        Log.Logger.Error("Send BattlePayBattlePetDelivered guid: {} && creatureID: {}", petguid.Counter, creatureID);
    }

    public void SendBattlePayDistribution(uint productId, ushort status, ulong distributionId, ObjectGuid targetGuid)
    {
        var distributionBattlePay = new DistributionUpdate();
        var product = _battlePayDataStoreMgr.GetProduct(productId);

        var productInfo = _battlePayDataStoreMgr.GetProductInfoForProduct(productId);

        distributionBattlePay.DistributionObject.DistributionID = distributionId;
        distributionBattlePay.DistributionObject.Status = status;
        distributionBattlePay.DistributionObject.ProductID = productId;
        distributionBattlePay.DistributionObject.Revoked = false; // not needed for us

        if (!targetGuid.IsEmpty)
        {
            distributionBattlePay.DistributionObject.TargetPlayer = targetGuid;
            distributionBattlePay.DistributionObject.TargetVirtualRealm = WorldManager.Realm.Id.VirtualRealmAddress;
            distributionBattlePay.DistributionObject.TargetNativeRealm = WorldManager.Realm.Id.VirtualRealmAddress;
        }

        var productData = new BpayProduct
        {
            ProductId = product.ProductId,
            Type = product.Type,
            Flags = product.Flags,
            Unk1 = product.Unk1,
            DisplayId = product.DisplayId,
            ItemId = product.ItemId,
            Unk4 = product.Unk4,
            Unk5 = product.Unk5,
            Unk6 = product.Unk6,
            Unk7 = product.Unk7,
            Unk8 = product.Unk8,
            Unk9 = product.Unk9,
            UnkString = product.UnkString,
            UnkBit = product.UnkBit,
            UnkBits = product.UnkBits
        };

        foreach (var item in _battlePayDataStoreMgr.GetItemsOfProduct(product.ProductId))
        {
            var productItem = new BpayProductItem
            {
                ID = item.ID,
                UnkByte = item.UnkByte,
                ItemID = item.ItemID,
                Quantity = item.Quantity,
                UnkInt1 = item.UnkInt1,
                UnkInt2 = item.UnkInt2,
                IsPet = item.IsPet,
                PetResult = item.PetResult
            };

            var dInfo = WriteDisplayInfo(productInfo.Entry);

            if (dInfo.Item1)
                productItem.Display = dInfo.Item2;
        }

        var data = WriteDisplayInfo(productInfo.Entry);

        if (data.Item1)
            productData.Display = data.Item2;

        distributionBattlePay.DistributionObject.Product = productData;
        _session.SendPacket(distributionBattlePay);
    }

    public void SendBattlePayMessage(uint bpaymessageID, string name, uint value = 0)
    {
        var msg = "";

        if (bpaymessageID == 1)
            msg += "The purchase '" + name + "' was successful!";

        if (bpaymessageID == 2)
            msg += "Remaining credits: " + GetBattlePayCredits() / 10000 + " .";

        if (bpaymessageID == 10)
            msg += "You cannot purchase '" + name + "' . Contact a GameInfo master to find out more.";

        if (bpaymessageID == 11)
            msg += "Your bags are too full to add : " + name + " .";

        if (bpaymessageID == 12)
            msg += "You have already purchased : " + name + " .";

        if (bpaymessageID == 20)
            msg += "The battle pay credits have been updated for the character '" + name + "' ! Available credits:" + value + " .";

        if (bpaymessageID == 21)
            msg += "You must enter an amount !";

        if (bpaymessageID == 3)
            msg += "You have now '" + value + "' credits.";

        _session.CommandHandler.SendSysMessage(msg);
    }

    //C++ TO C# CONVERTER TASK: There is no C# equivalent to C++ suffix return type syntax:
    //ORIGINAL LINE: auto ProductFilter(WorldPackets::BattlePay::Product product)->bool;
    //C++ TO C# CONVERTER TASK: The return type of the following function could not be determined:
    //C++ TO C# CONVERTER TASK: The implementation of the following method could not be found:
    //	auto ProductFilter(WorldPackets::BattlePay::Product product);
    public void SendProductList()
    {
        var response = new ProductListResponse();
        var player = _session.Player; // it's a false value if player is in character screen

        if (!IsAvailable)
        {
            response.Result = (uint)ProductListResult.LockUnk1;
            _session.SendPacket(response);

            return;
        }

        response.Result = (uint)ProductListResult.Available;
        response.CurrencyID = ShopCurrency > 0 ? ShopCurrency : 1;

        // BATTLEPAY GROUP
        foreach (var itr in _battlePayDataStoreMgr.ProductGroups)
        {
            var group = new BpayGroup
            {
                GroupId = itr.GroupId,
                IconFileDataID = itr.IconFileDataID,
                DisplayType = itr.DisplayType,
                Ordering = itr.Ordering,
                Unk = itr.Unk,
                Name = itr.Name,
                Description = itr.Description
            };

            response.ProductGroups.Add(group);
        }

        // BATTLEPAY SHOP
        foreach (var itr in _battlePayDataStoreMgr.ShopEntries)
        {
            var shop = new BpayShop
            {
                EntryId = itr.EntryId,
                GroupID = itr.GroupID,
                ProductID = itr.ProductID,
                Ordering = itr.Ordering,
                VasServiceType = itr.VasServiceType,
                StoreDeliveryType = itr.StoreDeliveryType
            };

            // shop entry and display entry must be the same
            var data = WriteDisplayInfo(itr.Entry);

            if (data.Item1)
                shop.Display = data.Item2;

            // when logged out don't show everything
            if (player == null && shop.StoreDeliveryType != 2)
                continue;

            var productAddon = _battlePayDataStoreMgr.GetProductAddon(itr.Entry);

            if (productAddon != null)
                if (productAddon.DisableListing > 0)
                    continue;

            response.Shops.Add(shop);
        }

        // BATTLEPAY PRODUCT INFO
        foreach (var itr in _battlePayDataStoreMgr.ProductInfos)
        {
            var productInfo = itr.Value;

            var productAddon = _battlePayDataStoreMgr.GetProductAddon(productInfo.Entry);

            if (productAddon != null)
                if (productAddon.DisableListing > 0)
                    continue;

            var productinfo = new BpayProductInfo
            {
                ProductId = productInfo.ProductId,
                NormalPriceFixedPoint = productInfo.NormalPriceFixedPoint,
                CurrentPriceFixedPoint = productInfo.CurrentPriceFixedPoint,
                ProductIds = productInfo.ProductIds,
                Unk1 = productInfo.Unk1,
                Unk2 = productInfo.Unk2,
                UnkInts = productInfo.UnkInts,
                Unk3 = productInfo.Unk3,
                ChoiceType = productInfo.ChoiceType
            };

            // productinfo entry and display entry must be the same
            var data = WriteDisplayInfo(productInfo.Entry);

            if (data.Item1)
                productinfo.Display = data.Item2;

            response.ProductInfos.Add(productinfo);
        }

        foreach (var itr in _battlePayDataStoreMgr.Products)
        {
            var product = itr.Value;
            var productInfo = _battlePayDataStoreMgr.GetProductInfoForProduct(product.ProductId);

            var productAddon = _battlePayDataStoreMgr.GetProductAddon(productInfo.Entry);

            if (productAddon != null)
                if (productAddon.DisableListing > 0)
                    continue;

            // BATTLEPAY PRODUCTS
            var pProduct = new BpayProduct
            {
                ProductId = product.ProductId,
                Type = product.Type,
                Flags = product.Flags,
                Unk1 = product.Unk1,
                DisplayId = product.DisplayId,
                ItemId = product.ItemId,
                Unk4 = product.Unk4,
                Unk5 = product.Unk5,
                Unk6 = product.Unk6,
                Unk7 = product.Unk7,
                Unk8 = product.Unk8,
                Unk9 = product.Unk9,
                UnkString = product.UnkString,
                UnkBit = product.UnkBit,
                UnkBits = product.UnkBits
            };

            // BATTLEPAY ITEM
            if (product.Items.Count > 0)
                foreach (var item in _battlePayDataStoreMgr.GetItemsOfProduct(product.ProductId))
                {
                    var pItem = new BpayProductItem
                    {
                        ID = item.ID,
                        UnkByte = item.UnkByte,
                        ItemID = item.ItemID,
                        Quantity = item.Quantity,
                        UnkInt1 = item.UnkInt1,
                        UnkInt2 = item.UnkInt2,
                        IsPet = item.IsPet,
                        PetResult = item.PetResult
                    };

                    if (_battlePayDataStoreMgr.DisplayInfoExist(productInfo.Entry))
                    {
                        // productinfo entry and display entry must be the same
                        var disInfo = WriteDisplayInfo(productInfo.Entry);

                        if (disInfo.Item1)
                            pItem.Display = disInfo.Item2;
                    }

                    pProduct.Items.Add(pItem);
                }

            // productinfo entry and display entry must be the same
            var data = WriteDisplayInfo(productInfo.Entry);

            if (data.Item1)
                pProduct.Display = data.Item2;

            response.Products.Add(pProduct);
        }

        /*
        // debug
        TC_LOG_INFO("server.BattlePay", "SendProductList with {} ProductInfos, {} Products, {} Shops. CurrencyID: {}.", response.ProductInfos.size(), response.Products.size(), response.Shops.size(), response.CurrencyID);
        for (int i = 0; i != response.ProductInfos.size(); i++)
        {
            TC_LOG_INFO("server.BattlePay", "({}) ProductInfo: ProductId [{}], First SubProductId [{}], CurrentPriceFixedPoint [{}]", i, response.ProductInfos[i].ProductId, response.ProductInfos[i].ProductIds[0], response.ProductInfos[i].CurrentPriceFixedPoint);
            TC_LOG_INFO("server.BattlePay", "({}) Products: ProductId [{}], UnkString [{}]", i, response.Products[i].ProductId, response.Products[i].UnkString);
            TC_LOG_INFO("server.BattlePay", "({}) Shops: ProductId [{}]", i, response.Shops[i].ProductID);
        }
        */

        _session.SendPacket(response);
    }

    public void Update(uint diff)
    {
        Log.Logger.Information("BattlepayManager::Update");
        /*
        auto& data = _actualTransaction;
        auto product = sBattlePayDataStore->GetProduct(data.ProductID);

        switch (data.Status)
        {
        case Battlepay::Properties::DistributionStatus::BATTLE_PAY_DIST_STATUS_ADD_TO_PROCESS:
        {
            switch (product->Type)
            {
            case CharacterBoost:
            {
                auto const& player = data.TargetCharacter;
                if (!player)
                    break;

                WorldPackets::BattlePay::BattlePayCharacterUpgradeQueued responseQueued;
                responseQueued.EquipmentItems = sDB2Manager.GetItemLoadOutItemsByClassID(player->getClass(), 3)[0];
                responseQueued.Character = data.TargetCharacter;
                _session->SendPacket(responseQueued.Write());

                data.Status = DistributionStatus::BATTLE_PAY_DIST_STATUS_PROCESS_COMPLETE;
                SendBattlePayDistribution(data.ProductID, data.Status, data.DistributionId, data.TargetCharacter);
                break;
            }
            }
            break;
        }
        case Battlepay::Properties::DistributionStatus::BATTLE_PAY_DIST_STATUS_PROCESS_COMPLETE: //send SMSG_BATTLE_PAY_VAS_PURCHASE_STARTED
        {
            switch (product->WebsiteType)
            {
            case CharacterBoost:
            {
                data.Status = DistributionStatus::BATTLE_PAY_DIST_STATUS_FINISHED;
                SendBattlePayDistribution(data.ProductID, data.Status, data.DistributionId, data.TargetCharacter);
                break;
            }
            }
            break;
        }
        case Battlepay::Properties::DistributionStatus::BATTLE_PAY_DIST_STATUS_FINISHED:
        {
            switch (product->WebsiteType)
            {
            case CharacterBoost:
                SendBattlePayDistribution(data.ProductID, data.Status, data.DistributionId, data.TargetCharacter);
                break;
            }
            break;
        }
        case Battlepay::Properties::DistributionStatus::BATTLE_PAY_DIST_STATUS_AVAILABLE:
        case Battlepay::Properties::DistributionStatus::BATTLE_PAY_DIST_STATUS_NONE:
        }
        */
    }

    public bool UpdateBattlePayCredits(ulong price)
    {
        //TC_LOG_INFO("server.BattlePay", "UpdateBattlePayCredits: GetBattlePayCredits(): {} - price: {}", GetBattlePayCredits(), price);
        var calcCredit = (GetBattlePayCredits() - price) / 10000;
        var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.LOGIN_UPD_BATTLE_PAY_ACCOUNT_CREDITS);
        stmt.AddValue(0, calcCredit);
        stmt.AddValue(1, _session.BattlenetAccountId);
        _loginDatabase.Execute(stmt);

        return true;
    }

    public Tuple<bool, BpayDisplayInfo> WriteDisplayInfo(uint displayInfoEntry, uint productId = 0)
    {
        //C++ TO C# CONVERTER TASK: Lambda expressions cannot be assigned to 'var':
        var qualityColor = (uint displayInfoOrProductInfoEntry) =>
        {
            var productAddon = _battlePayDataStoreMgr.GetProductAddon(displayInfoOrProductInfoEntry);

            if (productAddon == null)
                return "|cffffffff";

            return _battlePayDataStoreMgr.GetProductAddon(displayInfoOrProductInfoEntry).NameColorIndex switch
            {
                0 => "|cffffffff",
                1 => "|cff1eff00",
                2 => "|cff0070dd",
                3 => "|cffa335ee",
                4 => "|cffff8000",
                5 => "|cffe5cc80",
                6 => "|cffe5cc80",
                _ => "|cffffffff"
            };
        };

        var info = new BpayDisplayInfo();

        var displayInfo = _battlePayDataStoreMgr.GetDisplayInfo(displayInfoEntry);

        if (displayInfo == null)
            return Tuple.Create(false, info);

        info.CreatureDisplayID = displayInfo.CreatureDisplayID;
        info.VisualID = displayInfo.VisualID;
        info.Name1 = qualityColor(displayInfoEntry) + displayInfo.Name1;
        info.Name2 = displayInfo.Name2;
        info.Name3 = displayInfo.Name3;
        info.Name4 = displayInfo.Name4;
        info.Name5 = displayInfo.Name5;
        info.Name6 = displayInfo.Name6;
        info.Name7 = displayInfo.Name7;
        info.Flags = displayInfo.Flags;
        info.Unk1 = displayInfo.Unk1;
        info.Unk2 = displayInfo.Unk2;
        info.Unk3 = displayInfo.Unk3;
        info.UnkInt1 = displayInfo.UnkInt1;
        info.UnkInt2 = displayInfo.UnkInt2;
        info.UnkInt3 = displayInfo.UnkInt3;

        for (var v = 0; v < displayInfo.Visuals.Count; v++)
        {
            var displayVisual = displayInfo.Visuals[v];

            var visual = new BpayVisual
            {
                Name = displayVisual.Name,
                DisplayId = displayVisual.DisplayId,
                VisualId = displayVisual.VisualId,
                Unk = displayVisual.Unk
            };

            info.Visuals.Add(visual);
        }

        if (displayInfo.Flags != 0)
            info.Flags = displayInfo.Flags;

        return Tuple.Create(true, info);
    }
}
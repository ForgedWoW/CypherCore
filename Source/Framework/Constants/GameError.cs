﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum GameError : uint
{
    System = 0,
    InternalError = 1,
    InvFull = 2,
    BankFull = 3,
    CantEquipLevelI = 4,
    CantEquipSkill = 5,
    CantEquipEver = 6,
    CantEquipRank = 7,
    CantEquipRating = 8,
    CantEquipReputation = 9,
    ProficiencyNeeded = 10,
    WrongSlot = 11,
    CantEquipNeedTalent = 12,
    BagFull = 13,
    InternalBagError = 14,
    DestroyNonemptyBag = 15,
    BagInBag = 16,
    TooManySpecialBags = 17,
    TradeEquippedBag = 18,
    AmmoOnly = 19,
    NoSlotAvailable = 20,
    WrongBagType = 21,
    ReagentbagWrongSlot = 22,
    SlotOnlyReagentbag = 23,
    ReagentbagItemType = 24,
    ItemMaxCount = 25,
    NotEquippable = 26,
    CantStack = 27,
    CantSwap = 28,
    SlotEmpty = 29,
    ItemNotFound = 30,
    TooFewToSplit = 31,
    SplitFailed = 32,
    NotABag = 33,
    NotOwner = 34,
    OnlyOneQuiver = 35,
    NoBankSlot = 36,
    NoBankHere = 37,
    ItemLocked = 38,
    Handed2Equipped = 39,
    VendorNotInterested = 40,
    VendorRefuseScrappableAzerite = 41,
    VendorHatesYou = 42,
    VendorSoldOut = 43,
    VendorTooFar = 44,
    VendorDoesntBuy = 45,
    NotEnoughMoney = 46,
    ReceiveItemS = 47,
    DropBoundItem = 48,
    TradeBoundItem = 49,
    TradeQuestItem = 50,
    TradeTempEnchantBound = 51,
    TradeGroundItem = 52,
    TradeBag = 53,
    TradeFactionSpecific = 54,
    SpellFailedS = 55,
    ItemCooldown = 56,
    PotionCooldown = 57,
    FoodCooldown = 58,
    SpellCooldown = 59,
    AbilityCooldown = 60,
    SpellAlreadyKnownS = 61,
    PetSpellAlreadyKnownS = 62,
    ProficiencyGainedS = 63,
    SkillGainedS = 64,
    SkillUpSi = 65,
    LearnSpellS = 66,
    LearnAbilityS = 67,
    LearnPassiveS = 68,
    LearnRecipeS = 69,
    ProfessionsRecipeDiscoveryS = 70,
    LearnCompanionS = 71,
    LearnMountS = 72,
    LearnToyS = 73,
    LearnHeirloomS = 74,
    LearnTransmogS = 75,
    CompletedTransmogSetS = 76,
    AppearanceAlreadyLearned = 77,
    RevokeTransmogS = 78,
    InvitePlayerS = 79,
    SuggestInvitePlayerS = 80,
    InformSuggestInviteS = 81,
    InformSuggestInviteSs = 82,
    RequestJoinPlayerS = 83,
    InviteSelf = 84,
    InvitedToGroupSs = 85,
    InvitedAlreadyInGroupSs = 86,
    AlreadyInGroupS = 87,
    RequestedInviteToGroupSs = 88,
    CrossRealmRaidInvite = 89,
    PlayerBusyS = 90,
    NewLeaderS = 91,
    NewLeaderYou = 92,
    NewGuideS = 93,
    NewGuideYou = 94,
    LeftGroupS = 95,
    LeftGroupYou = 96,
    GroupDisbanded = 97,
    DeclineGroupS = 98,
    DeclineGroupRequestS = 99,
    JoinedGroupS = 100,
    UninviteYou = 101,
    BadPlayerNameS = 102,
    NotInGroup = 103,
    TargetNotInGroupS = 104,
    TargetNotInInstanceS = 105,
    NotInInstanceGroup = 106,
    GroupFull = 107,
    NotLeader = 108,
    PlayerDiedS = 109,
    GuildCreateS = 110,
    GuildInviteS = 111,
    InvitedToGuildSss = 112,
    AlreadyInGuildS = 113,
    AlreadyInvitedToGuildS = 114,
    InvitedToGuild = 115,
    AlreadyInGuild = 116,
    GuildAccept = 117,
    GuildDeclineS = 118,
    GuildDeclineAutoS = 119,
    GuildPermissions = 120,
    GuildJoinS = 121,
    GuildFounderS = 122,
    GuildPromoteSss = 123,
    GuildDemoteSs = 124,
    GuildDemoteSss = 125,
    GuildInviteSelf = 126,
    GuildQuitS = 127,
    GuildLeaveS = 128,
    GuildRemoveSs = 129,
    GuildRemoveSelf = 130,
    GuildDisbandS = 131,
    GuildDisbandSelf = 132,
    GuildLeaderS = 133,
    GuildLeaderSelf = 134,
    GuildPlayerNotFoundS = 135,
    GuildPlayerNotInGuildS = 136,
    GuildPlayerNotInGuild = 137,
    GuildCantPromoteS = 138,
    GuildCantDemoteS = 139,
    GuildNotInAGuild = 140,
    GuildInternal = 141,
    GuildLeaderIsS = 142,
    GuildLeaderChangedSs = 143,
    GuildDisbanded = 144,
    GuildNotAllied = 145,
    GuildLeaderLeave = 146,
    GuildRanksLocked = 147,
    GuildRankInUse = 148,
    GuildRankTooHighS = 149,
    GuildRankTooLowS = 150,
    GuildNameExistsS = 151,
    GuildWithdrawLimit = 152,
    GuildNotEnoughMoney = 153,
    GuildTooMuchMoney = 154,
    GuildBankConjuredItem = 155,
    GuildBankEquippedItem = 156,
    GuildBankBoundItem = 157,
    GuildBankQuestItem = 158,
    GuildBankWrappedItem = 159,
    GuildBankFull = 160,
    GuildBankWrongTab = 161,
    NoGuildCharter = 162,
    OutOfRange = 163,
    PlayerDead = 164,
    ClientLockedOut = 165,
    ClientOnTransport = 166,
    KilledByS = 167,
    LootLocked = 168,
    LootTooFar = 169,
    LootDidntKill = 170,
    LootBadFacing = 171,
    LootNotstanding = 172,
    LootStunned = 173,
    LootNoUi = 174,
    LootWhileInvulnerable = 175,
    NoLoot = 176,
    QuestAcceptedS = 177,
    QuestCompleteS = 178,
    QuestFailedS = 179,
    QuestFailedBagFullS = 180,
    QuestFailedMaxCountS = 181,
    QuestFailedLowLevel = 182,
    QuestFailedMissingItems = 183,
    QuestFailedWrongRace = 184,
    QuestFailedNotEnoughMoney = 185,
    QuestFailedExpansion = 186,
    QuestOnlyOneTimed = 187,
    QuestNeedPrereqs = 188,
    QuestNeedPrereqsCustom = 189,
    QuestAlreadyOn = 190,
    QuestAlreadyDone = 191,
    QuestAlreadyDoneDaily = 192,
    QuestHasInProgress = 193,
    QuestRewardExpI = 194,
    QuestRewardMoneyS = 195,
    QuestMustChoose = 196,
    QuestLogFull = 197,
    CombatDamageSsi = 198,
    InspectS = 199,
    CantUseItem = 200,
    CantUseItemInArena = 201,
    CantUseItemInRatedBattleground = 202,
    MustEquipItem = 203,
    PassiveAbility = 204,
    H2skillnotfound = 205,
    NoAttackTarget = 206,
    InvalidAttackTarget = 207,
    AttackPvpTargetWhileUnflagged = 208,
    AttackStunned = 209,
    AttackPacified = 210,
    AttackMounted = 211,
    AttackFleeing = 212,
    AttackConfused = 213,
    AttackCharmed = 214,
    AttackDead = 215,
    AttackPreventedByMechanicS = 216,
    AttackChannel = 217,
    Taxisamenode = 218,
    Taxinosuchpath = 219,
    Taxiunspecifiedservererror = 220,
    Taxinotenoughmoney = 221,
    Taxitoofaraway = 222,
    Taxinovendornearby = 223,
    Taxinotvisited = 224,
    Taxiplayerbusy = 225,
    Taxiplayeralreadymounted = 226,
    Taxiplayershapeshifted = 227,
    Taxiplayermoving = 228,
    Taxinopaths = 229,
    Taxinoteligible = 230,
    Taxinotstanding = 231,
    Taxiincombat = 232,
    NoReplyTarget = 233,
    GenericNoTarget = 234,
    InitiateTradeS = 235,
    TradeRequestS = 236,
    TradeBlockedS = 237,
    TradeTargetDead = 238,
    TradeTooFar = 239,
    TradeCancelled = 240,
    TradeComplete = 241,
    TradeBagFull = 242,
    TradeTargetBagFull = 243,
    TradeMaxCountExceeded = 244,
    TradeTargetMaxCountExceeded = 245,
    InventoryTradeTooManyUniqueItem = 246,
    AlreadyTrading = 247,
    MountInvalidmountee = 248,
    MountToofaraway = 249,
    MountAlreadymounted = 250,
    MountNotmountable = 251,
    MountNotyourpet = 252,
    MountOther = 253,
    MountLooting = 254,
    MountRacecantmount = 255,
    MountShapeshifted = 256,
    MountNoFavorites = 257,
    MountNoMounts = 258,
    DismountNopet = 259,
    DismountNotmounted = 260,
    DismountNotyourpet = 261,
    SpellFailedTotems = 262,
    SpellFailedReagents = 263,
    SpellFailedReagentsGeneric = 264,
    SpellFailedOptionalReagents = 265,
    CantTradeGold = 266,
    SpellFailedEquippedItem = 267,
    SpellFailedEquippedItemClassS = 268,
    SpellFailedShapeshiftFormS = 269,
    SpellFailedAnotherInProgress = 270,
    Badattackfacing = 271,
    Badattackpos = 272,
    ChestInUse = 273,
    UseCantOpen = 274,
    UseLocked = 275,
    DoorLocked = 276,
    ButtonLocked = 277,
    UseLockedWithItemS = 278,
    UseLockedWithSpellS = 279,
    UseLockedWithSpellKnownSi = 280,
    UseTooFar = 281,
    UseBadAngle = 282,
    UseObjectMoving = 283,
    UseSpellFocus = 284,
    UseDestroyed = 285,
    SetLootFreeforall = 286,
    SetLootRoundrobin = 287,
    SetLootMaster = 288,
    SetLootGroup = 289,
    SetLootThresholdS = 290,
    NewLootMasterS = 291,
    SpecifyMasterLooter = 292,
    LootSpecChangedS = 293,
    TameFailed = 294,
    ChatWhileDead = 295,
    ChatPlayerNotFoundS = 296,
    Newtaxipath = 297,
    NoPet = 298,
    Notyourpet = 299,
    PetNotRenameable = 300,
    QuestObjectiveCompleteS = 301,
    QuestUnknownComplete = 302,
    QuestAddKillSii = 303,
    QuestAddFoundSii = 304,
    QuestAddItemSii = 305,
    QuestAddPlayerKillSii = 306,
    Cannotcreatedirectory = 307,
    Cannotcreatefile = 308,
    PlayerWrongFaction = 309,
    PlayerIsNeutral = 310,
    BankslotFailedTooMany = 311,
    BankslotInsufficientFunds = 312,
    BankslotNotbanker = 313,
    FriendDbError = 314,
    FriendListFull = 315,
    FriendAddedS = 316,
    BattletagFriendAddedS = 317,
    FriendOnlineSs = 318,
    FriendOfflineS = 319,
    FriendNotFound = 320,
    FriendWrongFaction = 321,
    FriendRemovedS = 322,
    BattletagFriendRemovedS = 323,
    FriendError = 324,
    FriendAlreadyS = 325,
    FriendSelf = 326,
    FriendDeleted = 327,
    IgnoreFull = 328,
    IgnoreSelf = 329,
    IgnoreNotFound = 330,
    IgnoreAlreadyS = 331,
    IgnoreAddedS = 332,
    IgnoreRemovedS = 333,
    IgnoreAmbiguous = 334,
    IgnoreDeleted = 335,
    OnlyOneBolt = 336,
    OnlyOneAmmo = 337,
    SpellFailedEquippedSpecificItem = 338,
    WrongBagTypeSubclass = 339,
    CantWrapStackable = 340,
    CantWrapEquipped = 341,
    CantWrapWrapped = 342,
    CantWrapBound = 343,
    CantWrapUnique = 344,
    CantWrapBags = 345,
    OutOfMana = 346,
    OutOfRage = 347,
    OutOfFocus = 348,
    OutOfEnergy = 349,
    OutOfChi = 350,
    OutOfHealth = 351,
    OutOfRunes = 352,
    OutOfRunicPower = 353,
    OutOfSoulShards = 354,
    OutOfLunarPower = 355,
    OutOfHolyPower = 356,
    OutOfMaelstrom = 357,
    OutOfComboPoints = 358,
    OutOfInsanity = 359,
    OutOfEssence = 360,
    OutOfArcaneCharges = 361,
    OutOfFury = 362,
    OutOfPain = 363,
    OutOfPowerDisplay = 364,
    LootGone = 365,
    MountForceddismount = 366,
    AutofollowTooFar = 367,
    UnitNotFound = 368,
    InvalidFollowTarget = 369,
    InvalidFollowPvpCombat = 370,
    InvalidFollowTargetPvpCombat = 371,
    InvalidInspectTarget = 372,
    GuildemblemSuccess = 373,
    GuildemblemInvalidTabardColors = 374,
    GuildemblemNoguild = 375,
    GuildemblemNotguildmaster = 376,
    GuildemblemNotenoughmoney = 377,
    GuildemblemInvalidvendor = 378,
    EmblemerrorNotabardgeoset = 379,
    SpellOutOfRange = 380,
    CommandNeedsTarget = 381,
    NoammoS = 382,
    Toobusytofollow = 383,
    DuelRequested = 384,
    DuelCancelled = 385,
    Deathbindalreadybound = 386,
    DeathbindSuccessS = 387,
    Noemotewhilerunning = 388,
    ZoneExplored = 389,
    ZoneExploredXp = 390,
    InvalidItemTarget = 391,
    InvalidQuestTarget = 392,
    IgnoringYouS = 393,
    FishNotHooked = 394,
    FishEscaped = 395,
    SpellFailedNotunsheathed = 396,
    PetitionOfferedS = 397,
    PetitionSigned = 398,
    PetitionSignedS = 399,
    PetitionDeclinedS = 400,
    PetitionAlreadySigned = 401,
    PetitionRestrictedAccountTrial = 402,
    PetitionAlreadySignedOther = 403,
    PetitionInGuild = 404,
    PetitionCreator = 405,
    PetitionNotEnoughSignatures = 406,
    PetitionNotSameServer = 407,
    PetitionFull = 408,
    PetitionAlreadySignedByS = 409,
    GuildNameInvalid = 410,
    SpellUnlearnedS = 411,
    PetSpellRooted = 412,
    PetSpellAffectingCombat = 413,
    PetSpellOutOfRange = 414,
    PetSpellNotBehind = 415,
    PetSpellTargetsDead = 416,
    PetSpellDead = 417,
    PetSpellNopath = 418,
    ItemCantBeDestroyed = 419,
    TicketAlreadyExists = 420,
    TicketCreateError = 421,
    TicketUpdateError = 422,
    TicketDbError = 423,
    TicketNoText = 424,
    TicketTextTooLong = 425,
    ObjectIsBusy = 426,
    ExhaustionWellrested = 427,
    ExhaustionRested = 428,
    ExhaustionNormal = 429,
    ExhaustionTired = 430,
    ExhaustionExhausted = 431,
    NoItemsWhileShapeshifted = 432,
    CantInteractShapeshifted = 433,
    RealmNotFound = 434,
    MailQuestItem = 435,
    MailBoundItem = 436,
    MailConjuredItem = 437,
    MailBag = 438,
    MailToSelf = 439,
    MailTargetNotFound = 440,
    MailDatabaseError = 441,
    MailDeleteItemError = 442,
    MailWrappedCod = 443,
    MailCantSendRealm = 444,
    MailTempReturnOutage = 445,
    MailRecepientCantReceiveMail = 446,
    MailSent = 447,
    MailTargetIsTrial = 448,
    NotHappyEnough = 449,
    UseCantImmune = 450,
    CantBeDisenchanted = 451,
    CantUseDisarmed = 452,
    AuctionDatabaseError = 453,
    AuctionHigherBid = 454,
    AuctionAlreadyBid = 455,
    AuctionOutbidS = 456,
    AuctionWonS = 457,
    AuctionRemovedS = 458,
    AuctionBidPlaced = 459,
    LogoutFailed = 460,
    QuestPushSuccessS = 461,
    QuestPushInvalidS = 462,
    QuestPushInvalidToRecipientS = 463,
    QuestPushAcceptedS = 464,
    QuestPushDeclinedS = 465,
    QuestPushBusyS = 466,
    QuestPushDeadS = 467,
    QuestPushDeadToRecipientS = 468,
    QuestPushLogFullS = 469,
    QuestPushLogFullToRecipientS = 470,
    QuestPushOnquestS = 471,
    QuestPushOnquestToRecipientS = 472,
    QuestPushAlreadyDoneS = 473,
    QuestPushAlreadyDoneToRecipientS = 474,
    QuestPushNotDailyS = 475,
    QuestPushTimerExpiredS = 476,
    QuestPushNotInPartyS = 477,
    QuestPushDifferentServerDailyS = 478,
    QuestPushDifferentServerDailyToRecipientS = 479,
    QuestPushNotAllowedS = 480,
    QuestPushPrerequisiteS = 481,
    QuestPushPrerequisiteToRecipientS = 482,
    QuestPushLowLevelS = 483,
    QuestPushLowLevelToRecipientS = 484,
    QuestPushHighLevelS = 485,
    QuestPushHighLevelToRecipientS = 486,
    QuestPushClassS = 487,
    QuestPushClassToRecipientS = 488,
    QuestPushRaceS = 489,
    QuestPushRaceToRecipientS = 490,
    QuestPushLowFactionS = 491,
    QuestPushLowFactionToRecipientS = 492,
    QuestPushExpansionS = 493,
    QuestPushExpansionToRecipientS = 494,
    QuestPushNotGarrisonOwnerS = 495,
    QuestPushNotGarrisonOwnerToRecipientS = 496,
    QuestPushWrongCovenantS = 497,
    QuestPushWrongCovenantToRecipientS = 498,
    QuestPushNewPlayerExperienceS = 499,
    QuestPushNewPlayerExperienceToRecipientS = 500,
    QuestPushWrongFactionS = 501,
    QuestPushWrongFactionToRecipientS = 502,
    QuestPushCrossFactionRestrictedS = 503,
    RaidGroupLowlevel = 504,
    RaidGroupOnly = 505,
    RaidGroupFull = 506,
    RaidGroupRequirementsUnmatch = 507,
    CorpseIsNotInInstance = 508,
    PvpKillHonorable = 509,
    PvpKillDishonorable = 510,
    SpellFailedAlreadyAtFullHealth = 511,
    SpellFailedAlreadyAtFullMana = 512,
    SpellFailedAlreadyAtFullPowerS = 513,
    AutolootMoneyS = 514,
    GenericStunned = 515,
    GenericThrottle = 516,
    ClubFinderSearchingTooFast = 517,
    TargetStunned = 518,
    MustRepairDurability = 519,
    RaidYouJoined = 520,
    RaidYouLeft = 521,
    InstanceGroupJoinedWithParty = 522,
    InstanceGroupJoinedWithRaid = 523,
    RaidMemberAddedS = 524,
    RaidMemberRemovedS = 525,
    InstanceGroupAddedS = 526,
    InstanceGroupRemovedS = 527,
    ClickOnItemToFeed = 528,
    TooManyChatChannels = 529,
    LootRollPending = 530,
    LootPlayerNotFound = 531,
    NotInRaid = 532,
    LoggingOut = 533,
    TargetLoggingOut = 534,
    NotWhileMounted = 535,
    NotWhileShapeshifted = 536,
    NotInCombat = 537,
    NotWhileDisarmed = 538,
    PetBroken = 539,
    TalentWipeError = 540,
    SpecWipeError = 541,
    GlyphWipeError = 542,
    PetSpecWipeError = 543,
    FeignDeathResisted = 544,
    MeetingStoneInQueueS = 545,
    MeetingStoneLeftQueueS = 546,
    MeetingStoneOtherMemberLeft = 547,
    MeetingStonePartyKickedFromQueue = 548,
    MeetingStoneMemberStillInQueue = 549,
    MeetingStoneSuccess = 550,
    MeetingStoneInProgress = 551,
    MeetingStoneMemberAddedS = 552,
    MeetingStoneGroupFull = 553,
    MeetingStoneNotLeader = 554,
    MeetingStoneInvalidLevel = 555,
    MeetingStoneTargetNotInParty = 556,
    MeetingStoneTargetInvalidLevel = 557,
    MeetingStoneMustBeLeader = 558,
    MeetingStoneNoRaidGroup = 559,
    MeetingStoneNeedParty = 560,
    MeetingStoneNotFound = 561,
    MeetingStoneTargetInVehicle = 562,
    GuildemblemSame = 563,
    EquipTradeItem = 564,
    PvpToggleOn = 565,
    PvpToggleOff = 566,
    GroupJoinBattlegroundDeserters = 567,
    GroupJoinBattlegroundDead = 568,
    GroupJoinBattlegroundS = 569,
    GroupJoinBattlegroundFail = 570,
    GroupJoinBattlegroundTooMany = 571,
    SoloJoinBattlegroundS = 572,
    JoinSingleScenarioS = 573,
    BattlegroundTooManyQueues = 574,
    BattlegroundCannotQueueForRated = 575,
    BattledgroundQueuedForRated = 576,
    BattlegroundTeamLeftQueue = 577,
    BattlegroundNotInBattleground = 578,
    AlreadyInArenaTeamS = 579,
    InvalidPromotionCode = 580,
    BgPlayerJoinedSs = 581,
    BgPlayerLeftS = 582,
    RestrictedAccount = 583,
    RestrictedAccountTrial = 584,
    PlayTimeExceeded = 585,
    ApproachingPartialPlayTime = 586,
    ApproachingPartialPlayTime2 = 587,
    ApproachingNoPlayTime = 588,
    ApproachingNoPlayTime2 = 589,
    UnhealthyTime = 590,
    ChatRestrictedTrial = 591,
    ChatThrottled = 592,
    MailReachedCap = 593,
    InvalidRaidTarget = 594,
    RaidLeaderReadyCheckStartS = 595,
    ReadyCheckInProgress = 596,
    ReadyCheckThrottled = 597,
    DungeonDifficultyFailed = 598,
    DungeonDifficultyChangedS = 599,
    TradeWrongRealm = 600,
    TradeNotOnTaplist = 601,
    ChatPlayerAmbiguousS = 602,
    LootCantLootThatNow = 603,
    LootMasterInvFull = 604,
    LootMasterUniqueItem = 605,
    LootMasterOther = 606,
    FilteringYouS = 607,
    UsePreventedByMechanicS = 608,
    ItemUniqueEquippable = 609,
    LfgLeaderIsLfmS = 610,
    LfgPending = 611,
    CantSpeakLangage = 612,
    VendorMissingTurnins = 613,
    BattlegroundNotInTeam = 614,
    NotInBattleground = 615,
    NotEnoughHonorPoints = 616,
    NotEnoughArenaPoints = 617,
    SocketingRequiresMetaGem = 618,
    SocketingMetaGemOnlyInMetaslot = 619,
    SocketingRequiresHydraulicGem = 620,
    SocketingHydraulicGemOnlyInHydraulicslot = 621,
    SocketingRequiresCogwheelGem = 622,
    SocketingCogwheelGemOnlyInCogwheelslot = 623,
    SocketingItemTooLowLevel = 624,
    ItemMaxCountSocketed = 625,
    SystemDisabled = 626,
    QuestFailedTooManyDailyQuestsI = 627,
    ItemMaxCountEquippedSocketed = 628,
    ItemUniqueEquippableSocketed = 629,
    UserSquelched = 630,
    AccountSilenced = 631,
    PartyMemberSilenced = 632,
    PartyMemberSilencedLfgDelist = 633,
    TooMuchGold = 634,
    NotBarberSitting = 635,
    QuestFailedCais = 636,
    InviteRestrictedTrial = 637,
    VoiceIgnoreFull = 638,
    VoiceIgnoreSelf = 639,
    VoiceIgnoreNotFound = 640,
    VoiceIgnoreAlreadyS = 641,
    VoiceIgnoreAddedS = 642,
    VoiceIgnoreRemovedS = 643,
    VoiceIgnoreAmbiguous = 644,
    VoiceIgnoreDeleted = 645,
    UnknownMacroOptionS = 646,
    NotDuringArenaMatch = 647,
    NotInRatedBattleground = 648,
    PlayerSilenced = 649,
    PlayerUnsilenced = 650,
    ComsatDisconnect = 651,
    ComsatReconnectAttempt = 652,
    ComsatConnectFail = 653,
    MailInvalidAttachmentSlot = 654,
    MailTooManyAttachments = 655,
    MailInvalidAttachment = 656,
    MailAttachmentExpired = 657,
    VoiceChatParentalDisableMic = 658,
    ProfaneChatName = 659,
    PlayerSilencedEcho = 660,
    PlayerUnsilencedEcho = 661,
    LootCantLootThat = 662,
    ArenaExpiredCais = 663,
    GroupActionThrottled = 664,
    AlreadyPickpocketed = 665,
    NameInvalid = 666,
    NameNoName = 667,
    NameTooShort = 668,
    NameTooLong = 669,
    NameMixedLanguages = 670,
    NameProfane = 671,
    NameReserved = 672,
    NameThreeConsecutive = 673,
    NameInvalidSpace = 674,
    NameConsecutiveSpaces = 675,
    NameRussianConsecutiveSilentCharacters = 676,
    NameRussianSilentCharacterAtBeginningOrEnd = 677,
    NameDeclensionDoesntMatchBaseName = 678,
    RecruitAFriendNotLinked = 679,
    RecruitAFriendNotNow = 680,
    RecruitAFriendSummonLevelMax = 681,
    RecruitAFriendSummonCooldown = 682,
    RecruitAFriendSummonOffline = 683,
    RecruitAFriendInsufExpanLvl = 684,
    RecruitAFriendMapIncomingTransferNotAllowed = 685,
    NotSameAccount = 686,
    BadOnUseEnchant = 687,
    TradeSelf = 688,
    TooManySockets = 689,
    ItemMaxLimitCategoryCountExceededIs = 690,
    TradeTargetMaxLimitCategoryCountExceededIs = 691,
    ItemMaxLimitCategorySocketedExceededIs = 692,
    ItemMaxLimitCategoryEquippedExceededIs = 693,
    ShapeshiftFormCannotEquip = 694,
    ItemInventoryFullSatchel = 695,
    ScalingStatItemLevelExceeded = 696,
    ScalingStatItemLevelTooLow = 697,
    PurchaseLevelTooLow = 698,
    GroupSwapFailed = 699,
    InviteInCombat = 700,
    InvalidGlyphSlot = 701,
    GenericNoValidTargets = 702,
    CalendarEventAlertS = 703,
    PetLearnSpellS = 704,
    PetLearnAbilityS = 705,
    PetSpellUnlearnedS = 706,
    InviteUnknownRealm = 707,
    InviteNoPartyServer = 708,
    InvitePartyBusy = 709,
    InvitePartyBusyPendingRequest = 710,
    InvitePartyBusyPendingSuggest = 711,
    PartyTargetAmbiguous = 712,
    PartyLfgInviteRaidLocked = 713,
    PartyLfgBootLimit = 714,
    PartyLfgBootCooldownS = 715,
    PartyLfgBootNotEligibleS = 716,
    PartyLfgBootInpatientTimerS = 717,
    PartyLfgBootInProgress = 718,
    PartyLfgBootTooFewPlayers = 719,
    PartyLfgBootVoteSucceeded = 720,
    PartyLfgBootVoteFailed = 721,
    PartyLfgBootInCombat = 722,
    PartyLfgBootDungeonComplete = 723,
    PartyLfgBootLootRolls = 724,
    PartyLfgBootVoteRegistered = 725,
    PartyPrivateGroupOnly = 726,
    PartyLfgTeleportInCombat = 727,
    RaidDisallowedByLevel = 728,
    RaidDisallowedByCrossRealm = 729,
    PartyRoleNotAvailable = 730,
    JoinLfgObjectFailed = 731,
    LfgRemovedLevelup = 732,
    LfgRemovedXpToggle = 733,
    LfgRemovedFactionChange = 734,
    BattlegroundInfoThrottled = 735,
    BattlegroundAlreadyIn = 736,
    ArenaTeamChangeFailedQueued = 737,
    ArenaTeamPermissions = 738,
    NotWhileFalling = 739,
    NotWhileMoving = 740,
    NotWhileFatigued = 741,
    MaxSockets = 742,
    MultiCastActionTotemS = 743,
    BattlegroundJoinLevelup = 744,
    RemoveFromPvpQueueXpGain = 745,
    BattlegroundJoinXpGain = 746,
    BattlegroundJoinMercenary = 747,
    BattlegroundJoinTooManyHealers = 748,
    BattlegroundJoinRatedTooManyHealers = 749,
    BattlegroundJoinTooManyTanks = 750,
    BattlegroundJoinTooManyDamage = 751,
    RaidDifficultyFailed = 752,
    RaidDifficultyChangedS = 753,
    LegacyRaidDifficultyChangedS = 754,
    RaidLockoutChangedS = 755,
    RaidConvertedToParty = 756,
    PartyConvertedToRaid = 757,
    PlayerDifficultyChangedS = 758,
    GmresponseDbError = 759,
    BattlegroundJoinRangeIndex = 760,
    ArenaJoinRangeIndex = 761,
    RemoveFromPvpQueueFactionChange = 762,
    BattlegroundJoinFailed = 763,
    BattlegroundJoinNoValidSpecForRole = 764,
    BattlegroundJoinRespec = 765,
    BattlegroundInvitationDeclined = 766,
    BattlegroundJoinTimedOut = 767,
    BattlegroundDupeQueue = 768,
    BattlegroundJoinMustCompleteQuest = 769,
    InBattlegroundRespec = 770,
    MailLimitedDurationItem = 771,
    YellRestrictedTrial = 772,
    ChatRaidRestrictedTrial = 773,
    LfgRoleCheckFailed = 774,
    LfgRoleCheckFailedTimeout = 775,
    LfgRoleCheckFailedNotViable = 776,
    LfgReadyCheckFailed = 777,
    LfgReadyCheckFailedTimeout = 778,
    LfgGroupFull = 779,
    LfgNoLfgObject = 780,
    LfgNoSlotsPlayer = 781,
    LfgNoSlotsParty = 782,
    LfgNoSpec = 783,
    LfgMismatchedSlots = 784,
    LfgMismatchedSlotsLocalXrealm = 785,
    LfgPartyPlayersFromDifferentRealms = 786,
    LfgMembersNotPresent = 787,
    LfgGetInfoTimeout = 788,
    LfgInvalidSlot = 789,
    LfgDeserterPlayer = 790,
    LfgDeserterParty = 791,
    LfgDead = 792,
    LfgRandomCooldownPlayer = 793,
    LfgRandomCooldownParty = 794,
    LfgTooManyMembers = 795,
    LfgTooFewMembers = 796,
    LfgProposalFailed = 797,
    LfgProposalDeclinedSelf = 798,
    LfgProposalDeclinedParty = 799,
    LfgNoSlotsSelected = 800,
    LfgNoRolesSelected = 801,
    LfgRoleCheckInitiated = 802,
    LfgReadyCheckInitiated = 803,
    LfgPlayerDeclinedRoleCheck = 804,
    LfgPlayerDeclinedReadyCheck = 805,
    LfgJoinedQueue = 806,
    LfgJoinedFlexQueue = 807,
    LfgJoinedRfQueue = 808,
    LfgJoinedScenarioQueue = 809,
    LfgJoinedWorldPvpQueue = 810,
    LfgJoinedBattlefieldQueue = 811,
    LfgJoinedList = 812,
    LfgLeftQueue = 813,
    LfgLeftList = 814,
    LfgRoleCheckAborted = 815,
    LfgReadyCheckAborted = 816,
    LfgCantUseBattleground = 817,
    LfgCantUseDungeons = 818,
    LfgReasonTooManyLfg = 819,
    LfgFarmLimit = 820,
    LfgNoCrossFactionParties = 821,
    InvalidTeleportLocation = 822,
    TooFarToInteract = 823,
    BattlegroundPlayersFromDifferentRealms = 824,
    DifficultyChangeCooldownS = 825,
    DifficultyChangeCombatCooldownS = 826,
    DifficultyChangeWorldstate = 827,
    DifficultyChangeEncounter = 828,
    DifficultyChangeCombat = 829,
    DifficultyChangePlayerBusy = 830,
    DifficultyChangePlayerOnVehicle = 831,
    DifficultyChangeAlreadyStarted = 832,
    DifficultyChangeOtherHeroicS = 833,
    DifficultyChangeHeroicInstanceAlreadyRunning = 834,
    ArenaTeamPartySize = 835,
    SoloShuffleWargameGroupSize = 836,
    SoloShuffleWargameGroupComp = 837,
    SoloShuffleMinItemLevel = 838,
    PvpPlayerAbandoned = 839,
    QuestForceRemovedS = 840,
    AttackNoActions = 841,
    InRandomBg = 842,
    InNonRandomBg = 843,
    BnFriendSelf = 844,
    BnFriendAlready = 845,
    BnFriendBlocked = 846,
    BnFriendListFull = 847,
    BnFriendRequestSent = 848,
    BnBroadcastThrottle = 849,
    BgDeveloperOnly = 850,
    CurrencySpellSlotMismatch = 851,
    CurrencyNotTradable = 852,
    RequiresExpansionS = 853,
    QuestFailedSpell = 854,
    TalentFailedUnspentTalentPoints = 855,
    TalentFailedNotEnoughTalentsInPrimaryTree = 856,
    TalentFailedNoPrimaryTreeSelected = 857,
    TalentFailedCantRemoveTalent = 858,
    TalentFailedUnknown = 859,
    TalentFailedInCombat = 860,
    TalentFailedInPvpMatch = 861,
    TalentFailedInMythicPlus = 862,
    WargameRequestFailure = 863,
    RankRequiresAuthenticator = 864,
    GuildBankVoucherFailed = 865,
    WargameRequestSent = 866,
    RequiresAchievementI = 867,
    RefundResultExceedMaxCurrency = 868,
    CantBuyQuantity = 869,
    ItemIsBattlePayLocked = 870,
    PartyAlreadyInBattlegroundQueue = 871,
    PartyConfirmingBattlegroundQueue = 872,
    BattlefieldTeamPartySize = 873,
    InsuffTrackedCurrencyIs = 874,
    NotOnTournamentRealm = 875,
    GuildTrialAccountTrial = 876,
    GuildTrialAccountVeteran = 877,
    GuildUndeletableDueToLevel = 878,
    CantDoThatInAGroup = 879,
    GuildLeaderReplaced = 880,
    TransmogrifyCantEquip = 881,
    TransmogrifyInvalidItemType = 882,
    TransmogrifyNotSoulbound = 883,
    TransmogrifyInvalidSource = 884,
    TransmogrifyInvalidDestination = 885,
    TransmogrifyMismatch = 886,
    TransmogrifyLegendary = 887,
    TransmogrifySameItem = 888,
    TransmogrifySameAppearance = 889,
    TransmogrifyNotEquipped = 890,
    VoidDepositFull = 891,
    VoidWithdrawFull = 892,
    VoidStorageWrapped = 893,
    VoidStorageStackable = 894,
    VoidStorageUnbound = 895,
    VoidStorageRepair = 896,
    VoidStorageCharges = 897,
    VoidStorageQuest = 898,
    VoidStorageConjured = 899,
    VoidStorageMail = 900,
    VoidStorageBag = 901,
    VoidTransferStorageFull = 902,
    VoidTransferInvFull = 903,
    VoidTransferInternalError = 904,
    VoidTransferItemInvalid = 905,
    DifficultyDisabledInLfg = 906,
    VoidStorageUnique = 907,
    VoidStorageLoot = 908,
    VoidStorageHoliday = 909,
    VoidStorageDuration = 910,
    VoidStorageLoadFailed = 911,
    VoidStorageInvalidItem = 912,
    ParentalControlsChatMuted = 913,
    SorStartExperienceIncomplete = 914,
    SorInvalidEmail = 915,
    SorInvalidComment = 916,
    ChallengeModeResetCooldownS = 917,
    ChallengeModeResetKeystone = 918,
    PetJournalAlreadyInLoadout = 919,
    ReportSubmittedSuccessfully = 920,
    ReportSubmissionFailed = 921,
    SuggestionSubmittedSuccessfully = 922,
    BugSubmittedSuccessfully = 923,
    ChallengeModeEnabled = 924,
    ChallengeModeDisabled = 925,
    PetbattleCreateFailed = 926,
    PetbattleNotHere = 927,
    PetbattleNotHereOnTransport = 928,
    PetbattleNotHereUnevenGround = 929,
    PetbattleNotHereObstructed = 930,
    PetbattleNotWhileInCombat = 931,
    PetbattleNotWhileDead = 932,
    PetbattleNotWhileFlying = 933,
    PetbattleTargetInvalid = 934,
    PetbattleTargetOutOfRange = 935,
    PetbattleTargetNotCapturable = 936,
    PetbattleNotATrainer = 937,
    PetbattleDeclined = 938,
    PetbattleInBattle = 939,
    PetbattleInvalidLoadout = 940,
    PetbattleAllPetsDead = 941,
    PetbattleNoPetsInSlots = 942,
    PetbattleNoAccountLock = 943,
    PetbattleWildPetTapped = 944,
    PetbattleRestrictedAccount = 945,
    PetbattleOpponentNotAvailable = 946,
    PetbattleNotWhileInMatchedBattle = 947,
    CantHaveMorePetsOfThatType = 948,
    CantHaveMorePets = 949,
    PvpMapNotFound = 950,
    PvpMapNotSet = 951,
    PetbattleQueueQueued = 952,
    PetbattleQueueAlreadyQueued = 953,
    PetbattleQueueJoinFailed = 954,
    PetbattleQueueJournalLock = 955,
    PetbattleQueueRemoved = 956,
    PetbattleQueueProposalDeclined = 957,
    PetbattleQueueProposalTimeout = 958,
    PetbattleQueueOpponentDeclined = 959,
    PetbattleQueueRequeuedInternal = 960,
    PetbattleQueueRequeuedRemoved = 961,
    PetbattleQueueSlotLocked = 962,
    PetbattleQueueSlotEmpty = 963,
    PetbattleQueueSlotNoTracker = 964,
    PetbattleQueueSlotNoSpecies = 965,
    PetbattleQueueSlotCantBattle = 966,
    PetbattleQueueSlotRevoked = 967,
    PetbattleQueueSlotDead = 968,
    PetbattleQueueSlotNoPet = 969,
    PetbattleQueueNotWhileNeutral = 970,
    PetbattleGameTimeLimitWarning = 971,
    PetbattleGameRoundsLimitWarning = 972,
    HasRestriction = 973,
    ItemUpgradeItemTooLowLevel = 974,
    ItemUpgradeNoPath = 975,
    ItemUpgradeNoMoreUpgrades = 976,
    BonusRollEmpty = 977,
    ChallengeModeFull = 978,
    ChallengeModeInProgress = 979,
    ChallengeModeIncorrectKeystone = 980,
    BattletagFriendNotFound = 981,
    BattletagFriendNotValid = 982,
    BattletagFriendNotAllowed = 983,
    BattletagFriendThrottled = 984,
    BattletagFriendSuccess = 985,
    PetTooHighLevelToUncage = 986,
    PetbattleInternal = 987,
    CantCagePetYet = 988,
    NoLootInChallengeMode = 989,
    QuestPetBattleVictoriesPvpIi = 990,
    RoleCheckAlreadyInProgress = 991,
    RecruitAFriendAccountLimit = 992,
    RecruitAFriendFailed = 993,
    SetLootPersonal = 994,
    SetLootMethodFailedCombat = 995,
    ReagentBankFull = 996,
    ReagentBankLocked = 997,
    GarrisonBuildingExists = 998,
    GarrisonInvalidPlot = 999,
    GarrisonInvalidBuildingid = 1000,
    GarrisonInvalidPlotBuilding = 1001,
    GarrisonRequiresBlueprint = 1002,
    GarrisonNotEnoughCurrency = 1003,
    GarrisonNotEnoughGold = 1004,
    GarrisonCompleteMissionWrongFollowerType = 1005,
    AlreadyUsingLfgList = 1006,
    RestrictedAccountLfgListTrial = 1007,
    ToyUseLimitReached = 1008,
    ToyAlreadyKnown = 1009,
    TransmogSetAlreadyKnown = 1010,
    NotEnoughCurrency = 1011,
    SpecIsDisabled = 1012,
    FeatureRestrictedTrial = 1013,
    CantBeObliterated = 1014,
    CantBeScrapped = 1015,
    CantBeRecrafted = 1016,
    ArtifactRelicDoesNotMatchArtifact = 1017,
    MustEquipArtifact = 1018,
    CantDoThatRightNow = 1019,
    AffectingCombat = 1020,
    EquipmentManagerCombatSwapS = 1021,
    EquipmentManagerBagsFull = 1022,
    EquipmentManagerMissingItemS = 1023,
    MovieRecordingWarningPerf = 1024,
    MovieRecordingWarningDiskFull = 1025,
    MovieRecordingWarningNoMovie = 1026,
    MovieRecordingWarningRequirements = 1027,
    MovieRecordingWarningCompressing = 1028,
    NoChallengeModeReward = 1029,
    ClaimedChallengeModeReward = 1030,
    ChallengeModePeriodResetSs = 1031,
    CantDoThatChallengeModeActive = 1032,
    TalentFailedRestArea = 1033,
    CannotAbandonLastPet = 1034,
    TestCvarSetSss = 1035,
    QuestTurnInFailReason = 1036,
    ClaimedChallengeModeRewardOld = 1037,
    TalentGrantedByAura = 1038,
    ChallengeModeAlreadyComplete = 1039,
    GlyphTargetNotAvailable = 1040,
    PvpWarmodeToggleOn = 1041,
    PvpWarmodeToggleOff = 1042,
    SpellFailedLevelRequirement = 1043,
    SpellFailedCantFlyHere = 1044,
    BattlegroundJoinRequiresLevel = 1045,
    BattlegroundJoinDisqualified = 1046,
    BattlegroundJoinDisqualifiedNoName = 1047,
    VoiceChatGenericUnableToConnect = 1048,
    VoiceChatServiceLost = 1049,
    VoiceChatChannelNameTooShort = 1050,
    VoiceChatChannelNameTooLong = 1051,
    VoiceChatChannelAlreadyExists = 1052,
    VoiceChatTargetNotFound = 1053,
    VoiceChatTooManyRequests = 1054,
    VoiceChatPlayerSilenced = 1055,
    VoiceChatParentalDisableAll = 1056,
    VoiceChatDisabled = 1057,
    NoPvpReward = 1058,
    ClaimedPvpReward = 1059,
    AzeriteEssenceSelectionFailedEssenceNotUnlocked = 1060,
    AzeriteEssenceSelectionFailedCantRemoveEssence = 1061,
    AzeriteEssenceSelectionFailedConditionFailed = 1062,
    AzeriteEssenceSelectionFailedRestArea = 1063,
    AzeriteEssenceSelectionFailedSlotLocked = 1064,
    AzeriteEssenceSelectionFailedNotAtForge = 1065,
    AzeriteEssenceSelectionFailedHeartLevelTooLow = 1066,
    AzeriteEssenceSelectionFailedNotEquipped = 1067,
    SocketingRequiresPunchcardredGem = 1068,
    SocketingPunchcardredGemOnlyInPunchcardredslot = 1069,
    SocketingRequiresPunchcardyellowGem = 1070,
    SocketingPunchcardyellowGemOnlyInPunchcardyellowslot = 1071,
    SocketingRequiresPunchcardblueGem = 1072,
    SocketingPunchcardblueGemOnlyInPunchcardblueslot = 1073,
    SocketingRequiresDominationShard = 1074,
    SocketingDominationShardOnlyInDominationslot = 1075,
    SocketingRequiresCypherGem = 1076,
    SocketingCypherGemOnlyInCypherslot = 1077,
    SocketingRequiresTinkerGem = 1078,
    SocketingTinkerGemOnlyInTinkerslot = 1079,
    LevelLinkingResultLinked = 1080,
    LevelLinkingResultUnlinked = 1081,
    ClubFinderErrorPostClub = 1082,
    ClubFinderErrorApplyClub = 1083,
    ClubFinderErrorRespondApplicant = 1084,
    ClubFinderErrorCancelApplication = 1085,
    ClubFinderErrorTypeAcceptApplication = 1086,
    ClubFinderErrorTypeNoInvitePermissions = 1087,
    ClubFinderErrorTypeNoPostingPermissions = 1088,
    ClubFinderErrorTypeApplicantList = 1089,
    ClubFinderErrorTypeApplicantListNoPerm = 1090,
    ClubFinderErrorTypeFinderNotAvailable = 1091,
    ClubFinderErrorTypeGetPostingIds = 1092,
    ClubFinderErrorTypeJoinApplication = 1093,
    ClubFinderErrorTypeRealmNotEligible = 1094,
    ClubFinderErrorTypeFlaggedRename = 1095,
    ClubFinderErrorTypeFlaggedDescriptionChange = 1096,
    ItemInteractionNotEnoughGold = 1097,
    ItemInteractionNotEnoughCurrency = 1098,
    PlayerChoiceErrorPendingChoice = 1099,
    SoulbindInvalidConduit = 1100,
    SoulbindInvalidConduitItem = 1101,
    SoulbindInvalidTalent = 1102,
    SoulbindDuplicateConduit = 1103,
    ActivateSoulbindS = 1104,
    ActivateSoulbindFailedRestArea = 1105,
    CantUseProfanity = 1106,
    NotInPetBattle = 1107,
    NotInNpe = 1108,
    NoSpec = 1109,
    NoDominationshardOverwrite = 1110,
    UseWeeklyRewardsDisabled = 1111,
    CrossFactionGroupJoined = 1112,
    CantTargetUnfriendlyInOverworld = 1113,
    EquipablespellsSlotsFull = 1114
}
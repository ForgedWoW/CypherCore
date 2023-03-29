// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum LfgJoinResult
{
    // 3 = No client reaction | 18 = "Rolecheck failed"
    Ok = 0x00,                              // Joined (No Client Msg)
    GroupFull = 0x1f,                       // Your Group Is Already Full.
    NoLfgObject = 0x21,                     // Internal Lfg Error.
    NoSlots = 0x22,                         // You Do Not Meet The Requirements For The Chosen Dungeons.
    MismatchedSlots = 0x23,                 // You Cannot Mix Dungeons, Raids, And Random When Picking Dungeons.
    PartyPlayersFromDifferentRealms = 0x24, // The Dungeon You Chose Does Not Support Players From Multiple Realms.
    MembersNotPresent = 0x25,               // One Or More Group Members Are Pending Invites Or Disconnected.
    GetInfoTimeout = 0x26,                  // Could Not Retrieve Information About Some Party Members.
    InvalidSlot = 0x27,                     // One Or More Dungeons Was Not Valid.
    DeserterPlayer = 0x28,                  // You Can Not Queue For Dungeons Until Your Deserter Debuff Wears Off.
    DeserterParty = 0x29,                   // One Or More Party Members Has A Deserter Debuff.
    RandomCooldownPlayer = 0x2a,            // You Can Not Queue For Random Dungeons While On Random Dungeon Cooldown.
    RandomCooldownParty = 0x2b,             // One Or More Party Members Are On Random Dungeon Cooldown.
    TooManyMembers = 0x2c,                  // You Have Too Many Group Members To Queue For That.
    CantUseDungeons = 0x2d,                 // You Cannot Queue For A Dungeon Or Raid While Using Battlegrounds Or Arenas.
    RoleCheckFailed = 0x2e,                 // The Role Check Has Failed.
    TooFewMembers = 0x34,                   // You Do Not Have Enough Group Members To Queue For That.
    ReasonTooManyLfg = 0x35,                // You Are Queued For Too Many Instances.
    MismatchedSlotsLocalXrealm = 0x37,      // You Cannot Mix Realm-Only And X-Realm Entries When Listing Your Name In Other Raids.
    AlreadyUsingLfgList = 0x3f,             // You Can'T Do That While Using Premade Groups.
    NotLeader = 0x45,                       // You Are Not The Party Leader.
    Dead = 0x49,

    PartyNotMeetReqs = 6, // One Or More Party Members Do Not Meet The Requirements For The Chosen Dungeons (Fixme)
}
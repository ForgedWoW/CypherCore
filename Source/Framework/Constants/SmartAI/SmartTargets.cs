// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum SmartTargets
{
    None = 0,                  // None
    Self = 1,                  // Self Cast
    Victim = 2,                // Our Current Target (Ie: Highest Aggro)
    HostileSecondAggro = 3,    // Second highest aggro, maxdist, playerOnly, powerType + 1
    HostileLastAggro = 4,      // Dead last on aggro, maxdist, playerOnly, powerType + 1
    HostileRandom = 5,         // Just any random target on our threat list, maxdist, playerOnly, powerType + 1
    HostileRandomNotTop = 6,   // Any random target except top threat, maxdist, playerOnly, powerType + 1
    ActionInvoker = 7,         // Unit Who Caused This Event To Occur
    Position = 8,              // Use Xyz From Event Params
    CreatureRange = 9,         // Creatureentry(0any), Mindist, Maxdist
    CreatureGuid = 10,         // Guid, Entry
    CreatureDistance = 11,     // Creatureentry(0any), Maxdist
    Stored = 12,               // Id, Uses Pre-Stored Target(List)
    GameobjectRange = 13,      // Entry(0any), Min, Max
    GameobjectGuid = 14,       // Guid, Entry
    GameobjectDistance = 15,   // Entry(0any), Maxdist
    InvokerParty = 16,         // Invoker'S Party Members
    PlayerRange = 17,          // Min, Max
    PlayerDistance = 18,       // Maxdist
    ClosestCreature = 19,      // Creatureentry(0any), Maxdist, Dead?
    ClosestGameobject = 20,    // Entry(0any), Maxdist
    ClosestPlayer = 21,        // Maxdist
    ActionInvokerVehicle = 22, // Unit'S Vehicle Who Caused This Event To Occur
    OwnerOrSummoner = 23,      // Unit's owner or summoner, Use Owner/Charmer of this unit
    ThreatList = 24,           // All units on creature's threat list, maxdist
    ClosestEnemy = 25,         // maxDist, playerOnly
    ClosestFriendly = 26,      // maxDist, playerOnly
    LootRecipients = 27,       // all players that have tagged this creature (for kill credit)
    Farthest = 28,             // maxDist, playerOnly, isInLos
    VehiclePassenger = 29,     // seatMask (0 - all seats)
    ClosestUnspawnedGameobject = 30,

    End = 31
}
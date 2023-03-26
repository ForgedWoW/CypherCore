// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Spells;

public class SpellImplicitTargetInfo
{
    private static readonly StaticData[] _data = new StaticData[(int)Targets.TotalSpellTargets]
	{
		new(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None, SpellTargetSelectionCategories.Nyi, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),             // 0
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 1 TARGET_UNIT_CASTER
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Nearby, SpellTargetCheckTypes.Enemy, SpellTargetDirectionTypes.None),          // 2 TARGET_UNIT_NEARBY_ENEMY
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Nearby, SpellTargetCheckTypes.Ally, SpellTargetDirectionTypes.None),           // 3 TARGET_UNIT_NEARBY_ALLY
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Nearby, SpellTargetCheckTypes.Party, SpellTargetDirectionTypes.None),          // 4 TARGET_UNIT_NEARBY_PARTY
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 5 TARGET_UNIT_PET
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Enemy, SpellTargetDirectionTypes.None),         // 6 TARGET_UNIT_TARGET_ENEMY
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Src, SpellTargetSelectionCategories.Area, SpellTargetCheckTypes.Entry, SpellTargetDirectionTypes.None),               // 7 TARGET_UNIT_SRC_AREA_ENTRY
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Dest, SpellTargetSelectionCategories.Area, SpellTargetCheckTypes.Entry, SpellTargetDirectionTypes.None),              // 8 TARGET_UNIT_DEST_AREA_ENTRY
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 9 TARGET_DEST_HOME
		new(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None, SpellTargetSelectionCategories.Nyi, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),             // 10
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Src, SpellTargetSelectionCategories.Nyi, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),              // 11 TARGET_UNIT_SRC_AREA_UNK_11
		new(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None, SpellTargetSelectionCategories.Nyi, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),             // 12
		new(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None, SpellTargetSelectionCategories.Nyi, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),             // 13
		new(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None, SpellTargetSelectionCategories.Nyi, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),             // 14
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Src, SpellTargetSelectionCategories.Area, SpellTargetCheckTypes.Enemy, SpellTargetDirectionTypes.None),               // 15 TARGET_UNIT_SRC_AREA_ENEMY
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Dest, SpellTargetSelectionCategories.Area, SpellTargetCheckTypes.Enemy, SpellTargetDirectionTypes.None),              // 16 TARGET_UNIT_DEST_AREA_ENEMY
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 17 TARGET_DEST_DB
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 18 TARGET_DEST_CASTER
		new(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None, SpellTargetSelectionCategories.Nyi, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),             // 19
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Area, SpellTargetCheckTypes.Party, SpellTargetDirectionTypes.None),            // 20 TARGET_UNIT_CASTER_AREA_PARTY
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Ally, SpellTargetDirectionTypes.None),          // 21 TARGET_UNIT_TARGET_ALLY
		new(SpellTargetObjectTypes.Src, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),        // 22 TARGET_SRC_CASTER
		new(SpellTargetObjectTypes.Gobj, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 23 TARGET_GAMEOBJECT_TARGET
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Cone, SpellTargetCheckTypes.Enemy, SpellTargetDirectionTypes.Front),           // 24 TARGET_UNIT_CONE_ENEMY_24
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 25 TARGET_UNIT_TARGET_ANY
		new(SpellTargetObjectTypes.GobjItem, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),   // 26 TARGET_GAMEOBJECT_ITEM_TARGET
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 27 TARGET_UNIT_MASTER
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Enemy, SpellTargetDirectionTypes.None),           // 28 TARGET_DEST_DYNOBJ_ENEMY
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Ally, SpellTargetDirectionTypes.None),            // 29 TARGET_DEST_DYNOBJ_ALLY
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Src, SpellTargetSelectionCategories.Area, SpellTargetCheckTypes.Ally, SpellTargetDirectionTypes.None),                // 30 TARGET_UNIT_SRC_AREA_ALLY
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Dest, SpellTargetSelectionCategories.Area, SpellTargetCheckTypes.Ally, SpellTargetDirectionTypes.None),               // 31 TARGET_UNIT_DEST_AREA_ALLY
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.FrontLeft),  // 32 TARGET_DEST_CASTER_SUMMON
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Src, SpellTargetSelectionCategories.Area, SpellTargetCheckTypes.Party, SpellTargetDirectionTypes.None),               // 33 TARGET_UNIT_SRC_AREA_PARTY
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Dest, SpellTargetSelectionCategories.Area, SpellTargetCheckTypes.Party, SpellTargetDirectionTypes.None),              // 34 TARGET_UNIT_DEST_AREA_PARTY
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Party, SpellTargetDirectionTypes.None),         // 35 TARGET_UNIT_TARGET_PARTY
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Nyi, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),           // 36 TARGET_DEST_CASTER_UNK_36
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Last, SpellTargetSelectionCategories.Area, SpellTargetCheckTypes.Party, SpellTargetDirectionTypes.None),              // 37 TARGET_UNIT_LASTTARGET_AREA_PARTY
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Nearby, SpellTargetCheckTypes.Entry, SpellTargetDirectionTypes.None),          // 38 TARGET_UNIT_NEARBY_ENTRY
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 39 TARGET_DEST_CASTER_FISHING
		new(SpellTargetObjectTypes.Gobj, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Nearby, SpellTargetCheckTypes.Entry, SpellTargetDirectionTypes.None),          // 40 TARGET_GAMEOBJECT_NEARBY_ENTRY
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.FrontRight), // 41 TARGET_DEST_CASTER_FRONT_RIGHT
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.BackRight),  // 42 TARGET_DEST_CASTER_BACK_RIGHT
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.BackLeft),   // 43 TARGET_DEST_CASTER_BACK_LEFT
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.FrontLeft),  // 44 TARGET_DEST_CASTER_FRONT_LEFT
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Ally, SpellTargetDirectionTypes.None),          // 45 TARGET_UNIT_TARGET_CHAINHEAL_ALLY
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Nearby, SpellTargetCheckTypes.Entry, SpellTargetDirectionTypes.None),          // 46 TARGET_DEST_NEARBY_ENTRY
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.Front),      // 47 TARGET_DEST_CASTER_FRONT
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.Back),       // 48 TARGET_DEST_CASTER_BACK
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.Right),      // 49 TARGET_DEST_CASTER_RIGHT
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.Left),       // 50 TARGET_DEST_CASTER_LEFT
		new(SpellTargetObjectTypes.Gobj, SpellTargetReferenceTypes.Src, SpellTargetSelectionCategories.Area, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),             // 51 TARGET_GAMEOBJECT_SRC_AREA
		new(SpellTargetObjectTypes.Gobj, SpellTargetReferenceTypes.Dest, SpellTargetSelectionCategories.Area, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),            // 52 TARGET_GAMEOBJECT_DEST_AREA
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Enemy, SpellTargetDirectionTypes.None),         // 53 TARGET_DEST_TARGET_ENEMY
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Cone, SpellTargetCheckTypes.Enemy, SpellTargetDirectionTypes.Front),           // 54 TARGET_UNIT_CONE_180_DEG_ENEMY
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 55 TARGET_DEST_CASTER_FRONT_LEAP
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Area, SpellTargetCheckTypes.Raid, SpellTargetDirectionTypes.None),             // 56 TARGET_UNIT_CASTER_AREA_RAID
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Raid, SpellTargetDirectionTypes.None),          // 57 TARGET_UNIT_TARGET_RAID
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Nearby, SpellTargetCheckTypes.Raid, SpellTargetDirectionTypes.None),           // 58 TARGET_UNIT_NEARBY_RAID
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Cone, SpellTargetCheckTypes.Ally, SpellTargetDirectionTypes.Front),            // 59 TARGET_UNIT_CONE_ALLY
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Cone, SpellTargetCheckTypes.Entry, SpellTargetDirectionTypes.Front),           // 60 TARGET_UNIT_CONE_ENTRY
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Area, SpellTargetCheckTypes.RaidClass, SpellTargetDirectionTypes.None),        // 61 TARGET_UNIT_TARGET_AREA_RAID_CLASS
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 62 TARGET_DEST_CASTER_GROUND
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 63 TARGET_DEST_TARGET_ANY
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.Front),      // 64 TARGET_DEST_TARGET_FRONT
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.Back),       // 65 TARGET_DEST_TARGET_BACK
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.Right),      // 66 TARGET_DEST_TARGET_RIGHT
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.Left),       // 67 TARGET_DEST_TARGET_LEFT
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.FrontRight), // 68 TARGET_DEST_TARGET_FRONT_RIGHT
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.BackRight),  // 69 TARGET_DEST_TARGET_BACK_RIGHT
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.BackLeft),   // 70 TARGET_DEST_TARGET_BACK_LEFT
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.FrontLeft),  // 71 TARGET_DEST_TARGET_FRONT_LEFT
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.Random),     // 72 TARGET_DEST_CASTER_RANDOM
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.Random),     // 73 TARGET_DEST_CASTER_RADIUS
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.Random),     // 74 TARGET_DEST_TARGET_RANDOM
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.Random),     // 75 TARGET_DEST_TARGET_RADIUS
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Channel, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 76 TARGET_DEST_CHANNEL_TARGET
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Channel, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 77 TARGET_UNIT_CHANNEL_TARGET
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.Front),        // 78 TARGET_DEST_DEST_FRONT
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.Back),         // 79 TARGET_DEST_DEST_BACK
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.Right),        // 80 TARGET_DEST_DEST_RIGHT
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.Left),         // 81 TARGET_DEST_DEST_LEFT
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.FrontRight),   // 82 TARGET_DEST_DEST_FRONT_RIGHT
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.BackRight),    // 83 TARGET_DEST_DEST_BACK_RIGHT
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.BackLeft),     // 84 TARGET_DEST_DEST_BACK_LEFT
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.FrontLeft),    // 85 TARGET_DEST_DEST_FRONT_LEFT
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.Random),       // 86 TARGET_DEST_DEST_RANDOM
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),         // 87 TARGET_DEST_DEST
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),         // 88 TARGET_DEST_DYNOBJ_NONE
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest, SpellTargetSelectionCategories.Traj, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),            // 89 TARGET_DEST_TRAJ
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 90 TARGET_UNIT_TARGET_MINIPET
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.Random),       // 91 TARGET_DEST_DEST_RADIUS
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 92 TARGET_UNIT_SUMMONER
		new(SpellTargetObjectTypes.Corpse, SpellTargetReferenceTypes.Src, SpellTargetSelectionCategories.Area, SpellTargetCheckTypes.Enemy, SpellTargetDirectionTypes.None),             // 93 TARGET_CORPSE_SRC_AREA_ENEMY
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 94 TARGET_UNIT_VEHICLE
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Passenger, SpellTargetDirectionTypes.None),     // 95 TARGET_UNIT_TARGET_PASSENGER
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 96 TARGET_UNIT_PASSENGER_0
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 97 TARGET_UNIT_PASSENGER_1
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 98 TARGET_UNIT_PASSENGER_2
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 99 TARGET_UNIT_PASSENGER_3
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 100 TARGET_UNIT_PASSENGER_4
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 101 TARGET_UNIT_PASSENGER_5
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 102 TARGET_UNIT_PASSENGER_6
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 103 TARGET_UNIT_PASSENGER_7
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Dest, SpellTargetSelectionCategories.Cone, SpellTargetCheckTypes.Enemy, SpellTargetDirectionTypes.Front),             // 104 TARGET_UNIT_CONE_CASTER_TO_DEST_ENEMY
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Area, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),          // 105 TARGET_UNIT_CASTER_AND_PASSENGERS
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Channel, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 106 TARGET_DEST_CHANNEL_CASTER
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Nearby, SpellTargetCheckTypes.Entry, SpellTargetDirectionTypes.None),          // 107 TARGET_DEST_NEARBY_ENTRY_2
		new(SpellTargetObjectTypes.Gobj, SpellTargetReferenceTypes.Dest, SpellTargetSelectionCategories.Cone, SpellTargetCheckTypes.Enemy, SpellTargetDirectionTypes.Front),             // 108 TARGET_GAMEOBJECT_CONE_CASTER_TO_DEST_ENEMY
		new(SpellTargetObjectTypes.Gobj, SpellTargetReferenceTypes.Dest, SpellTargetSelectionCategories.Cone, SpellTargetCheckTypes.Ally, SpellTargetDirectionTypes.Front),              // 109 TARGET_GAMEOBJECT_CONE_CASTER_TO_DEST_ALLY
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Dest, SpellTargetSelectionCategories.Cone, SpellTargetCheckTypes.Entry, SpellTargetDirectionTypes.Front),             // 110 TARGET_UNIT_CONE_CASTER_TO_DEST_ENTRY
		new(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None, SpellTargetSelectionCategories.Nyi, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),             // 111
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 112
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 113
		new(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None, SpellTargetSelectionCategories.Nyi, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),             // 114
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Src, SpellTargetSelectionCategories.Area, SpellTargetCheckTypes.Enemy, SpellTargetDirectionTypes.None),               // 115 TARGET_UNIT_SRC_AREA_FURTHEST_ENEMY
		new(SpellTargetObjectTypes.UnitAndDest, SpellTargetReferenceTypes.Last, SpellTargetSelectionCategories.Area, SpellTargetCheckTypes.Enemy, SpellTargetDirectionTypes.None),       // 116 TARGET_UNIT_AND_DEST_LAST_ENEMY
		new(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None, SpellTargetSelectionCategories.Nyi, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),             // 117
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Area, SpellTargetCheckTypes.Raid, SpellTargetDirectionTypes.None),             // 118 TARGET_UNIT_TARGET_ALLY_OR_RAID
		new(SpellTargetObjectTypes.Corpse, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Area, SpellTargetCheckTypes.Raid, SpellTargetDirectionTypes.None),           // 119 TARGET_CORPSE_SRC_AREA_RAID
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Area, SpellTargetCheckTypes.Summoned, SpellTargetDirectionTypes.None),         // 120 TARGET_UNIT_SELF_AND_SUMMONS
		new(SpellTargetObjectTypes.Corpse, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Ally, SpellTargetDirectionTypes.None),        // 121 TARGET_CORPSE_TARGET_ALLY
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Area, SpellTargetCheckTypes.Threat, SpellTargetDirectionTypes.None),           // 122 TARGET_UNIT_AREA_THREAT_LIST
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Area, SpellTargetCheckTypes.Tap, SpellTargetDirectionTypes.None),              // 123 TARGET_UNIT_AREA_TAP_LIST
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 124 TARGET_UNIT_TARGET_TAP_LIST
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 125 TARGET_DEST_CASTER_GROUND_2
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.None, SpellTargetSelectionCategories.Nyi, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),             // 126 TARGET_UNIT_CASTER_AREA_ENEMY_CLUMP
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.None, SpellTargetSelectionCategories.Nyi, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),             // 127 TARGET_DEST_CASTER_ENEMY_CLUMP_CENTROID
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Cone, SpellTargetCheckTypes.Ally, SpellTargetDirectionTypes.Front),            // 128 TARGET_UNIT_RECT_CASTER_ALLY
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Cone, SpellTargetCheckTypes.Entry, SpellTargetDirectionTypes.Front),           // 129 TARGET_UNIT_RECT_CASTER_ENEMY
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Cone, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.Front),         // 130 TARGET_UNIT_RECT_CASTER
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 131 TARGET_DEST_SUMMONER
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Ally, SpellTargetDirectionTypes.None),          // 132 TARGET_DEST_TARGET_ALLY
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Dest, SpellTargetSelectionCategories.Line, SpellTargetCheckTypes.Ally, SpellTargetDirectionTypes.None),               // 133 TARGET_UNIT_LINE_CASTER_TO_DEST_ALLY
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Dest, SpellTargetSelectionCategories.Line, SpellTargetCheckTypes.Enemy, SpellTargetDirectionTypes.None),              // 134 TARGET_UNIT_LINE_CASTER_TO_DEST_ENEMY
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Dest, SpellTargetSelectionCategories.Line, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),            // 135 TARGET_UNIT_LINE_CASTER_TO_DEST
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Dest, SpellTargetSelectionCategories.Cone, SpellTargetCheckTypes.Ally, SpellTargetDirectionTypes.Front),              // 136 TARGET_UNIT_CONE_CASTER_TO_DEST_ALLY
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 137 TARGET_DEST_CASTER_MOVEMENT_DIRECTION
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),         // 138 TARGET_DEST_DEST_GROUND
		new(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None, SpellTargetSelectionCategories.Nyi, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),             // 139
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.None, SpellTargetSelectionCategories.Nyi, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),             // 140 TARGET_DEST_CASTER_CLUMP_CENTROID
		new(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None, SpellTargetSelectionCategories.Nyi, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),             // 141
		new(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None, SpellTargetSelectionCategories.Nyi, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),             // 142
		new(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None, SpellTargetSelectionCategories.Nyi, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),             // 143
		new(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None, SpellTargetSelectionCategories.Nyi, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),             // 144
		new(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None, SpellTargetSelectionCategories.Nyi, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),             // 145
		new(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None, SpellTargetSelectionCategories.Nyi, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),             // 146
		new(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None, SpellTargetSelectionCategories.Nyi, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),             // 147
		new(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None, SpellTargetSelectionCategories.Nyi, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),             // 148
		new(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.Random),     // 149
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),       // 150 TARGET_UNIT_OWN_CRITTER
		new(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Area, SpellTargetCheckTypes.Enemy, SpellTargetDirectionTypes.None),            // 151
		new(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None, SpellTargetSelectionCategories.Nyi, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),             // 152
	};

    private readonly Targets _target;

	public bool IsArea => SelectionCategory == SpellTargetSelectionCategories.Area || SelectionCategory == SpellTargetSelectionCategories.Cone;

	public SpellTargetSelectionCategories SelectionCategory => _data[(int)_target].SelectionCategory;

	public SpellTargetReferenceTypes ReferenceType => _data[(int)_target].ReferenceType;

	public SpellTargetObjectTypes ObjectType => _data[(int)_target].ObjectType;

	public SpellTargetCheckTypes CheckType => _data[(int)_target].SelectionCheckType;

	private SpellTargetDirectionTypes DirectionType => _data[(int)_target].DirectionType;

	public Targets Target => _target;

	public SpellImplicitTargetInfo(Targets target = 0)
	{
		_target = target;
	}

	public float CalcDirectionAngle()
	{
		var pi = MathFunctions.PI;

		switch (DirectionType)
		{
			case SpellTargetDirectionTypes.Front:
				return 0.0f;
			case SpellTargetDirectionTypes.Back:
				return pi;
			case SpellTargetDirectionTypes.Right:
				return -pi / 2;
			case SpellTargetDirectionTypes.Left:
				return pi / 2;
			case SpellTargetDirectionTypes.FrontRight:
				return -pi / 4;
			case SpellTargetDirectionTypes.BackRight:
				return -3 * pi / 4;
			case SpellTargetDirectionTypes.BackLeft:
				return 3 * pi / 4;
			case SpellTargetDirectionTypes.FrontLeft:
				return pi / 4;
			case SpellTargetDirectionTypes.Random:
				return (float)RandomHelper.NextDouble() * (2 * pi);
			default:
				return 0.0f;
		}
	}

	public SpellCastTargetFlags GetExplicitTargetMask(ref bool srcSet, ref bool dstSet)
	{
		SpellCastTargetFlags targetMask = 0;

		if (Target == Targets.DestTraj)
		{
			if (!srcSet)
				targetMask = SpellCastTargetFlags.SourceLocation;

			if (!dstSet)
				targetMask |= SpellCastTargetFlags.DestLocation;
		}
		else
		{
			switch (ReferenceType)
			{
				case SpellTargetReferenceTypes.Src:
					if (srcSet)
						break;

					targetMask = SpellCastTargetFlags.SourceLocation;

					break;
				case SpellTargetReferenceTypes.Dest:
					if (dstSet)
						break;

					targetMask = SpellCastTargetFlags.DestLocation;

					break;
				case SpellTargetReferenceTypes.Target:
					switch (ObjectType)
					{
						case SpellTargetObjectTypes.Gobj:
							targetMask = SpellCastTargetFlags.Gameobject;

							break;
						case SpellTargetObjectTypes.GobjItem:
							targetMask = SpellCastTargetFlags.GameobjectItem;

							break;
						case SpellTargetObjectTypes.UnitAndDest:
						case SpellTargetObjectTypes.Unit:
						case SpellTargetObjectTypes.Dest:
							switch (CheckType)
							{
								case SpellTargetCheckTypes.Enemy:
									targetMask = SpellCastTargetFlags.UnitEnemy;

									break;
								case SpellTargetCheckTypes.Ally:
									targetMask = SpellCastTargetFlags.UnitAlly;

									break;
								case SpellTargetCheckTypes.Party:
									targetMask = SpellCastTargetFlags.UnitParty;

									break;
								case SpellTargetCheckTypes.Raid:
									targetMask = SpellCastTargetFlags.UnitRaid;

									break;
								case SpellTargetCheckTypes.Passenger:
									targetMask = SpellCastTargetFlags.UnitPassenger;

									break;
								case SpellTargetCheckTypes.RaidClass:
								default:
									targetMask = SpellCastTargetFlags.Unit;

									break;
							}

							break;
					}

					break;
			}
		}

		switch (ObjectType)
		{
			case SpellTargetObjectTypes.Src:
				srcSet = true;

				break;
			case SpellTargetObjectTypes.Dest:
			case SpellTargetObjectTypes.UnitAndDest:
				dstSet = true;

				break;
		}

		return targetMask;
	}

	public struct StaticData
	{
		public StaticData(SpellTargetObjectTypes obj, SpellTargetReferenceTypes reference,
						SpellTargetSelectionCategories selection, SpellTargetCheckTypes selectionCheck, SpellTargetDirectionTypes direction)
		{
			ObjectType = obj;
			ReferenceType = reference;
			SelectionCategory = selection;
			SelectionCheckType = selectionCheck;
			DirectionType = direction;
		}

		public SpellTargetObjectTypes ObjectType;       // type of object returned by target type
		public SpellTargetReferenceTypes ReferenceType; // defines which object is used as a reference when selecting target
		public SpellTargetSelectionCategories SelectionCategory;
		public SpellTargetCheckTypes SelectionCheckType; // defines selection criteria
		public SpellTargetDirectionTypes DirectionType;  // direction for cone and dest targets
	}
}
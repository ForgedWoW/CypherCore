// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum ScriptCommands
{
	Talk = 0,  // Source/Target = Creature, Target = Any, Datalong = Talk Type (see ChatType enum), datalong2 & 1 = player talk (instead of creature), dataint = string_id
	Emote = 1, // Source/Target = Creature, Datalong = Emote Id, Datalong2 = 0: Set Emote State; > 0: Play Emote State
	FieldSetDeprecated = 2,
	MoveTo = 3, // Source/Target = Creature, Datalong2 = Time To Reach, X/Y/Z = Destination
	FlagSetDeprecated = 4,
	FlagRemoveDeprecated = 5,
	TeleportTo = 6,          // Source/Target = Creature/Player (See Datalong2), Datalong = MapId, Datalong2 = 0: Player; 1: Creature, X/Y/Z = Destination, O = Orientation
	QuestExplored = 7,       // Target/Source = Player, Target/Source = Go/Creature, Datalong = Quest Id, Datalong2 = Distance Or 0
	KillCredit = 8,          // Target/Source = Player, Datalong = Creature Entry, Datalong2 = 0: Personal Credit, 1: Group Credit
	RespawnGameobject = 9,   // Source = Worldobject (Summoner), Datalong = Go Guid, Datalong2 = Despawn Delay
	TempSummonCreature = 10, // Source = Worldobject (Summoner), Datalong = Creature Entry, Datalong2 = Despawn Delay, X/Y/Z = Summon Position, O = Orientation
	OpenDoor = 11,           // Source = Unit, Datalong = Go Guid, Datalong2 = Reset Delay (Min 15)
	CloseDoor = 12,          // Source = Unit, Datalong = Go Guid, Datalong2 = Reset Delay (Min 15)
	ActivateObject = 13,     // Source = Unit, Target = Go
	RemoveAura = 14,         // Source (Datalong2 != 0) Or Target (Datalong2 == 0) = Unit, Datalong = Spell Id
	CastSpell = 15,          // Source And/Or Target = Unit, Datalong2 = Cast Direction (0: S.T 1: S.S 2: T.T 3: T.S 4: S.Creature With Dataint Entry), Dataint & 1 = Triggered Flag
	PlaySound = 16,          // Source = Worldobject, Target = None/Player, Datalong = Sound Id, Datalong2 (Bitmask: 0/1=Anyone/Player, 0/2=Without/With Distance Dependency, So 1|2 = 3 Is Target With Distance Dependency)
	CreateItem = 17,         // Target/Source = Player, Datalong = Item Entry, Datalong2 = Amount
	DespawnSelf = 18,        // Target/Source = Creature, Datalong = Despawn Delay

	LoadPath = 20,         // Source = Unit, Datalong = Path Id, Datalong2 = Is Repeatable
	CallscriptToUnit = 21, // Source = Worldobject (If Present Used As A Search Center), Datalong = Script Id, Datalong2 = Unit Lowguid, Dataint = Script Table To Use (See Scriptstype)
	Kill = 22,             // Source/Target = Creature, Dataint = Remove Corpse Attribute

	// Cyphercore Only
	Orientation = 30, // Source = Unit, Target (Datalong > 0) = Unit, Datalong = > 0 Turn Source To Face Target, O = Orientation
	Equip = 31,       // Soucre = Creature, Datalong = Equipment Id
	Model = 32,       // Source = Creature, Datalong = Model Id
	CloseGossip = 33, // Source = Player
	Playmovie = 34,   // Source = Player, Datalong = Movie Id
	Movement = 35,    // Source = Creature, datalong = MovementType, datalong2 = MovementDistance (wander_distance f.ex.), dataint = pathid
	PlayAnimkit = 36  // Source = Creature, datalong = AnimKit id
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.BaseScripts;
using Forged.MapServer.Scripting.Interfaces.IMap;
using Framework.Constants;

namespace Scripts.EasternKingdoms.Deadmines;

[Script]
internal class InstanceDeadmines : InstanceMapScript, IInstanceMapGetInstanceScript
{
    public InstanceDeadmines() : base(nameof(InstanceDeadmines), 36) { }

    public InstanceScript GetInstanceScript(InstanceMap map)
    {
        return new InstanceDeadminesInstanceMapScript(map);
    }

    private class InstanceDeadminesInstanceMapScript : InstanceScript
    {
        public const string NOTE_TEXT = "A note falls to the floor!";
        public static readonly Position NoteSpawn = new(-74.36111f, -820.0139f, 40.67145f, 4.014257f);

        /// https://wowpedia.fandom.com/wiki/DungeonEncounterID
        private static readonly DungeonEncounterData[] Encounters =
        {
            new(DmData.DATA_HELIX, 1065),
            // new(DMData.DATA_NIGHTMARE_HELIX, 1065),
            new(DmData.DATA_GLUBTOK, 1064), new(DmData.DATA_COOKIE, 1060), new(DmData.DATA_FOEREAPER, 1063), new(DmData.DATA_RIPSNARL, 1062), new(DmData.DATA_VANESSA, 1081),
            // new(DMData.DATA_VANESSA_NIGHTMARE, 1081)
        };

        private static readonly DoorData[] DoorData =
        {
            new(DmGameObjects.GO_FACTORY_DOOR, DmData.DATA_GLUBTOK, DoorType.Passage), new(DmGameObjects.GO_HEAVY_DOOR_HELIX, DmData.DATA_HELIX, DoorType.Passage), new(DmGameObjects.GO_FOUNDRY_DOOR, DmData.DATA_FOEREAPER, DoorType.Passage), new(DmGameObjects.GO_IRONCLAD_DOOR, DmData.DATA_FOEREAPER, DoorType.Passage),
        };

        private static readonly ObjectData[] CreatureData =
        {
            new(DmCreatures.NPC_HELIX_GEARBREAKER, DmData.DATA_HELIX), new(DmCreatures.NPC_HELIX_NIGHTMARE, DmData.DATA_NIGHTMARE_HELIX), new(DmCreatures.NPC_GLUBTOK, DmData.DATA_GLUBTOK), new(DmCreatures.NPC_CAPTAIN_COOKIE, DmData.DATA_COOKIE), new(DmCreatures.NPC_FOE_REAPER_5000, DmData.DATA_FOEREAPER), new(DmCreatures.NPC_ADMIRAL_RIPSNARL, DmData.DATA_RIPSNARL), new(DmCreatures.NPC_VANESSA_NIGHTMARE, DmData.DATA_VANESSA_NIGHTMARE), new(DmCreatures.NPC_VANESSA_BOSS, DmData.DATA_VANESSA), new(DmCreatures.NPC_GLUBTOK_NIGHTMARE, DmData.DATA_NIGHTMARE_MECHANICAL)
        };

        private static readonly ObjectData[] GameObjectData =
            { };

        private ObjectGuid _vanessa;
        private ObjectGuid _vanessaNote;
        private ObjectGuid _vanessaBoss;
        private ObjectGuid _glubtokGUID;

        private TeamFaction _teamInInstance;


        public InstanceDeadminesInstanceMapScript(InstanceMap map) : base(map)
        {
            SetBossNumber((uint)DmData.MAX_BOSSES);
            SetHeaders("DM");
            LoadDoorData(DoorData);
            LoadObjectData(CreatureData, GameObjectData);
            LoadDungeonEncounterData(Encounters);
        }

        public override void OnCreatureCreate(Creature creature)
        {
            var players = Instance.Players;

            if (!players.Empty())
                _teamInInstance = players.First().Team;

            switch (creature.Entry)
            {
                case 46889: // Kagtha
                    if (_teamInInstance == TeamFaction.Alliance)
                        creature.UpdateEntry(42308); // Lieutenant Horatio Laine

                    break;
                case 46902: // Miss Mayhem
                    if (_teamInInstance == TeamFaction.Alliance)
                        creature.UpdateEntry(491); // Quartermaster Lewis <Quartermaster>

                    break;
                case 46903: // Mayhem Reaper Prototype
                    if (_teamInInstance == TeamFaction.Alliance)
                        creature.UpdateEntry(1); // GM WAYPOINT

                    break;
                case 46906: // Slinky Sharpshiv
                    if (_teamInInstance == TeamFaction.Alliance)
                        creature.UpdateEntry(46612); // Lieutenant Horatio Laine

                    break;
                case 46613: // Crime Scene Alarm-O-Bot
                    if (_teamInInstance == TeamFaction.Horde)
                        creature.UpdateEntry(1); // GM WAYPOINT

                    break;
                case 50595: // Stormwind Defender
                    if (_teamInInstance == TeamFaction.Horde)
                        creature.UpdateEntry(46890); // Shattered Hand Assassin

                    break;
                case 46614: // Stormwind Investigator
                    if (_teamInInstance == TeamFaction.Horde)
                        creature.UpdateEntry(1); // GM WAYPOINT

                    break;
                case DmCreatures.NPC_VANESSA_VANCLEEF:
                    _vanessa = creature.GUID;

                    break;
                case DmCreatures.NPC_VANESSA_BOSS:
                    _vanessaBoss = creature.GUID;

                    break;
                case DmCreatures.NPC_VANESSA_NOTE:
                    _vanessaNote = creature.GUID;

                    break;
                case DmCreatures.NPC_GLUBTOK:
                    _glubtokGUID = creature.GUID;

                    break;
            }
        }

        public override bool SetBossState(uint id, EncounterState state)
        {
            if (!base.SetBossState(id, state))
                return false;

            switch (id)
            {
                case DmData.DATA_COOKIE:
                    if (state == EncounterState.Done)
                        if (Instance.IsHeroic)
                            SummonNote();

                    break;
                case DmData.DATA_VANESSA_NIGHTMARE:
                    if (state == EncounterState.Fail)
                        SummonNote();

                    break;
            }

            return true;
        }

        public override ulong GetData64(uint data)
        {
            switch (data)
            {
                case DmCreatures.NPC_VANESSA_VANCLEEF:
                    return _vanessa.Counter;

                case DmCreatures.NPC_VANESSA_BOSS:
                    return _vanessaBoss.Counter;

                case DmCreatures.NPC_VANESSA_NOTE:
                    return _vanessaNote.Counter;

                case DmCreatures.NPC_GLUBTOK:
                    return _glubtokGUID.Counter;
            }

            return 0;
        }

        private void SummonNote()
        {
            Creature note = Instance.SummonCreature(DmCreatures.NPC_VANESSA_NOTE, NoteSpawn);

            if (note != null)
                note.TextEmote(NOTE_TEXT, null, true);
        }
    }
}
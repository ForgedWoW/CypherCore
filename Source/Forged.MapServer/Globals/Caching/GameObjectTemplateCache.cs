// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.G;
using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class GameObjectTemplateCache : IObjectCache
{
    private readonly CliDB _cliDB;
    private readonly DB6Storage<GameObjectsRecord> _gameObjectsRecords;
    private readonly PageTextCache _pageTextCache;
    private readonly ScriptManager _scriptManager;
    private readonly DB6Storage<SpellFocusObjectRecord> _spellFocusObjectRecords;
    private readonly SpellManager _spellManager;
    private readonly List<ushort> _transportMaps = new();
    private readonly WorldDatabase _worldDatabase;
    private readonly WorldManager _worldManager;

    public GameObjectTemplateCache(WorldManager worldManager, WorldDatabase worldDatabase, ScriptManager scriptManager, DB6Storage<GameObjectsRecord> gameObjectsRecords,
                                   DB6Storage<SpellFocusObjectRecord> spellFocusObjectRecords, CliDB cliDB, SpellManager spellManager, PageTextCache pageTextCache)
    {
        _worldManager = worldManager;
        _worldDatabase = worldDatabase;
        _scriptManager = scriptManager;
        _gameObjectsRecords = gameObjectsRecords;
        _spellFocusObjectRecords = spellFocusObjectRecords;
        _cliDB = cliDB;
        _spellManager = spellManager;
        _pageTextCache = pageTextCache;
    }

    public Dictionary<uint, GameObjectTemplate> GameObjectTemplates { get; } = new();

    public GameObjectTemplate GetGameObjectTemplate(uint entry)
    {
        return GameObjectTemplates.LookupByKey(entry);
    }

    public bool IsTransportMap(uint mapId)
    {
        return _transportMaps.Contains((ushort)mapId);
    }

    public void Load()
    {
        var time = Time.MSTime;

        foreach (var db2GO in _gameObjectsRecords.Values)
        {
            GameObjectTemplate go = new()
            {
                entry = db2GO.Id,
                type = db2GO.TypeID,
                displayId = db2GO.DisplayID,
                name = db2GO.Name[_worldManager.DefaultDbcLocale],
                size = db2GO.Scale
            };

            unsafe
            {
                for (byte x = 0; x < db2GO.PropValue.Length; ++x)
                    go.Raw.data[x] = db2GO.PropValue[x];
            }

            go.ContentTuningId = 0;
            go.ScriptId = 0;

            GameObjectTemplates[db2GO.Id] = go;
        }

        //                                          0      1     2          3     4         5               6     7
        var result = _worldDatabase.Query("SELECT entry, type, displayId, name, IconName, castBarCaption, unk1, size, " +
                                          //8      9      10     11     12     13     14     15     16     17     18      19      20
                                          "Data0, Data1, Data2, Data3, Data4, Data5, Data6, Data7, Data8, Data9, Data10, Data11, Data12, " +
                                          //21      22      23      24      25      26      27      28      29      30      31      32      33      34      35      36
                                          "Data13, Data14, Data15, Data16, Data17, Data18, Data19, Data20, Data21, Data22, Data23, Data24, Data25, Data26, Data27, Data28, " +
                                          //37      38       39     40      41      42      43               44      45
                                          "Data29, Data30, Data31, Data32, Data33, Data34, ContentTuningId, AIName, ScriptName FROM gameobject_template");

        if (result.IsEmpty())
            Log.Logger.Information("Loaded 0 gameobject definitions. DB table `gameobject_template` is empty.");
        else
        {
            do
            {
                var entry = result.Read<uint>(0);

                GameObjectTemplate got = new()
                {
                    entry = entry,
                    type = (GameObjectTypes)result.Read<uint>(1),
                    displayId = result.Read<uint>(2),
                    name = result.Read<string>(3),
                    IconName = result.Read<string>(4),
                    castBarCaption = result.Read<string>(5),
                    unk1 = result.Read<string>(6),
                    size = result.Read<float>(7)
                };

                unsafe
                {
                    for (byte x = 0; x < SharedConst.MaxGOData; ++x)
                        got.Raw.data[x] = result.Read<int>(8 + x);
                }

                got.ContentTuningId = result.Read<uint>(43);
                got.AIName = result.Read<string>(44);
                got.ScriptId = _scriptManager.GetScriptId(result.Read<string>(45));

                switch (got.type)
                {
                    case GameObjectTypes.Door: //0
                        if (got.Door.open != 0)
                            CheckGOLockId(got, got.Door.open, 1);

                        CheckGONoDamageImmuneId(got, got.Door.noDamageImmune, 3);

                        break;

                    case GameObjectTypes.Button: //1
                        if (got.Button.open != 0)
                            CheckGOLockId(got, got.Button.open, 1);

                        CheckGONoDamageImmuneId(got, got.Button.noDamageImmune, 4);

                        break;

                    case GameObjectTypes.QuestGiver: //2
                        if (got.QuestGiver.open != 0)
                            CheckGOLockId(got, got.QuestGiver.open, 0);

                        CheckGONoDamageImmuneId(got, got.QuestGiver.noDamageImmune, 5);

                        break;

                    case GameObjectTypes.Chest: //3
                        if (got.Chest.open != 0)
                            CheckGOLockId(got, got.Chest.open, 0);

                        CheckGOConsumable(got, got.Chest.consumable, 3);

                        if (got.Chest.linkedTrap != 0) // linked trap
                            CheckGOLinkedTrapId(got, got.Chest.linkedTrap, 7);

                        break;

                    case GameObjectTypes.Trap: //6
                        if (got.Trap.open != 0)
                            CheckGOLockId(got, got.Trap.open, 0);

                        break;

                    case GameObjectTypes.Chair: //7
                        CheckAndFixGOChairHeightId(got, ref got.Chair.chairheight, 1);

                        break;

                    case GameObjectTypes.SpellFocus: //8
                        if (got.SpellFocus.spellFocusType != 0)
                            if (!_spellFocusObjectRecords.ContainsKey(got.SpellFocus.spellFocusType))
                                Log.Logger.Error("GameObject (Entry: {0} GoType: {1}) have data0={2} but SpellFocus (Id: {3}) not exist.",
                                                 entry,
                                                 got.type,
                                                 got.SpellFocus.spellFocusType,
                                                 got.SpellFocus.spellFocusType);

                        if (got.SpellFocus.linkedTrap != 0) // linked trap
                            CheckGOLinkedTrapId(got, got.SpellFocus.linkedTrap, 2);

                        break;

                    case GameObjectTypes.Goober: //10
                        if (got.Goober.open != 0)
                            CheckGOLockId(got, got.Goober.open, 0);

                        CheckGOConsumable(got, got.Goober.consumable, 3);

                        if (got.Goober.pageID != 0) // pageId
                            if (_pageTextCache.GetPageText(got.Goober.pageID) == null)
                                Log.Logger.Error("GameObject (Entry: {0} GoType: {1}) have data7={2} but PageText (Entry {3}) not exist.", entry, got.type, got.Goober.pageID, got.Goober.pageID);

                        CheckGONoDamageImmuneId(got, got.Goober.noDamageImmune, 11);

                        if (got.Goober.linkedTrap != 0) // linked trap
                            CheckGOLinkedTrapId(got, got.Goober.linkedTrap, 12);

                        break;

                    case GameObjectTypes.AreaDamage: //12
                        if (got.AreaDamage.open != 0)
                            CheckGOLockId(got, got.AreaDamage.open, 0);

                        break;

                    case GameObjectTypes.Camera: //13
                        if (got.Camera.open != 0)
                            CheckGOLockId(got, got.Camera.open, 0);

                        break;

                    case GameObjectTypes.MapObjTransport: //15
                    {
                        if (got.MoTransport.taxiPathID != 0)
                            if (got.MoTransport.taxiPathID >= _cliDB.TaxiPathNodesByPath.Count || _cliDB.TaxiPathNodesByPath[got.MoTransport.taxiPathID].Empty())
                                Log.Logger.Error("GameObject (Entry: {0} GoType: {1}) have data0={2} but TaxiPath (Id: {3}) not exist.",
                                                 entry,
                                                 got.type,
                                                 got.MoTransport.taxiPathID,
                                                 got.MoTransport.taxiPathID);

                        var transportMap = got.MoTransport.SpawnMap;

                        if (transportMap != 0)
                            _transportMaps.Add((ushort)transportMap);

                        break;
                    }
                    case GameObjectTypes.SpellCaster: //22
                        // always must have spell
                        CheckGOSpellId(got, got.SpellCaster.spell, 0);

                        break;

                    case GameObjectTypes.FlagStand: //24
                        if (got.FlagStand.open != 0)
                            CheckGOLockId(got, got.FlagStand.open, 0);

                        CheckGONoDamageImmuneId(got, got.FlagStand.noDamageImmune, 5);

                        break;

                    case GameObjectTypes.FishingHole: //25
                        if (got.FishingHole.open != 0)
                            CheckGOLockId(got, got.FishingHole.open, 4);

                        break;

                    case GameObjectTypes.FlagDrop: //26
                        if (got.FlagDrop.open != 0)
                            CheckGOLockId(got, got.FlagDrop.open, 0);

                        CheckGONoDamageImmuneId(got, got.FlagDrop.noDamageImmune, 3);

                        break;

                    case GameObjectTypes.BarberChair: //32
                        CheckAndFixGOChairHeightId(got, ref got.BarberChair.chairheight, 0);

                        if (got.BarberChair.SitAnimKit != 0 && !_cliDB.AnimKitStorage.ContainsKey(got.BarberChair.SitAnimKit))
                        {
                            Log.Logger.Error("GameObject (Entry: {0} GoType: {1}) have data2 = {2} but AnimKit.dbc (Id: {3}) not exist, set to 0.",
                                             entry,
                                             got.type,
                                             got.BarberChair.SitAnimKit,
                                             got.BarberChair.SitAnimKit);

                            got.BarberChair.SitAnimKit = 0;
                        }

                        break;

                    case GameObjectTypes.GarrisonBuilding:
                    {
                        var transportMap = got.GarrisonBuilding.SpawnMap;

                        if (transportMap != 0)
                            _transportMaps.Add((ushort)transportMap);
                    }

                    break;

                    case GameObjectTypes.GatheringNode:
                        if (got.GatheringNode.open != 0)
                            CheckGOLockId(got, got.GatheringNode.open, 0);

                        if (got.GatheringNode.linkedTrap != 0)
                            CheckGOLinkedTrapId(got, got.GatheringNode.linkedTrap, 20);

                        break;
                }

                GameObjectTemplates[entry] = got;
            } while (result.NextRow());

            Log.Logger.Information("Loaded {0} GameInfo object templates in {1} ms", GameObjectTemplates.Count, Time.GetMSTimeDiffToNow(time));
        }
    }

    private void CheckAndFixGOChairHeightId(GameObjectTemplate goInfo, ref uint dataN, uint n)
    {
        if (dataN <= UnitStandStateType.SitHighChair - UnitStandStateType.SitLowChair)
            return;

        Log.Logger.Error("Gameobject (Entry: {0} GoType: {1}) have data{2}={3} but correct chair height in range 0..{4}.", goInfo.entry, goInfo.type, n, dataN, UnitStandStateType.SitHighChair - UnitStandStateType.SitLowChair);

        // prevent client and server unexpected work
        dataN = 0;
    }

    private void CheckGOConsumable(GameObjectTemplate goInfo, uint dataN, uint n)
    {
        // 0/1 correct values
        if (dataN <= 1)
            return;

        Log.Logger.Error("Gameobject (Entry: {0} GoType: {1}) have data{2}={3} but expected boolean (0/1) consumable field value.",
                         goInfo.entry,
                         goInfo.type,
                         n,
                         dataN);
    }

    private void CheckGOLinkedTrapId(GameObjectTemplate goInfo, uint dataN, uint n)
    {
        var trapInfo = GetGameObjectTemplate(dataN);

        if (trapInfo != null && trapInfo.type != GameObjectTypes.Trap)
            Log.Logger.Error("Gameobject (Entry: {0} GoType: {1}) have data{2}={3} but GO (Entry {4}) have not GAMEOBJECT_TYPE_TRAP type.", goInfo.entry, goInfo.type, n, dataN, dataN);
    }

    private void CheckGOLockId(GameObjectTemplate goInfo, uint dataN, uint n)
    {
        if (_cliDB.LockStorage.ContainsKey(dataN))
            return;

        Log.Logger.Debug("Gameobject (Entry: {0} GoType: {1}) have data{2}={3} but lock (Id: {4}) not found.", goInfo.entry, goInfo.type, n, goInfo.Door.open, goInfo.Door.open);
    }

    private void CheckGONoDamageImmuneId(GameObjectTemplate goTemplate, uint dataN, uint n)
    {
        // 0/1 correct values
        if (dataN <= 1)
            return;

        Log.Logger.Error("Gameobject (Entry: {0} GoType: {1}) have data{2}={3} but expected boolean (0/1) noDamageImmune field value.", goTemplate.entry, goTemplate.type, n, dataN);
    }

    private void CheckGOSpellId(GameObjectTemplate goInfo, uint dataN, uint n)
    {
        if (_spellManager.HasSpellInfo(dataN))
            return;

        Log.Logger.Error("Gameobject (Entry: {0} GoType: {1}) have data{2}={3} but Spell (Entry {4}) not exist.", goInfo.entry, goInfo.type, n, dataN, dataN);
    }
}
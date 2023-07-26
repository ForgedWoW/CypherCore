// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Text;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Events;
using Forged.MapServer.Spells;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
// ReSharper disable UnusedType.Local
// ReSharper disable MemberHidesStaticFromOuterClass

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("lookup")]
internal class LookupCommands
{
    private static readonly int MaxResults = 50;

    [Command("area", RBACPermissions.CommandLookupArea, true)]
    private static bool HandleLookupAreaCommand(CommandHandler handler, string namePart)
    {
        namePart = namePart.ToLower();

        var found = false;
        uint count = 0;

        // Search in AreaTable.dbc
        foreach (var areaEntry in handler.CliDB.AreaTableStorage.Values)
        {
            var locale = handler.SessionDbcLocale;
            var name = areaEntry.AreaName[locale];

            if (string.IsNullOrEmpty(name))
                continue;

            if (!name.Like(namePart))
            {
                locale = 0;

                for (; locale < Locale.Total; ++locale)
                {
                    if (locale == handler.SessionDbcLocale)
                        continue;

                    name = areaEntry.AreaName[locale];

                    if (name.IsEmpty())
                        continue;

                    if (name.Like(namePart))
                        break;
                }
            }

            if (locale < Locale.Total)
            {
                if (MaxResults != 0 && count++ == MaxResults)
                {
                    handler.SendSysMessage(CypherStrings.CommandLookupMaxResults, MaxResults);

                    return true;
                }

                // send area in "id - [name]" format
                var ss = "";

                if (handler.Session != null)
                    ss += areaEntry.Id + " - |cffffffff|Harea:" + areaEntry.Id + "|h[" + name + "]|h|r";
                else
                    ss += areaEntry.Id + " - " + name;

                handler.SendSysMessage(ss);

                found = found switch
                {
                    false => true,
                    _     => true
                };
            }
        }

        if (!found)
            handler.SendSysMessage(CypherStrings.CommandNoareafound);

        return true;
    }

    [Command("creature", RBACPermissions.CommandLookupCreature, true)]
    private static bool HandleLookupCreatureCommand(CommandHandler handler, string namePart)
    {
        namePart = namePart.ToLower();

        var found = false;
        uint count = 0;

        var ctc = handler.ObjectManager.CreatureTemplates;

        foreach (var template in ctc)
        {
            var id = template.Value.Entry;
            var localeIndex = handler.SessionDbLocaleIndex;
            var creatureLocale = handler.ObjectManager.GetCreatureLocale(id);

            if (creatureLocale != null)
                if (creatureLocale.Name.Length > localeIndex && !string.IsNullOrEmpty(creatureLocale.Name[localeIndex]))
                {
                    var creatureName = creatureLocale.Name[localeIndex];

                    if (creatureName.Like(namePart))
                    {
                        if (MaxResults != 0 && count++ == MaxResults)
                        {
                            handler.SendSysMessage(CypherStrings.CommandLookupMaxResults, MaxResults);

                            return true;
                        }

                        if (handler.Session != null)
                            handler.SendSysMessage(CypherStrings.CreatureEntryListChat, id, id, creatureName);
                        else
                            handler.SendSysMessage(CypherStrings.CreatureEntryListConsole, id, creatureName);

                        found = found switch
                        {
                            false => true,
                            _     => true
                        };

                        continue;
                    }
                }

            var name = template.Value.Name;

            if (string.IsNullOrEmpty(name))
                continue;

            if (!name.Like(namePart))
                continue;

            if (MaxResults != 0 && count++ == MaxResults)
            {
                handler.SendSysMessage(CypherStrings.CommandLookupMaxResults, MaxResults);

                return true;
            }

            if (handler.Session != null)
                handler.SendSysMessage(CypherStrings.CreatureEntryListChat, id, id, name);
            else
                handler.SendSysMessage(CypherStrings.CreatureEntryListConsole, id, name);

            found = found switch
            {
                false => true,
                _     => true
            };
        }

        if (!found)
            handler.SendSysMessage(CypherStrings.CommandNocreaturefound);

        return true;
    }

    [Command("event", RBACPermissions.CommandLookupEvent, true)]
    private static bool HandleLookupEventCommand(CommandHandler handler, string namePart)
    {
        namePart = namePart.ToLower();

        var found = false;
        uint count = 0;

        var events = handler.ClassFactory.Resolve<GameEventManager>().GetEventMap();
        var activeEvents = handler.ClassFactory.Resolve<GameEventManager>().GetActiveEventList();

        for (ushort id = 0; id < events.Length; ++id)
        {
            var eventData = events[id];

            var descr = eventData.Description;

            if (string.IsNullOrEmpty(descr))
                continue;

            if (!descr.Like(namePart))
                continue;

            if (MaxResults != 0 && count++ == MaxResults)
            {
                handler.SendSysMessage(CypherStrings.CommandLookupMaxResults, MaxResults);

                return true;
            }

            var active = activeEvents.Contains(id) ? handler.GetCypherString(CypherStrings.Active) : "";

            if (handler.Session != null)
                handler.SendSysMessage(CypherStrings.EventEntryListChat, id, id, eventData.Description, active);
            else
                handler.SendSysMessage(CypherStrings.EventEntryListConsole, id, eventData.Description, active);

            found = found switch
            {
                false => true,
                _     => true
            };
        }

        if (!found)
            handler.SendSysMessage(CypherStrings.Noeventfound);

        return true;
    }

    [Command("faction", RBACPermissions.CommandLookupFaction, true)]
    private static bool HandleLookupFactionCommand(CommandHandler handler, string namePart)
    {
        // Can be NULL at console call
        var target = handler.SelectedPlayer;

        namePart = namePart.ToLower();

        var found = false;
        uint count = 0;


        foreach (var factionEntry in handler.CliDB.FactionStorage.Values)
        {
            var factionState = target?.ReputationMgr.GetState(factionEntry);

            var locale = handler.SessionDbcLocale;
            var name = factionEntry.Name[locale];

            if (string.IsNullOrEmpty(name))
                continue;

            if (!name.Like(namePart))
            {
                locale = 0;

                for (; locale < Locale.Total; ++locale)
                {
                    if (locale == handler.SessionDbcLocale)
                        continue;

                    name = factionEntry.Name[locale];

                    if (name.IsEmpty())
                        continue;

                    if (name.Like(namePart))
                        break;
                }
            }

            if (locale < Locale.Total)
            {
                if (MaxResults != 0 && count++ == MaxResults)
                {
                    handler.SendSysMessage(CypherStrings.CommandLookupMaxResults, MaxResults);

                    return true;
                }

                // send faction in "id - [faction] rank reputation [visible] [at war] [own team] [unknown] [invisible] [inactive]" format
                // or              "id - [faction] [no reputation]" format
                StringBuilder ss = new();

                if (handler.Session != null)
                    ss.AppendFormat("{0} - |cffffffff|Hfaction:{0}|h[{1}]|h|r", factionEntry.Id, name);
                else
                    ss.Append(factionEntry.Id + " - " + name);

                if (factionState != null) // and then target != NULL also
                {
                    var index = target.ReputationMgr.GetReputationRankStrIndex(factionEntry);
                    var rankName = handler.GetCypherString((CypherStrings)index);

                    ss.Append($" {rankName}|h|r ({target.ReputationMgr.GetReputation(factionEntry)})");

                    if (factionState.Flags.HasFlag(ReputationFlags.Visible))
                        ss.Append(handler.GetCypherString(CypherStrings.FactionVisible));

                    if (factionState.Flags.HasFlag(ReputationFlags.AtWar))
                        ss.Append(handler.GetCypherString(CypherStrings.FactionAtwar));

                    if (factionState.Flags.HasFlag(ReputationFlags.Peaceful))
                        ss.Append(handler.GetCypherString(CypherStrings.FactionPeaceForced));

                    if (factionState.Flags.HasFlag(ReputationFlags.Hidden))
                        ss.Append(handler.GetCypherString(CypherStrings.FactionHidden));

                    if (factionState.Flags.HasFlag(ReputationFlags.Header))
                        ss.Append(handler.GetCypherString(CypherStrings.FactionInvisibleForced));

                    if (factionState.Flags.HasFlag(ReputationFlags.Inactive))
                        ss.Append(handler.GetCypherString(CypherStrings.FactionInactive));
                }
                else
                    ss.Append(handler.GetCypherString(CypherStrings.FactionNoreputation));

                handler.SendSysMessage(ss.ToString());

                found = found switch
                {
                    false => true,
                    _     => true
                };
            }
        }

        if (!found)
            handler.SendSysMessage(CypherStrings.CommandFactionNotfound);

        return true;
    }

    [Command("itemset", RBACPermissions.CommandLookupItemset, true)]
    private static bool HandleLookupItemSetCommand(CommandHandler handler, string namePart)
    {
        namePart = namePart.ToLower();

        var found = false;
        uint count = 0;

        // Search in ItemSet.dbc
        foreach (var set in handler.CliDB.ItemSetStorage.Values)
        {
            var locale = handler.SessionDbcLocale;
            var name = set.Name[locale];

            if (name.IsEmpty())
                continue;

            if (!name.Like(namePart))
            {
                locale = 0;

                for (; locale < Locale.Total; ++locale)
                {
                    if (locale == handler.SessionDbcLocale)
                        continue;

                    name = set.Name[locale];

                    if (name.IsEmpty())
                        continue;

                    if (name.Like(namePart))
                        break;
                }
            }

            if (locale < Locale.Total)
            {
                if (MaxResults != 0 && count++ == MaxResults)
                {
                    handler.SendSysMessage(CypherStrings.CommandLookupMaxResults, MaxResults);

                    return true;
                }

                // send item set in "id - [namedlink locale]" format
                if (handler.Session != null)
                    handler.SendSysMessage(CypherStrings.ItemsetListChat, set.Id, set.Id, name, "");
                else
                    handler.SendSysMessage(CypherStrings.ItemsetListConsole, set.Id, name, "");

                found = found switch
                {
                    false => true,
                    _     => true
                };
            }
        }

        if (!found)
            handler.SendSysMessage(CypherStrings.CommandNoitemsetfound);

        return true;
    }

    [Command("object", RBACPermissions.CommandLookupObject, true)]
    private static bool HandleLookupObjectCommand(CommandHandler handler, string namePart)
    {
        var found = false;
        uint count = 0;

        var gotc = handler.ObjectManager.GameObjectTemplates;

        foreach (var template in gotc.Values)
        {
            var localeIndex = handler.SessionDbLocaleIndex;

            var objectLocalte = handler.ObjectManager.GetGameObjectLocale(template.entry);

            if (objectLocalte != null)
                if (objectLocalte.Name.Length > localeIndex && !string.IsNullOrEmpty(objectLocalte.Name[localeIndex]))
                {
                    var objName = objectLocalte.Name[localeIndex];

                    if (objName.Like(namePart))
                    {
                        if (MaxResults != 0 && count++ == MaxResults)
                        {
                            handler.SendSysMessage(CypherStrings.CommandLookupMaxResults, MaxResults);

                            return true;
                        }

                        if (handler.Session != null)
                            handler.SendSysMessage(CypherStrings.GoEntryListChat, template.entry, template.entry, objName);
                        else
                            handler.SendSysMessage(CypherStrings.GoEntryListConsole, template.entry, objName);

                        found = found switch
                        {
                            false => true,
                            _     => true
                        };

                        continue;
                    }
                }

            var name = template.name;

            if (string.IsNullOrEmpty(name))
                continue;

            if (name.Like(namePart))
            {
                if (MaxResults != 0 && count++ == MaxResults)
                {
                    handler.SendSysMessage(CypherStrings.CommandLookupMaxResults, MaxResults);

                    return true;
                }

                if (handler.Session != null)
                    handler.SendSysMessage(CypherStrings.GoEntryListChat, template.entry, template.entry, name);
                else
                    handler.SendSysMessage(CypherStrings.GoEntryListConsole, template.entry, name);

                found = found switch
                {
                    false => true,
                    _     => true
                };
            }
        }

        if (!found)
            handler.SendSysMessage(CypherStrings.CommandNogameobjectfound);

        return true;
    }

    [Command("skill", RBACPermissions.CommandLookupSkill, true)]
    private static bool HandleLookupSkillCommand(CommandHandler handler, string namePart)
    {
        // can be NULL in console call
        var target = handler.SelectedPlayer;

        var found = false;
        uint count = 0;

        // Search in SkillLine.dbc
        foreach (var skillInfo in handler.CliDB.SkillLineStorage.Values)
        {
            var locale = handler.SessionDbcLocale;
            var name = skillInfo.DisplayName[locale];

            if (string.IsNullOrEmpty(name))
                continue;

            if (!name.Like(namePart))
            {
                locale = 0;

                for (; locale < Locale.Total; ++locale)
                {
                    if (locale == handler.SessionDbcLocale)
                        continue;

                    name = skillInfo.DisplayName[locale];

                    if (name.IsEmpty())
                        continue;

                    if (name.Like(namePart))
                        break;
                }
            }

            if (locale >= Locale.Total)
                continue;

            if (MaxResults != 0 && count++ == MaxResults)
            {
                handler.SendSysMessage(CypherStrings.CommandLookupMaxResults, MaxResults);

                return true;
            }

            var valStr = "";
            var knownStr = "";

            if (target != null && target.HasSkill((SkillType)skillInfo.Id))
            {
                knownStr = handler.GetCypherString(CypherStrings.Known);
                uint curValue = target.GetPureSkillValue((SkillType)skillInfo.Id);
                uint maxValue = target.GetPureMaxSkillValue((SkillType)skillInfo.Id);
                uint permValue = target.GetSkillPermBonusValue(skillInfo.Id);
                uint tempValue = target.GetSkillTempBonusValue(skillInfo.Id);

                var valFormat = handler.GetCypherString(CypherStrings.SkillValues);
                valStr = string.Format(valFormat, curValue, maxValue, permValue, tempValue);
            }

            // send skill in "id - [namedlink locale]" format
            if (handler.Session != null)
                handler.SendSysMessage(CypherStrings.SkillListChat, skillInfo.Id, skillInfo.Id, name, "", knownStr, valStr);
            else
                handler.SendSysMessage(CypherStrings.SkillListConsole, skillInfo.Id, name, "", knownStr, valStr);

            found = found switch
            {
                false => true,
                _     => true   
            };
        }

        if (!found)
            handler.SendSysMessage(CypherStrings.CommandNoskillfound);

        return true;
    }

    [Command("taxinode", RBACPermissions.CommandLookupTaxinode, true)]
    private static bool HandleLookupTaxiNodeCommand(CommandHandler handler, string namePart)
    {
        var found = false;
        uint count = 0;
        var locale = handler.SessionDbcLocale;

        // Search in TaxiNodes.dbc
        foreach (var nodeEntry in handler.CliDB.TaxiNodesStorage.Values)
        {
            var name = nodeEntry.Name[locale];

            if (string.IsNullOrEmpty(name))
                continue;

            if (!name.Like(namePart))
                continue;

            if (MaxResults != 0 && count++ == MaxResults)
            {
                handler.SendSysMessage(CypherStrings.CommandLookupMaxResults, MaxResults);

                return true;
            }

            // send taxinode in "id - [name] (Map:m X:x Y:y Z:z)" format
            if (handler.Session != null)
                handler.SendSysMessage(CypherStrings.TaxinodeEntryListChat,
                                       nodeEntry.Id,
                                       nodeEntry.Id,
                                       name,
                                       "",
                                       nodeEntry.ContinentID,
                                       nodeEntry.Pos.X,
                                       nodeEntry.Pos.Y,
                                       nodeEntry.Pos.Z);
            else
                handler.SendSysMessage(CypherStrings.TaxinodeEntryListConsole,
                                       nodeEntry.Id,
                                       name,
                                       "",
                                       nodeEntry.ContinentID,
                                       nodeEntry.Pos.X,
                                       nodeEntry.Pos.Y,
                                       nodeEntry.Pos.Z);

            found = found switch
            {
                false => true,
                _     => true
            };
        }

        if (!found)
            handler.SendSysMessage(CypherStrings.CommandNotaxinodefound);

        return true;
    }

    [Command("tele", RBACPermissions.CommandLookupTele, true)]
    private static bool HandleLookupTeleCommand(CommandHandler handler, string namePart)
    {
        namePart = namePart.ToLower();

        StringBuilder reply = new();
        uint count = 0;
        var limitReached = false;

        foreach (var tele in handler.ObjectManager.GameTeleStorage)
        {
            if (!tele.Value.Name.Like(namePart))
                continue;

            if (MaxResults != 0 && count++ == MaxResults)
            {
                limitReached = true;

                break;
            }

            reply.AppendFormat(handler.Player != null ? "  |cffffffff|Htele:{0}|h[{1}]|h|r\n" : "  {0} : {1}\n", tele.Key, tele.Value.Name);
        }

        if (reply.Capacity == 0)
            handler.SendSysMessage(CypherStrings.CommandTeleNolocation);
        else
            handler.SendSysMessage(CypherStrings.CommandTeleLocation, reply.ToString());

        if (limitReached)
            handler.SendSysMessage(CypherStrings.CommandLookupMaxResults, MaxResults);

        return true;
    }

    [Command("title", RBACPermissions.CommandLookupTitle, true)]
    private static bool HandleLookupTitleCommand(CommandHandler handler, string namePart)
    {
        // can be NULL in console call
        var target = handler.SelectedPlayer;

        // title name have single string arg for player name
        var targetName = target != null ? target.GetName() : "NAME";

        uint counter = 0; // Counter for figure out that we found smth.

        // Search in CharTitles.dbc
        foreach (var titleInfo in handler.CliDB.CharTitlesStorage.Values)
            for (var gender = Gender.Male; gender <= Gender.Female; ++gender)
            {
                if (target != null && target.Gender != gender)
                    continue;

                var locale = handler.SessionDbcLocale;
                var name = gender == Gender.Male ? titleInfo.Name[locale] : titleInfo.Name1[locale];

                if (string.IsNullOrEmpty(name))
                    continue;

                if (!name.Like(namePart))
                {
                    locale = 0;

                    for (; locale < Locale.Total; ++locale)
                    {
                        if (locale == handler.SessionDbcLocale)
                            continue;

                        name = (gender == Gender.Male ? titleInfo.Name : titleInfo.Name1)[locale];

                        if (name.IsEmpty())
                            continue;

                        if (name.Like(namePart))
                            break;
                    }
                }

                if (locale < Locale.Total)
                {
                    if (MaxResults != 0 && counter == MaxResults)
                    {
                        handler.SendSysMessage(CypherStrings.CommandLookupMaxResults, MaxResults);

                        return true;
                    }

                    var knownStr = target != null && target.HasTitle(titleInfo) ? handler.GetCypherString(CypherStrings.Known) : "";

                    var activeStr = target != null && target.PlayerData.PlayerTitle == titleInfo.MaskID
                                        ? handler.GetCypherString(CypherStrings.Active)
                                        : "";

                    var titleNameStr = string.Format(name.ConvertFormatSyntax(), targetName);

                    // send title in "id (idx:idx) - [namedlink locale]" format
                    if (handler.Session != null)
                        handler.SendSysMessage(CypherStrings.TitleListChat, titleInfo.Id, titleInfo.MaskID, titleInfo.Id, titleNameStr, "", knownStr, activeStr);
                    else
                        handler.SendSysMessage(CypherStrings.TitleListConsole, titleInfo.Id, titleInfo.MaskID, titleNameStr, "", knownStr, activeStr);

                    ++counter;
                }
            }

        if (counter == 0) // if counter == 0 then we found nth
            handler.SendSysMessage(CypherStrings.CommandNotitlefound);

        return true;
    }

    [CommandGroup("item")]
    private class LookupItemCommands
    {
        [Command("", RBACPermissions.CommandLookupItem, true)]
        private static bool HandleLookupItemCommand(CommandHandler handler, string namePart)
        {
            var found = false;
            uint count = 0;

            // Search in ItemSparse
            var its = handler.ObjectManager.ItemTemplates;

            foreach (var template in its.Values)
            {
                var name = template.GetName(handler.SessionDbcLocale);

                if (string.IsNullOrEmpty(name))
                    continue;

                if (!name.Like(namePart))
                    continue;

                if (MaxResults != 0 && count++ == MaxResults)
                {
                    handler.SendSysMessage(CypherStrings.CommandLookupMaxResults, MaxResults);

                    return true;
                }

                if (handler.Session != null)
                    handler.SendSysMessage(CypherStrings.ItemListChat, template.Id, template.Id, name);
                else
                    handler.SendSysMessage(CypherStrings.ItemListConsole, template.Id, name);

                found = found switch
                {
                    false => true,
                    _     => true
                };
            }

            if (!found)
                handler.SendSysMessage(CypherStrings.CommandNoitemfound);

            return true;
        }

        [Command("id", RBACPermissions.CommandLookupItemId, true)]
        private static bool HandleLookupItemIdCommand(CommandHandler handler, uint id)
        {
            var itemTemplate = handler.ObjectManager.GetItemTemplate(id);

            if (itemTemplate != null)
            {
                var name = itemTemplate.GetName(handler.SessionDbcLocale);

                if (name.IsEmpty())
                {
                    handler.SendSysMessage(CypherStrings.CommandNoitemfound);

                    return true;
                }

                if (handler.Session != null)
                    handler.SendSysMessage(CypherStrings.ItemListChat, id, id, name);
                else
                    handler.SendSysMessage(CypherStrings.ItemListConsole, id, name);
            }
            else
                handler.SendSysMessage(CypherStrings.CommandNoitemfound);

            return true;
        }

        [Command("set", RBACPermissions.CommandLookupItemset, true)]
        private static bool HandleLookupItemSetCommand(CommandHandler handler, string namePart)
        {
            if (namePart.IsEmpty())
                return false;

            var found = false;
            uint count = 0;
            var maxResults = handler.Configuration.GetDefaultValue("Command:LookupMaxResults", 0);

            // Search in ItemSet.dbc
            foreach (var (id, set) in handler.CliDB.ItemSetStorage)
            {
                var locale = handler.SessionDbcLocale;
                var name = set.Name[locale];

                if (name.IsEmpty())
                    continue;

                if (!name.Equals(namePart, StringComparison.OrdinalIgnoreCase))
                {
                    locale = Locale.enUS;

                    for (; locale < Locale.Total; ++locale)
                    {
                        if (locale == handler.SessionDbcLocale)
                            continue;

                        name = set.Name[locale];

                        if (name.IsEmpty())
                            continue;

                        if (name.Equals(namePart, StringComparison.OrdinalIgnoreCase))
                            break;
                    }
                }

                if (locale >= Locale.Total)
                    continue;

                if (maxResults != 0 && count++ == maxResults)
                {
                    handler.SendSysMessage(CypherStrings.CommandLookupMaxResults, maxResults);

                    return true;
                }

                // send item set in "id - [namedlink locale]" format
                if (handler.Session != null)
                    handler.SendSysMessage(CypherStrings.ItemsetListChat, id, id, name, "");
                else
                    handler.SendSysMessage(CypherStrings.ItemsetListConsole, id, name, "");

                found = found switch
                {
                    false => true,
                    _     => true
                };
            }

            if (!found)
                handler.SendSysMessage(CypherStrings.CommandNoitemsetfound);

            return true;
        }
    }

    [CommandGroup("map")]
    private class LookupMapCommands
    {
        [Command("map", RBACPermissions.CommandLookupMap, true)]
        private static bool HandleLookupMapCommand(CommandHandler handler, string namePart)
        {
            if (namePart.IsEmpty())
                return false;

            uint counter = 0;

            // search in Map.dbc
            foreach (var mapInfo in handler.CliDB.MapStorage.Values)
            {
                var locale = handler.SessionDbcLocale;
                var name = mapInfo.MapName[locale];

                if (string.IsNullOrEmpty(name))
                    continue;

                if (!name.Like(namePart) && handler.Session != null)
                {
                    locale = 0;

                    for (; locale < Locale.Total; ++locale)
                    {
                        if (locale == handler.SessionDbcLocale)
                            continue;

                        name = mapInfo.MapName[locale];

                        if (name.IsEmpty())
                            continue;

                        if (name.Like(namePart))
                            break;
                    }
                }

                if (locale < Locale.Total)
                {
                    if (MaxResults != 0 && counter == MaxResults)
                    {
                        handler.SendSysMessage(CypherStrings.CommandLookupMaxResults, MaxResults);

                        return true;
                    }

                    StringBuilder ss = new();
                    ss.Append(mapInfo.Id + " - [" + name + ']');

                    if (mapInfo.IsContinent())
                        ss.Append(handler.GetCypherString(CypherStrings.Continent));

                    switch (mapInfo.InstanceType)
                    {
                        case MapTypes.Instance:
                            ss.Append(handler.GetCypherString(CypherStrings.Instance));

                            break;
                        case MapTypes.Raid:
                            ss.Append(handler.GetCypherString(CypherStrings.Raid));

                            break;
                        case MapTypes.Battleground:
                            ss.Append(handler.GetCypherString(CypherStrings.Battleground));

                            break;
                        case MapTypes.Arena:
                            ss.Append(handler.GetCypherString(CypherStrings.Arena));

                            break;
                    }

                    handler.SendSysMessage(ss.ToString());

                    ++counter;
                }
            }

            if (counter == 0)
                handler.SendSysMessage(CypherStrings.CommandNomapfound);

            return true;
        }

        [Command("id", RBACPermissions.CommandLookupMapId, true)]
        private static bool HandleLookupMapIdCommand(CommandHandler handler, uint id)
        {
            if (handler.CliDB.MapStorage.TryGetValue(id, out var mapInfo))
            {
                var locale = handler.Session?.SessionDbcLocale ?? handler.WorldManager.DefaultDbcLocale;
                var name = mapInfo.MapName[locale];

                if (name.IsEmpty())
                {
                    handler.SendSysMessage(CypherStrings.CommandNomapfound);

                    return true;
                }

                StringBuilder ss = new();
                ss.Append($"{id} - [{name}]");

                if (mapInfo.IsContinent())
                    ss.Append(handler.GetCypherString(CypherStrings.Continent));

                switch (mapInfo.InstanceType)
                {
                    case MapTypes.Instance:
                        ss.Append(handler.GetCypherString(CypherStrings.Instance));

                        break;
                    case MapTypes.Raid:
                        ss.Append(handler.GetCypherString(CypherStrings.Raid));

                        break;
                    case MapTypes.Battleground:
                        ss.Append(handler.GetCypherString(CypherStrings.Battleground));

                        break;
                    case MapTypes.Arena:
                        ss.Append(handler.GetCypherString(CypherStrings.Arena));

                        break;
                    case MapTypes.Scenario:
                        ss.Append(handler.GetCypherString(CypherStrings.Scenario));

                        break;
                }

                handler.SendSysMessage(ss.ToString());
            }
            else
                handler.SendSysMessage(CypherStrings.CommandNomapfound);

            return true;
        }
    }

    [CommandGroup("player")]
    private class LookupPlayerCommands
    {
        [Command("account", RBACPermissions.CommandLookupPlayerAccount)]
        private static bool HandleLookupPlayerAccountCommand(CommandHandler handler, string account, int limit = -1)
        {
            var stmt = handler.ClassFactory.Resolve<LoginDatabase>().GetPreparedStatement(LoginStatements.SEL_ACCOUNT_LIST_BY_NAME);
            stmt.AddValue(0, account);

            return LookupPlayerSearchCommand(handler.ClassFactory.Resolve<LoginDatabase>().Query(stmt), limit, handler);
        }

        [Command("email", RBACPermissions.CommandLookupPlayerEmail)]
        private static bool HandleLookupPlayerEmailCommand(CommandHandler handler, string email, int limit = -1)
        {
            var stmt = handler.ClassFactory.Resolve<LoginDatabase>().GetPreparedStatement(LoginStatements.SEL_ACCOUNT_LIST_BY_EMAIL);
            stmt.AddValue(0, email);

            return LookupPlayerSearchCommand(handler.ClassFactory.Resolve<LoginDatabase>().Query(stmt), limit, handler);
        }

        [Command("ip", RBACPermissions.CommandLookupPlayerIp)]
        private static bool HandleLookupPlayerIpCommand(CommandHandler handler, string ip, int limit = -1)
        {
            var target = handler.SelectedPlayer;

            if (ip.IsEmpty())
            {
                // NULL only if used from console
                if (target == null || target == handler.Session.Player)
                    return false;

                ip = target.Session.RemoteAddress;
            }

            var stmt = handler.ClassFactory.Resolve<LoginDatabase>().GetPreparedStatement(LoginStatements.SEL_ACCOUNT_BY_IP);
            stmt.AddValue(0, ip);

            return LookupPlayerSearchCommand(handler.ClassFactory.Resolve<LoginDatabase>().Query(stmt), limit, handler);
        }

        private static bool LookupPlayerSearchCommand(SQLResult result, int limit, CommandHandler handler)
        {
            if (result.IsEmpty())
            {
                handler.SendSysMessage(CypherStrings.NoPlayersFound);

                return false;
            }

            var counter = 0;
            uint count = 0;

            do
            {
                if (MaxResults != 0 && count++ == MaxResults)
                {
                    handler.SendSysMessage(CypherStrings.CommandLookupMaxResults, MaxResults);

                    return true;
                }

                var accountId = result.Read<uint>(0);
                var accountName = result.Read<string>(1);

                var stmt = handler.ClassFactory.Resolve<CharacterDatabase>().GetPreparedStatement(CharStatements.SEL_CHAR_GUID_NAME_BY_ACC);
                stmt.AddValue(0, accountId);
                var result2 = handler.ClassFactory.Resolve<CharacterDatabase>().Query(stmt);

                if (!result2.IsEmpty())
                {
                    handler.SendSysMessage(CypherStrings.LookupPlayerAccount, accountName, accountId);

                    do
                    {
                        var guid = ObjectGuid.Create(HighGuid.Player, result2.Read<ulong>(0));
                        var name = result2.Read<string>(1);
                        var online = result2.Read<bool>(2);

                        handler.SendSysMessage(CypherStrings.LookupPlayerCharacter, name, guid.ToString(), online ? handler.GetCypherString(CypherStrings.Online) : "");
                        ++counter;
                    } while (result2.NextRow() && (limit == -1 || counter < limit));
                }
            } while (result.NextRow());

            if (counter == 0) // empty accounts only
            {
                handler.SendSysMessage(CypherStrings.NoPlayersFound);

                return false;
            }

            return true;
        }
    }

    [CommandGroup("quest")]
    private class LookupQuestCommands
    {
        [Command("", RBACPermissions.CommandLookupQuest, true)]
        private static bool HandleLookupQuestCommand(CommandHandler handler, string namePart)
        {
            // can be NULL at console call
            var target = handler.SelectedPlayer;

            namePart = namePart.ToLower();

            var found = false;
            uint count = 0;

            var qTemplates = handler.ObjectManager.QuestTemplates;

            foreach (var qInfo in qTemplates.Values)
            {
                int localeIndex = handler.SessionDbLocaleIndex;
                var questLocale = handler.ObjectManager.GetQuestLocale(qInfo.Id);

                if (questLocale != null)
                    if (questLocale.LogTitle.Length > localeIndex && !questLocale.LogTitle[localeIndex].IsEmpty())
                    {
                        var questTitle = questLocale.LogTitle[localeIndex];

                        if (questTitle.Like(namePart))
                        {
                            if (MaxResults != 0 && count++ == MaxResults)
                            {
                                handler.SendSysMessage(CypherStrings.CommandLookupMaxResults, MaxResults);

                                return true;
                            }

                            var statusStr = "";

                            if (target != null)
                                statusStr = target.GetQuestStatus(qInfo.Id) switch
                                {
                                    QuestStatus.Complete   => handler.GetCypherString(CypherStrings.CommandQuestComplete),
                                    QuestStatus.Incomplete => handler.GetCypherString(CypherStrings.CommandQuestActive),
                                    QuestStatus.Rewarded   => handler.GetCypherString(CypherStrings.CommandQuestRewarded),
                                    _                      => statusStr
                                };

                            if (handler.Session != null)
                            {
                                var maxLevel = 0;
                                var questLevels = handler.ClassFactory.Resolve<DB2Manager>().GetContentTuningData(qInfo.ContentTuningId, handler.Session.Player.PlayerData.CtrOptions.Value.ContentTuningConditionMask);

                                if (questLevels.HasValue)
                                    maxLevel = questLevels.Value.MaxLevel;

                                var scalingFactionGroup = 0;

                                if (handler.CliDB.ContentTuningStorage.TryGetValue(qInfo.ContentTuningId, out var contentTuning))
                                    scalingFactionGroup = contentTuning.GetScalingFactionGroup();

                                handler.SendSysMessage(CypherStrings.QuestListChat,
                                                       qInfo.Id,
                                                       qInfo.Id,
                                                       handler.Session.Player.GetQuestLevel(qInfo),
                                                       handler.Session.Player.GetQuestMinLevel(qInfo),
                                                       maxLevel,
                                                       scalingFactionGroup,
                                                       questTitle,
                                                       statusStr);
                            }
                            else
                                handler.SendSysMessage(CypherStrings.QuestListConsole, qInfo.Id, questTitle, statusStr);

                            found = found switch
                            {
                                false => true,
                                _     => true
                            };

                            continue;
                        }
                    }

                var title = qInfo.LogTitle;

                if (string.IsNullOrEmpty(title))
                    continue;

                if (!title.Like(namePart))
                    continue;

                {
                    if (MaxResults != 0 && count++ == MaxResults)
                    {
                        handler.SendSysMessage(CypherStrings.CommandLookupMaxResults, MaxResults);

                        return true;
                    }

                    var statusStr = "";

                    if (target != null)
                    {
                        var status = target.GetQuestStatus(qInfo.Id);

                        statusStr = status switch
                        {
                            QuestStatus.Complete   => handler.GetCypherString(CypherStrings.CommandQuestComplete),
                            QuestStatus.Incomplete => handler.GetCypherString(CypherStrings.CommandQuestActive),
                            QuestStatus.Rewarded   => handler.GetCypherString(CypherStrings.CommandQuestRewarded),
                            _                      => statusStr
                        };
                    }

                    if (handler.Session != null)
                    {
                        var maxLevel = 0;
                        var questLevels = handler.ClassFactory.Resolve<DB2Manager>().GetContentTuningData(qInfo.ContentTuningId, handler.Session.Player.PlayerData.CtrOptions.Value.ContentTuningConditionMask);

                        if (questLevels.HasValue)
                            maxLevel = questLevels.Value.MaxLevel;

                        var scalingFactionGroup = 0;

                        if (handler.CliDB.ContentTuningStorage.TryGetValue(qInfo.ContentTuningId, out var contentTuning))
                            scalingFactionGroup = contentTuning.GetScalingFactionGroup();

                        handler.SendSysMessage(CypherStrings.QuestListChat,
                                               qInfo.Id,
                                               qInfo.Id,
                                               handler.Session.Player.GetQuestLevel(qInfo),
                                               handler.Session.Player.GetQuestMinLevel(qInfo),
                                               maxLevel,
                                               scalingFactionGroup,
                                               title,
                                               statusStr);
                    }
                    else
                        handler.SendSysMessage(CypherStrings.QuestListConsole, qInfo.Id, title, statusStr);

                    found = found switch
                    {
                        false => true,
                        _     => true
                    };
                }
            }

            if (!found)
                handler.SendSysMessage(CypherStrings.CommandNoquestfound);

            return true;
        }

        [Command("id", RBACPermissions.CommandLookupQuestId, true)]
        private static bool HandleLookupQuestIdCommand(CommandHandler handler, uint id)
        {
            // can be NULL at console call
            var target = handler.SelectedPlayerOrSelf;

            var quest = handler.ObjectManager.GetQuestTemplate(id);

            if (quest != null)
            {
                var title = quest.LogTitle;

                if (title.IsEmpty())
                {
                    handler.SendSysMessage(CypherStrings.CommandNoquestfound);

                    return true;
                }

                var statusStr = "";

                if (target != null)
                    statusStr = target.GetQuestStatus(id) switch
                    {
                        QuestStatus.Complete   => handler.GetCypherString(CypherStrings.CommandQuestComplete),
                        QuestStatus.Incomplete => handler.GetCypherString(CypherStrings.CommandQuestActive),
                        QuestStatus.Rewarded   => handler.GetCypherString(CypherStrings.CommandQuestRewarded),
                        _                      => statusStr
                    };

                if (handler.Session != null)
                {
                    var maxLevel = 0;
                    var questLevels = handler.ClassFactory.Resolve<DB2Manager>().GetContentTuningData(quest.ContentTuningId, handler.Session.Player.PlayerData.CtrOptions.Value.ContentTuningConditionMask);

                    if (questLevels.HasValue)
                        maxLevel = questLevels.Value.MaxLevel;

                    var scalingFactionGroup = 0;

                    if (handler.CliDB.ContentTuningStorage.TryGetValue(quest.ContentTuningId, out var contentTuning))
                        scalingFactionGroup = contentTuning.GetScalingFactionGroup();

                    handler.SendSysMessage(CypherStrings.QuestListChat,
                                           id,
                                           id,
                                           handler.Session.Player.GetQuestLevel(quest),
                                           handler.Session.Player.GetQuestMinLevel(quest),
                                           maxLevel,
                                           scalingFactionGroup,
                                           title,
                                           statusStr);
                }
                else
                    handler.SendSysMessage(CypherStrings.QuestListConsole, id, title, statusStr);
            }
            else
                handler.SendSysMessage(CypherStrings.CommandNoquestfound);

            return true;
        }
    }

    [CommandGroup("spell")]
    private class LookupSpellCommands
    {
        [Command("", RBACPermissions.CommandLookupSpell)]
        private static bool HandleLookupSpellCommand(CommandHandler handler, string namePart)
        {
            // can be NULL at console call
            var target = handler.SelectedPlayer;

            var found = false;
            uint count = 0;

            // Search in SpellName.dbc
            foreach (var spellName in handler.CliDB.SpellNameStorage.Values)
            {
                var spellInfo = handler.ClassFactory.Resolve<SpellManager>().GetSpellInfo(spellName.Id);

                if (spellInfo != null)
                {
                    var locale = handler.SessionDbcLocale;
                    var name = spellInfo.SpellName[locale];

                    if (name.IsEmpty())
                        continue;

                    if (!name.Like(namePart))
                    {
                        locale = 0;

                        for (; locale < Locale.Total; ++locale)
                        {
                            if (locale == handler.SessionDbcLocale)
                                continue;

                            name = spellInfo.SpellName[locale];

                            if (name.IsEmpty())
                                continue;

                            if (name.Like(namePart))
                                break;
                        }
                    }

                    if (locale < Locale.Total)
                    {
                        if (MaxResults != 0 && count++ == MaxResults)
                        {
                            handler.SendSysMessage(CypherStrings.CommandLookupMaxResults, MaxResults);

                            return true;
                        }

                        var known = target != null && target.HasSpell(spellInfo.Id);
                        var spellEffectInfo = spellInfo.Effects.Find(spelleffectInfo => spelleffectInfo.IsEffectName(SpellEffectName.LearnSpell));

                        var learnSpellInfo = spellEffectInfo != null ? handler.ClassFactory.Resolve<SpellManager>().GetSpellInfo(spellEffectInfo.TriggerSpell, spellInfo.Difficulty) : null;

                        var talent = spellInfo.HasAttribute(SpellCustomAttributes.IsTalent);
                        var passive = spellInfo.IsPassive;
                        var active = target != null && target.HasAura(spellInfo.Id);

                        // unit32 used to prevent interpreting public byte as char at output
                        // find rank of learned spell for learning spell, or talent rank
                        uint rank = learnSpellInfo?.Rank ?? spellInfo.Rank;

                        // send spell in "id - [name, rank N] [talent] [passive] [learn] [known]" format
                        StringBuilder ss = new();

                        if (handler.Session != null)
                            ss.Append(spellInfo.Id + " - |cffffffff|Hspell:" + spellInfo.Id + "|h[" + name);
                        else
                            ss.Append(spellInfo.Id + " - " + name);

                        // include rank in link name
                        if (rank != 0)
                            ss.Append(handler.GetCypherString(CypherStrings.SpellRank) + rank);

                        if (handler.Session != null)
                            ss.Append("]|h|r");

                        if (talent)
                            ss.Append(handler.GetCypherString(CypherStrings.Talent));

                        if (passive)
                            ss.Append(handler.GetCypherString(CypherStrings.Passive));

                        if (learnSpellInfo != null)
                            ss.Append(handler.GetCypherString(CypherStrings.Learn));

                        if (known)
                            ss.Append(handler.GetCypherString(CypherStrings.Known));

                        if (active)
                            ss.Append(handler.GetCypherString(CypherStrings.Active));

                        handler.SendSysMessage(ss.ToString());

                        found = found switch
                        {
                            false => true,
                            _     => found
                        };
                    }
                }
            }

            if (!found)
                handler.SendSysMessage(CypherStrings.CommandNospellfound);

            return true;
        }

        [Command("id", RBACPermissions.CommandLookupSpellId)]
        private static bool HandleLookupSpellIdCommand(CommandHandler handler, uint id)
        {
            // can be NULL at console call
            var target = handler.SelectedPlayer;

            var spellInfo = handler.ClassFactory.Resolve<SpellManager>().GetSpellInfo(id);

            if (spellInfo != null)
            {
                var locale = handler.SessionDbcLocale;
                var name = spellInfo.SpellName[locale];

                if (string.IsNullOrEmpty(name))
                {
                    handler.SendSysMessage(CypherStrings.CommandNospellfound);

                    return true;
                }

                var known = target != null && target.HasSpell(id);
                var spellEffectInfo = spellInfo.Effects.Find(spelleffectInfo => spelleffectInfo.IsEffectName(SpellEffectName.LearnSpell));

                var learnSpellInfo = handler.ClassFactory.Resolve<SpellManager>().GetSpellInfo(spellEffectInfo.TriggerSpell);

                var talent = spellInfo.HasAttribute(SpellCustomAttributes.IsTalent);
                var passive = spellInfo.IsPassive;
                var active = target != null && target.HasAura(id);

                // unit32 used to prevent interpreting public byte as char at output
                // find rank of learned spell for learning spell, or talent rank
                uint rank = learnSpellInfo?.Rank ?? spellInfo.Rank;

                // send spell in "id - [name, rank N] [talent] [passive] [learn] [known]" format
                StringBuilder ss = new();

                if (handler.Session != null)
                    ss.Append(id + " - |cffffffff|Hspell:" + id + "|h[" + name);
                else
                    ss.Append(id + " - " + name);

                // include rank in link name
                if (rank != 0)
                    ss.Append(handler.GetCypherString(CypherStrings.SpellRank) + rank);

                if (handler.Session != null)
                    ss.Append("]|h|r");

                if (talent)
                    ss.Append(handler.GetCypherString(CypherStrings.Talent));

                if (passive)
                    ss.Append(handler.GetCypherString(CypherStrings.Passive));

                if (learnSpellInfo != null)
                    ss.Append(handler.GetCypherString(CypherStrings.Learn));

                if (known)
                    ss.Append(handler.GetCypherString(CypherStrings.Known));

                if (active)
                    ss.Append(handler.GetCypherString(CypherStrings.Active));

                handler.SendSysMessage(ss.ToString());
            }
            else
                handler.SendSysMessage(CypherStrings.CommandNospellfound);

            return true;
        }
    }
}
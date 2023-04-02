// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Groups;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Networking.Packets.Chat;
using Forged.MapServer.Server;
using Framework.Collections;
using Framework.Constants;
using Framework.IO;

namespace Forged.MapServer.Chat;

public class CommandHandler
{
    private static readonly string[] spellKeys =
    {
        "Hspell",   // normal spell
        "Htalent",  // talent spell
        "Henchant", // enchanting recipe spell
        "Htrade",   // profession/skill spell
        "Hglyph",   // glyph
    };

    public CommandHandler(WorldSession session = null)
    {
        Session = session;
    }

    public bool HasSentErrorMessage { get; private set; }

    public bool IsConsole => Session == null;

    public virtual string NameLink => GetNameLink(Session.Player);

    public Player Player => Session?.Player;

    public Creature SelectedCreature
    {
        get
        {
            if (Session == null)
                return null;

            return ObjectAccessor.GetCreatureOrPetOrVehicle(Session.Player, Session.Player.Target);
        }
    }

    public WorldObject SelectedObject
    {
        get
        {
            if (Session == null)
                return null;

            var selected = Session.Player.Target;

            if (selected.IsEmpty)
                return NearbyGameObject;

            return Global.ObjAccessor.GetUnit(Session.Player, selected);
        }
    }

    public Player SelectedPlayer
    {
        get
        {
            if (Session == null)
                return null;

            var selected = Session.Player.Target;

            if (selected.IsEmpty)
                return Session.Player;

            return Global.ObjAccessor.FindConnectedPlayer(selected);
        }
    }

    public Player SelectedPlayerOrSelf
    {
        get
        {
            if (Session == null)
                return null;

            var selected = Session.Player.Target;

            if (selected.IsEmpty)
                return Session.Player;

            // first try with selected target
            var targetPlayer = Global.ObjAccessor.FindConnectedPlayer(selected);

            // if the target is not a player, then return self
            if (!targetPlayer)
                targetPlayer = Session.Player;

            return targetPlayer;
        }
    }

    public Unit SelectedUnit
    {
        get
        {
            if (Session == null)
                return null;

            var selected = Session.Player.SelectedUnit;

            if (selected)
                return selected;

            return Session.Player;
        }
    }
    public WorldSession Session { get; }

    public virtual Locale SessionDbcLocale => Session.SessionDbcLocale;

    public virtual byte SessionDbLocaleIndex => (byte)Session.SessionDbLocaleIndex;

    private GameObject NearbyGameObject
    {
        get
        {
            if (Session == null)
                return null;

            var pl = Session.Player;
            NearestGameObjectCheck check = new(pl);
            GameObjectLastSearcher searcher = new(pl, check, GridType.Grid);
            Cell.VisitGrid(pl, searcher, MapConst.SizeofGrids);

            return searcher.GetTarget();
        }
    }
    public bool _ParseCommands(string text)
    {
        if (ChatCommandNode.TryExecuteCommand(this, text))
            return true;

        // Pretend commands don't exist for regular players
        if (Session != null && !Session.HasPermission(RBACPermissions.CommandsNotifyCommandNotFoundError))
            return false;

        // Send error message for GMs
        SendSysMessage(CypherStrings.CmdInvalid, text);

        return true;
    }

    public string ExtractKeyFromLink(StringArguments args, params string[] linkType)
    {
        return ExtractKeyFromLink(args, linkType, out _);
    }

    public string ExtractKeyFromLink(StringArguments args, string[] linkType, out int found_idx)
    {
        return ExtractKeyFromLink(args, linkType, out found_idx, out _);
    }

    public string ExtractKeyFromLink(StringArguments args, string[] linkType, out int found_idx, out string something1)
    {
        found_idx = 0;
        something1 = null;

        // skip empty
        if (args.Empty())
            return null;

        // return non link case
        if (args[0] != '|')
            return args.NextString();

        if (args[1] == 'c')
        {
            var check = args.NextString("|");

            if (string.IsNullOrEmpty(check))
                return null;
        }
        else
        {
            args.NextChar();
        }

        var cLinkType = args.NextString(":");

        if (string.IsNullOrEmpty(cLinkType))
            return null;

        for (var i = 0; i < linkType.Length; ++i)
            if (cLinkType == linkType[i])
            {
                var cKey = args.NextString(":|"); // extract key

                something1 = args.NextString(":|"); // extract something

                args.NextString("]"); // restart scan tail and skip name with possible spaces
                args.NextString();    // skip link tail (to allow continue strtok(NULL, s) use after return from function
                found_idx = i;

                return cKey;
            }

        args.NextString();
        SendSysMessage(CypherStrings.WrongLinkType);

        return null;
    }

    public ulong ExtractLowGuidFromLink(StringArguments args, ref HighGuid guidHigh)
    {
        string[] guidKeys =
        {
            "Hplayer", "Hcreature", "Hgameobject"
        };

        // |color|Hcreature:creature_guid|h[name]|h|r
        // |color|Hgameobject:go_guid|h[name]|h|r
        // |color|Hplayer:name|h[name]|h|r
        var idS = ExtractKeyFromLink(args, guidKeys, out var type);

        if (string.IsNullOrEmpty(idS))
            return 0;

        switch (type)
        {
            case 0:
            {
                guidHigh = HighGuid.Player;

                if (!GameObjectManager.NormalizePlayerName(ref idS))
                    return 0;

                var player = Global.ObjAccessor.FindPlayerByName(idS);

                if (player)
                    return player.GUID.Counter;

                var guid = Global.CharacterCacheStorage.GetCharacterGuidByName(idS);

                if (guid.IsEmpty)
                    return 0;

                return guid.Counter;
            }
            case 1:
            {
                guidHigh = HighGuid.Creature;

                if (!ulong.TryParse(idS, out var lowguid))
                    return 0;

                return lowguid;
            }
            case 2:
            {
                guidHigh = HighGuid.GameObject;

                if (!ulong.TryParse(idS, out var lowguid))
                    return 0;

                return lowguid;
            }
        }

        // unknown type?
        return 0;
    }

    public bool ExtractPlayerTarget(StringArguments args, out Player player)
    {
        return ExtractPlayerTarget(args, out player, out _, out _);
    }

    public bool ExtractPlayerTarget(StringArguments args, out Player player, out ObjectGuid playerGuid)
    {
        return ExtractPlayerTarget(args, out player, out playerGuid, out _);
    }

    public bool ExtractPlayerTarget(StringArguments args, out Player player, out ObjectGuid playerGuid, out string playerName)
    {
        player = null;
        playerGuid = ObjectGuid.Empty;
        playerName = "";

        if (args != null && !args.Empty())
        {
            var name = ExtractPlayerNameFromLink(args);

            if (string.IsNullOrEmpty(name))
            {
                SendSysMessage(CypherStrings.PlayerNotFound);
                HasSentErrorMessage = true;

                return false;
            }

            player = Global.ObjAccessor.FindPlayerByName(name);
            var guid = player == null ? Global.CharacterCacheStorage.GetCharacterGuidByName(name) : ObjectGuid.Empty;

            playerGuid = player?.GUID ?? guid;
            playerName = player != null || !guid.IsEmpty ? name : "";
        }
        else
        {
            player = SelectedPlayer;
            playerGuid = player?.GUID ?? ObjectGuid.Empty;
            playerName = player != null ? player.GetName() : "";
        }

        if (player == null && playerGuid.IsEmpty && string.IsNullOrEmpty(playerName))
        {
            SendSysMessage(CypherStrings.PlayerNotFound);
            HasSentErrorMessage = true;

            return false;
        }

        return true;
    }

    public string ExtractQuotedArg(string str)
    {
        if (string.IsNullOrEmpty(str))
            return null;

        if (!str.Contains("\""))
            return str;

        return str.Replace("\"", string.Empty);
    }

    public uint ExtractSpellIdFromLink(StringArguments args)
    {
        // number or [name] Shift-click form |color|Henchant:recipe_spell_id|h[prof_name: recipe_name]|h|r
        // number or [name] Shift-click form |color|Hglyph:glyph_slot_id:glyph_prop_id|h[value]|h|r
        // number or [name] Shift-click form |color|Hspell:spell_id|h[name]|h|r
        // number or [name] Shift-click form |color|Htalent:talent_id, rank|h[name]|h|r
        // number or [name] Shift-click form |color|Htrade:spell_id, skill_id, max_value, cur_value|h[name]|h|r
        var idS = ExtractKeyFromLink(args, spellKeys, out var type, out var param1Str);

        if (string.IsNullOrEmpty(idS))
            return 0;

        if (!uint.TryParse(idS, out var id))
            return 0;

        switch (type)
        {
            case 0:
                return id;
            case 1:
            {
                // talent
                var talentEntry = CliDB.TalentStorage.LookupByKey(id);

                if (talentEntry == null)
                    return 0;

                return talentEntry.SpellID;
            }
            case 2:
            case 3:
                return id;
            case 4:
            {
                if (!uint.TryParse(param1Str, out var glyph_prop_id))
                    glyph_prop_id = 0;

                var glyphPropEntry = CliDB.GlyphPropertiesStorage.LookupByKey(glyph_prop_id);

                if (glyphPropEntry == null)
                    return 0;

                return glyphPropEntry.SpellID;
            }
        }

        // unknown type?
        return 0;
    }

    public Creature GetCreatureFromPlayerMapByDbGuid(ulong lowguid)
    {
        if (!Session)
            return null;

        // Select the first alive creature or a dead one if not found
        Creature creature = null;
        var bounds = Session.Player.Location.Map.CreatureBySpawnIdStore.LookupByKey(lowguid);

        foreach (var it in bounds)
        {
            creature = it;

            if (it.IsAlive)
                break;
        }

        return creature;
    }

    public string GetCypherString(CypherStrings str)
    {
        return Global.ObjectMgr.GetCypherString(str);
    }

    public string GetNameLink(Player obj)
    {
        return PlayerLink(obj.GetName());
    }

    public GameObject GetObjectFromPlayerMapByDbGuid(ulong lowguid)
    {
        if (Session == null)
            return null;

        var bounds = Session.Player.Location.Map.GameObjectBySpawnIdStore.LookupByKey(lowguid);

        if (!bounds.Empty())
            return Enumerable.First<GameObject>(bounds);

        return null;
    }

    public string GetParsedString(CypherStrings cypherString, params object[] args)
    {
        return string.Format(Global.ObjectMgr.GetCypherString(cypherString), args);
    }

    public bool GetPlayerGroupAndGUIDByName(string name, out Player player, out PlayerGroup group, out ObjectGuid guid, bool offline = false)
    {
        player = null;
        guid = ObjectGuid.Empty;
        group = null;

        if (!name.IsEmpty())
        {
            if (!GameObjectManager.NormalizePlayerName(ref name))
            {
                SendSysMessage(CypherStrings.PlayerNotFound);

                return false;
            }

            player = Global.ObjAccessor.FindPlayerByName(name);

            if (offline)
                guid = Global.CharacterCacheStorage.GetCharacterGuidByName(name);
        }

        if (player)
        {
            group = player.Group;

            if (guid.IsEmpty || !offline)
                guid = player.GUID;
        }
        else
        {
            if (SelectedPlayer)
                player = SelectedPlayer;
            else
                player = Session.Player;

            if (guid.IsEmpty || !offline)
                guid = player.GUID;

            group = player.Group;
        }

        return true;
    }

    public bool HasLowerSecurity(Player target, ObjectGuid guid, bool strong = false)
    {
        WorldSession target_session = null;
        uint target_account = 0;

        if (target != null)
            target_session = target.Session;
        else if (!guid.IsEmpty)
            target_account = Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(guid);

        if (target_session == null && target_account == 0)
        {
            SendSysMessage(CypherStrings.PlayerNotFound);
            HasSentErrorMessage = true;

            return true;
        }

        return HasLowerSecurityAccount(target_session, target_account, strong);
    }

    public bool HasLowerSecurityAccount(WorldSession target, uint target_account, bool strong = false)
    {
        AccountTypes target_ac_sec;

        // allow everything from console and RA console
        if (Session == null)
            return false;

        // ignore only for non-players for non strong checks (when allow apply command at least to same sec level)
        if (!Global.AccountMgr.IsPlayerAccount(Session.Security) && !strong && !GetDefaultValue("GM.LowerSecurity", false))
            return false;

        if (target != null)
            target_ac_sec = target.Security;
        else if (target_account != 0)
            target_ac_sec = Global.AccountMgr.GetSecurity(target_account, (int)Global.WorldMgr.RealmId.Index);
        else
            return true; // caller must report error for (target == NULL && target_account == 0)

        if (Session.Security < target_ac_sec || (strong && Session.Security <= target_ac_sec))
        {
            SendSysMessage(CypherStrings.YoursSecurityIsLow);
            HasSentErrorMessage = true;

            return true;
        }

        return false;
    }

    public virtual bool HasPermission(RBACPermissions permission)
    {
        return Session.HasPermission(permission);
    }

    public virtual bool IsAvailable(ChatCommandNode cmd)
    {
        return HasPermission(cmd.Permission.RequiredPermission);
    }

    public virtual bool IsHumanReadable()
    {
        return true;
    }

    public virtual bool NeedReportToTarget(Player chr)
    {
        var pl = Session.Player;

        return pl != chr && pl.IsVisibleGloballyFor(chr);
    }

    public virtual bool ParseCommands(string text)
    {
        if (text.IsEmpty())
            return false;

        // chat case (.command or !command format)
        if (text[0] != '!' && text[0] != '.')
            return false;

        /// ignore single . and ! in line
        if (text.Length < 2)
            return false;

        // ignore messages staring from many dots.
        if (text[1] == text[0])
            return false;

        if (text[1] == ' ')
            return false;

        return _ParseCommands(text.Substring(1));
    }
    public string PlayerLink(string name)
    {
        return Session != null ? "|cffffffff|Hplayer:" + name + "|h[" + name + "]|h|r" : name;
    }
    public void SendGlobalGMSysMessage(string str)
    {
        // Chat output
        ChatPkt data = new();
        data.Initialize(ChatMsg.System, Language.Universal, null, null, str);
        Global.WorldMgr.SendGlobalGMMessage(data);
    }

    public void SendGlobalSysMessage(string str)
    {
        // Chat output
        ChatPkt data = new();
        data.Initialize(ChatMsg.System, Language.Universal, null, null, str);
        Global.WorldMgr.SendGlobalMessage(data);
    }

    public void SendNotification(CypherStrings str, params object[] args)
    {
        Session.SendNotification(str, args);
    }

    public void SendSysMessage(string str, params object[] args)
    {
        SendSysMessage(string.Format(str, args));
    }

    public void SendSysMessage(CypherStrings cypherString, params object[] args)
    {
        SendSysMessage(string.Format(Global.ObjectMgr.GetCypherString(cypherString), args));
    }

    public virtual void SendSysMessage(string str, bool escapeCharacters = false)
    {
        HasSentErrorMessage = true;

        if (escapeCharacters)
            str.Replace("|", "||");

        ChatPkt messageChat = new();

        var lines = new StringArray(str, "\n", "\r");

        for (var i = 0; i < lines.Length; ++i)
        {
            messageChat.Initialize(ChatMsg.System, Language.Universal, null, null, lines[i]);
            Session.SendPacket(messageChat);
        }
    }
    public void SetSentErrorMessage(bool val)
    {
        HasSentErrorMessage = val;
    }

    private string ExtractPlayerNameFromLink(StringArguments args)
    {
        // |color|Hplayer:name|h[name]|h|r
        var name = ExtractKeyFromLink(args, "Hplayer");

        if (name.IsEmpty())
            return "";

        if (!GameObjectManager.NormalizePlayerName(ref name))
            return "";

        return name;
    }

    private bool HasStringAbbr(string name, string part)
    {
        // non "" command
        if (!name.IsEmpty())
        {
            // "" part from non-"" command
            if (part.IsEmpty())
                return false;

            var partIndex = 0;

            while (true)
            {
                if (partIndex >= part.Length || part[partIndex] == ' ')
                    return true;
                else if (partIndex >= name.Length)
                    return false;
                else if (char.ToLower(name[partIndex]) != char.ToLower(part[partIndex]))
                    return false;

                ++partIndex;
            }
        }
        // allow with any for ""

        return true;
    }
}
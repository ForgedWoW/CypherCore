// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Accounts;
using Forged.MapServer.Cache;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Groups;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Networking.Packets.Chat;
using Forged.MapServer.Server;
using Forged.MapServer.World;
using Framework.Collections;
using Framework.Constants;
using Framework.IO;
using Framework.Util;
using Game.Common;
using Microsoft.Extensions.Configuration;

namespace Forged.MapServer.Chat;

public class CommandHandler
{
    private static readonly string[] SpellKeys =
    {
        "Hspell",   // normal spell
        "Htalent",  // talent spell
        "Henchant", // enchanting recipe spell
        "Htrade",   // profession/skill spell
        "Hglyph",   // glyph
    };

    public CommandHandler(ClassFactory classFactory, WorldSession session = null)
    {
        ClassFactory = classFactory;
        Configuration = classFactory.Resolve<IConfiguration>();
        WorldManager = classFactory.Resolve<WorldManager>();
        AccountManager = classFactory.Resolve<AccountManager>();
        ObjectManager = classFactory.Resolve<GameObjectManager>();
        CliDB = classFactory.Resolve<CliDB>();
        CharacterCache = classFactory.Resolve<CharacterCache>();
        ObjectAccessor = classFactory.Resolve<ObjectAccessor>();
        CommandManager = classFactory.Resolve<CommandManager>();
        Session = session;
    }

    public AccountManager AccountManager { get; }
    public CharacterCache CharacterCache { get; }
    public ClassFactory ClassFactory { get; private set; }
    public CliDB CliDB { get; }
    public CommandManager CommandManager { get; }
    public IConfiguration Configuration { get; private set; }
    public bool HasSentErrorMessage { get; private set; }

    public bool IsConsole => Session == null;

    public virtual string NameLink => GetNameLink(Session.Player);

    public ObjectAccessor ObjectAccessor { get; }
    public GameObjectManager ObjectManager { get; }
    public Player Player => Session?.Player;

    public Creature SelectedCreature => Session == null ? null : ObjectAccessor.GetCreatureOrPetOrVehicle(Session.Player, Session.Player.Target);

    public WorldObject SelectedObject
    {
        get
        {
            if (Session == null)
                return null;

            var selected = Session.Player.Target;

            if (selected.IsEmpty)
                return NearbyGameObject;

            return ObjectAccessor.GetUnit(Session.Player, selected);
        }
    }

    public Player SelectedPlayer
    {
        get
        {
            if (Session == null)
                return null;

            var selected = Session.Player.Target;

            return selected.IsEmpty ? Session.Player : ObjectAccessor.FindConnectedPlayer(selected);
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
            // if the target is not a player, then return self
            var targetPlayer = ObjectAccessor.FindConnectedPlayer(selected) ?? Session.Player;

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

            return selected ?? Session.Player;
        }
    }

    public WorldSession Session { get; }
    public virtual Locale SessionDbcLocale => Session.SessionDbcLocale;
    public virtual byte SessionDbLocaleIndex => (byte)Session.SessionDbLocaleIndex;
    public WorldManager WorldManager { get; }

    private GameObject NearbyGameObject
    {
        get
        {
            if (Session == null)
                return null;

            var pl = Session.Player;
            NearestGameObjectCheck check = new(pl);
            GameObjectLastSearcher searcher = new(pl, check, GridType.Grid);
            Session.Player.CellCalculator.VisitGrid(pl, searcher, MapConst.SizeofGrids);

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

    public string ExtractKeyFromLink(StringArguments args, string[] linkType, out int foundIdx)
    {
        return ExtractKeyFromLink(args, linkType, out foundIdx, out _);
    }

    public string ExtractKeyFromLink(StringArguments args, string[] linkType, out int foundIdx, out string something1)
    {
        foundIdx = 0;
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
            args.NextChar();

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
                foundIdx = i;

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

                if (!Session.Player.GameObjectManager.NormalizePlayerName(ref idS))
                    return 0;

                var player = ObjectAccessor.FindPlayerByName(idS);

                if (player != null)
                    return player.GUID.Counter;

                var guid = CharacterCache.GetCharacterGuidByName(idS);

                return guid.IsEmpty ? 0 : guid.Counter;
            }
            case 1:
            {
                guidHigh = HighGuid.Creature;

                return !ulong.TryParse(idS, out var lowguid) ? 0 : lowguid;
            }
            case 2:
            {
                guidHigh = HighGuid.GameObject;

                return !ulong.TryParse(idS, out var lowguid) ? 0 : lowguid;
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

            player = ObjectAccessor.FindPlayerByName(name);
            var guid = player == null ? CharacterCache.GetCharacterGuidByName(name) : ObjectGuid.Empty;

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
        var idS = ExtractKeyFromLink(args, SpellKeys, out var type, out var param1Str);

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
                return !CliDB.TalentStorage.TryGetValue(id, out var talentEntry) ? 0 : talentEntry.SpellID;
            }
            case 2:
            case 3:
                return id;

            case 4:
            {
                if (!uint.TryParse(param1Str, out var glyphPropID))
                    glyphPropID = 0;

                return !CliDB.GlyphPropertiesStorage.TryGetValue(glyphPropID, out var glyphPropEntry) ? 0 : glyphPropEntry.SpellID;
            }
        }

        // unknown type?
        return 0;
    }

    public Creature GetCreatureFromPlayerMapByDbGuid(ulong lowguid)
    {
        if (Session == null)
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
        return ObjectManager.CypherStringCache.GetCypherString(str);
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

        return !bounds.Empty() ? bounds.First() : null;
    }

    public string GetParsedString(CypherStrings cypherString, params object[] args)
    {
        return string.Format(ObjectManager.CypherStringCache.GetCypherString(cypherString), args);
    }

    public bool GetPlayerGroupAndGUIDByName(string name, out Player player, out PlayerGroup group, out ObjectGuid guid, bool offline = false)
    {
        player = null;
        guid = ObjectGuid.Empty;
        group = null;

        if (!name.IsEmpty())
        {
            if (!Session.Player.GameObjectManager.NormalizePlayerName(ref name))
            {
                SendSysMessage(CypherStrings.PlayerNotFound);

                return false;
            }

            player = ObjectAccessor.FindPlayerByName(name);

            if (offline)
                guid = CharacterCache.GetCharacterGuidByName(name);
        }

        if (player != null)
        {
            group = player.Group;

            if (guid.IsEmpty || !offline)
                guid = player.GUID;
        }
        else
        {
            player = SelectedPlayer ?? Session.Player;

            if (guid.IsEmpty || !offline)
                guid = player.GUID;

            group = player.Group;
        }

        return true;
    }

    public bool HasLowerSecurity(Player target, ObjectGuid guid, bool strong = false)
    {
        WorldSession targetSession = null;
        uint targetAccount = 0;

        if (target != null)
            targetSession = target.Session;
        else if (!guid.IsEmpty)
            targetAccount = CharacterCache.GetCharacterAccountIdByGuid(guid);

        if (targetSession != null || targetAccount != 0)
            return HasLowerSecurityAccount(targetSession, targetAccount, strong);

        SendSysMessage(CypherStrings.PlayerNotFound);
        HasSentErrorMessage = true;

        return true;
    }

    public bool HasLowerSecurityAccount(WorldSession target, uint targetAccount, bool strong = false)
    {
        AccountTypes targetAcSec;

        // allow everything from console and RA console
        if (Session == null)
            return false;

        // ignore only for non-players for non strong checks (when allow apply command at least to same sec level)
        if (!AccountManager.IsPlayerAccount(Session.Security) && !strong && !Configuration.GetDefaultValue("GM:LowerSecurity", false))
            return false;

        if (target != null)
            targetAcSec = target.Security;
        else if (targetAccount != 0)
            targetAcSec = AccountManager.GetSecurity(targetAccount, (int)WorldManager.Realm.Id.Index);
        else
            return true; // caller must report error for (target == NULL && target_account == 0)

        if (Session.Security >= targetAcSec && (!strong || Session.Security > targetAcSec))
            return false;

        SendSysMessage(CypherStrings.YoursSecurityIsLow);
        HasSentErrorMessage = true;

        return true;
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

        // ignore single . and ! in line
        if (text.Length < 2)
            return false;

        // ignore messages staring from many dots.
        if (text[1] == text[0])
            return false;

        return text[1] != ' ' && _ParseCommands(text[1..]);
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
        WorldManager.SendGlobalGMMessage(data);
    }

    public void SendGlobalSysMessage(string str)
    {
        // Chat output
        ChatPkt data = new();
        data.Initialize(ChatMsg.System, Language.Universal, null, null, str);
        WorldManager.SendGlobalMessage(data);
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
        SendSysMessage(string.Format(ObjectManager.CypherStringCache.GetCypherString(cypherString), args));
    }

    public virtual void SendSysMessage(string str, bool escapeCharacters = false)
    {
        HasSentErrorMessage = true;

        if (escapeCharacters)
            str = str.Replace("|", "||");

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

        return !Session.Player.GameObjectManager.NormalizePlayerName(ref name) ? "" : name;
    }
}
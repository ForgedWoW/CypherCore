// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using System.Text;
using Forged.MapServer.Cache;
using Forged.MapServer.Chat;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Framework.Constants;
using Framework.Database;

namespace Forged.MapServer.SupportSystem;

public class Ticket
{
    protected ObjectGuid AssignedTo;
    protected ObjectGuid ClosedBy;
    protected ulong CreateTime;

    protected uint MapId;
    protected Vector3 Pos;
    public Ticket() { }

    public Ticket(Player player)
    {
        Player = player;
        CreateTime = (ulong)GameTime.CurrentTime;
        PlayerGuid = player.GUID;
        CharacterCache = Player.ClassFactory.Resolve<CharacterCache>();
    }

    public Player AssignedPlayer => Player.ObjectAccessor.FindConnectedPlayer(AssignedTo);
    public ObjectGuid AssignedToGUID => AssignedTo;

    public string AssignedToName
    {
        get
        {
            if (AssignedTo.IsEmpty)
                return "";

            return CharacterCache.GetCharacterNameByGuid(AssignedTo, out var name) ? name : "";
        }
    }

    public CharacterCache CharacterCache { get; }
    public string Comment { get; protected set; }
    public uint Id { get; protected set; }
    public bool IsAssigned => !AssignedTo.IsEmpty;
    public bool IsClosed => !ClosedBy.IsEmpty;
    public Player Player { get; }
    public ObjectGuid PlayerGuid { get; set; }

    public string PlayerName
    {
        get
        {
            var name = "";

            if (!PlayerGuid.IsEmpty)
                CharacterCache.GetCharacterNameByGuid(PlayerGuid, out name);

            return name;
        }
    }

    public virtual void DeleteFromDB() { }

    public virtual string FormatViewMessageString(CommandHandler handler, bool detailed = false)
    {
        return "";
    }

    public virtual string FormatViewMessageString(CommandHandler handler, string closedName, string assignedToName, string unassignedName, string deletedName)
    {
        StringBuilder ss = new();
        ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistguid, Id));
        ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistname, PlayerName));

        if (!string.IsNullOrEmpty(closedName))
            ss.Append(handler.GetParsedString(CypherStrings.CommandTicketclosed, closedName));

        if (!string.IsNullOrEmpty(assignedToName))
            ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistassignedto, assignedToName));

        if (!string.IsNullOrEmpty(unassignedName))
            ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistunassigned, unassignedName));

        if (!string.IsNullOrEmpty(deletedName))
            ss.Append(handler.GetParsedString(CypherStrings.CommandTicketdeleted, deletedName));

        return ss.ToString();
    }

    public bool IsAssignedNotTo(ObjectGuid guid)
    {
        return IsAssigned && !IsAssignedTo(guid);
    }

    public bool IsAssignedTo(ObjectGuid guid)
    {
        return guid == AssignedTo;
    }

    public virtual void LoadFromDB(SQLFields fields) { }

    public virtual void SaveToDB() { }

    public virtual void SetAssignedTo(ObjectGuid guid, bool isAdmin = false)
    {
        AssignedTo = guid;
    }

    public void SetClosedBy(ObjectGuid value)
    {
        ClosedBy = value;
    }

    public void SetComment(string comment)
    {
        Comment = comment;
    }

    public void SetPosition(uint mapId, Vector3 pos)
    {
        MapId = mapId;
        Pos = pos;
    }

    public virtual void SetUnassigned()
    {
        AssignedTo.Clear();
    }

    public void TeleportTo(Player player)
    {
        player.TeleportTo(MapId, Pos.X, Pos.Y, Pos.Z, 0.0f);
    }

    private bool IsFromPlayer(ObjectGuid guid)
    {
        return guid == PlayerGuid;
    }
}
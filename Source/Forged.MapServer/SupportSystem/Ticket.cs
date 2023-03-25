// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using System.Text;
using Forged.MapServer.Chat;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Time;
using Framework.Constants;
using Framework.Database;

namespace Forged.MapServer.SupportSystem;

public class Ticket
{
	protected uint IdProtected;
	protected ObjectGuid PlayerGuidProtected;
	protected uint MapIdProtected;
	protected Vector3 PosProtected;
	protected ulong CreateTimeProtected;
	protected ObjectGuid ClosedByProtected; // 0 = Open, -1 = Console, playerGuid = player abandoned ticket, other = GM who closed it.
	protected ObjectGuid AssignedToProtected;
	protected string CommentProtected;

	public bool IsClosed => !ClosedByProtected.IsEmpty;

	public bool IsAssigned => !AssignedToProtected.IsEmpty;

	public uint Id => IdProtected;

	public ObjectGuid PlayerGuid => PlayerGuidProtected;

	public Player Player => Global.ObjAccessor.FindConnectedPlayer(PlayerGuidProtected);

	public string PlayerName
	{
		get
		{
			var name = "";

			if (!PlayerGuidProtected.IsEmpty)
				Global.CharacterCacheStorage.GetCharacterNameByGuid(PlayerGuidProtected, out name);

			return name;
		}
	}

	public Player AssignedPlayer => Global.ObjAccessor.FindConnectedPlayer(AssignedToProtected);

	public ObjectGuid AssignedToGUID => AssignedToProtected;

	public string AssignedToName
	{
		get
		{
			if (!AssignedToProtected.IsEmpty)
				if (Global.CharacterCacheStorage.GetCharacterNameByGuid(AssignedToProtected, out var name))
					return name;

			return "";
		}
	}

	public string Comment => CommentProtected;

	public Ticket() { }

	public Ticket(Player player)
	{
		CreateTimeProtected = (ulong)GameTime.GetGameTime();
		PlayerGuidProtected = player.GUID;
	}

	public void TeleportTo(Player player)
	{
		player.TeleportTo(MapIdProtected, PosProtected.X, PosProtected.Y, PosProtected.Z, 0.0f, 0);
	}

	public virtual string FormatViewMessageString(CommandHandler handler, bool detailed = false)
	{
		return "";
	}

	public virtual string FormatViewMessageString(CommandHandler handler, string closedName, string assignedToName, string unassignedName, string deletedName)
	{
		StringBuilder ss = new();
		ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistguid, IdProtected));
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

	public bool IsAssignedTo(ObjectGuid guid)
	{
		return guid == AssignedToProtected;
	}

	public bool IsAssignedNotTo(ObjectGuid guid)
	{
		return IsAssigned && !IsAssignedTo(guid);
	}

	public virtual void SetAssignedTo(ObjectGuid guid, bool IsAdmin = false)
	{
		AssignedToProtected = guid;
	}

	public virtual void SetUnassigned()
	{
		AssignedToProtected.Clear();
	}

	public void SetClosedBy(ObjectGuid value)
	{
		ClosedByProtected = value;
	}

	public void SetComment(string comment)
	{
		CommentProtected = comment;
	}

	public void SetPosition(uint mapId, Vector3 pos)
	{
		MapIdProtected = mapId;
		PosProtected = pos;
	}

	public virtual void LoadFromDB(SQLFields fields) { }
	public virtual void SaveToDB() { }
	public virtual void DeleteFromDB() { }

	bool IsFromPlayer(ObjectGuid guid)
	{
		return guid == PlayerGuidProtected;
	}
}
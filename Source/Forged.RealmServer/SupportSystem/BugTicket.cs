// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using System.Text;
using Framework.Constants;
using Framework.Database;
using Forged.RealmServer.Chat;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Entities.Objects;
using Forged.RealmServer.Entities.Players;

namespace Forged.RealmServer.SupportSystem;

public class BugTicket : Ticket
{
	float _facing;
	string _note;

	public BugTicket()
	{
		_note = "";
	}

	public BugTicket(Player player) : base(player)
	{
		_note = "";
		IdProtected = Global.SupportMgr.GenerateBugId();
	}

	public override void LoadFromDB(SQLFields fields)
	{
		byte idx = 0;
		IdProtected = fields.Read<uint>(idx);
		PlayerGuidProtected = ObjectGuid.Create(HighGuid.Player, fields.Read<ulong>(++idx));
		_note = fields.Read<string>(++idx);
		CreateTimeProtected = fields.Read<ulong>(++idx);
		MapIdProtected = fields.Read<ushort>(++idx);
		PosProtected = new Vector3(fields.Read<float>(++idx), fields.Read<float>(++idx), fields.Read<float>(++idx));
		_facing = fields.Read<float>(++idx);

		var closedBy = fields.Read<long>(++idx);

		if (closedBy == 0)
			ClosedByProtected = ObjectGuid.Empty;
		else if (closedBy < 0)
			ClosedByProtected.SetRawValue(0, (ulong)closedBy);
		else
			ClosedByProtected = ObjectGuid.Create(HighGuid.Player, (ulong)closedBy);

		var assignedTo = fields.Read<ulong>(++idx);

		if (assignedTo == 0)
			AssignedToProtected = ObjectGuid.Empty;
		else
			AssignedToProtected = ObjectGuid.Create(HighGuid.Player, assignedTo);

		CommentProtected = fields.Read<string>(++idx);
	}

	public override void SaveToDB()
	{
		byte idx = 0;
		var stmt = _characterDatabase.GetPreparedStatement(CharStatements.REP_GM_BUG);
		stmt.AddValue(idx, IdProtected);
		stmt.AddValue(++idx, PlayerGuidProtected.Counter);
		stmt.AddValue(++idx, _note);
		stmt.AddValue(++idx, CreateTimeProtected);
		stmt.AddValue(++idx, MapIdProtected);
		stmt.AddValue(++idx, PosProtected.X);
		stmt.AddValue(++idx, PosProtected.Y);
		stmt.AddValue(++idx, PosProtected.Z);
		stmt.AddValue(++idx, _facing);
		stmt.AddValue(++idx, ClosedByProtected.Counter);
		stmt.AddValue(++idx, AssignedToProtected.Counter);
		stmt.AddValue(++idx, CommentProtected);

		_characterDatabase.Execute(stmt);
	}

	public override void DeleteFromDB()
	{
		var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GM_BUG);
		stmt.AddValue(0, IdProtected);
		_characterDatabase.Execute(stmt);
	}

	public override string FormatViewMessageString(CommandHandler handler, bool detailed = false)
	{
		var curTime = (ulong)_gameTime.GetGameTime;

		StringBuilder ss = new();
		ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistguid, IdProtected));
		ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistname, PlayerName));
		ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistagecreate, Time.secsToTimeString(curTime - CreateTimeProtected, TimeFormat.ShortText, false)));

		if (!AssignedToProtected.IsEmpty)
			ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistassignedto, AssignedToName));

		if (detailed)
		{
			ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistmessage, _note));

			if (!string.IsNullOrEmpty(CommentProtected))
				ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistcomment, CommentProtected));
		}

		return ss.ToString();
	}

	public void SetFacing(float facing)
	{
		_facing = facing;
	}

	public void SetNote(string note)
	{
		_note = note;
	}

	string GetNote()
	{
		return _note;
	}
}
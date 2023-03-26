// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Chat;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Server;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.SupportSystem;

public class SupportManager : Singleton<SupportManager>
{
    private readonly Dictionary<uint, BugTicket> _bugTicketList = new();
    private readonly Dictionary<uint, ComplaintTicket> _complaintTicketList = new();
    private readonly Dictionary<uint, SuggestionTicket> _suggestionTicketList = new();

    private bool _supportSystemStatus;
    private bool _ticketSystemStatus;
    private bool _bugSystemStatus;
    private bool _complaintSystemStatus;
    private bool _suggestionSystemStatus;
    private uint _lastBugId;
    private uint _lastComplaintId;
    private uint _lastSuggestionId;
    private uint _openBugTicketCount;
    private uint _openComplaintTicketCount;
    private uint _openSuggestionTicketCount;
    private ulong _lastChange;
    private SupportManager() { }

	public void Initialize()
	{
		SetSupportSystemStatus(GetDefaultValue("Support.Enabled", true));
		SetTicketSystemStatus(GetDefaultValue("Support.TicketsEnabled", false));
		SetBugSystemStatus(GetDefaultValue("Support.BugsEnabled", false));
		SetComplaintSystemStatus(GetDefaultValue("Support.ComplaintsEnabled", false));
		SetSuggestionSystemStatus(GetDefaultValue("Support.SuggestionsEnabled", false));
	}

	public T GetTicket<T>(uint Id) where T : Ticket
	{
		switch (typeof(T).Name)
		{
			case "BugTicket":
				return _bugTicketList.LookupByKey(Id) as T;
			case "ComplaintTicket":
				return _complaintTicketList.LookupByKey(Id) as T;
			case "SuggestionTicket":
				return _suggestionTicketList.LookupByKey(Id) as T;
		}

		return default;
	}

	public uint GetOpenTicketCount<T>() where T : Ticket
	{
		switch (typeof(T).Name)
		{
			case "BugTicket":
				return _openBugTicketCount;
			case "ComplaintTicket":
				return _openComplaintTicketCount;
			case "SuggestionTicket":
				return _openSuggestionTicketCount;
		}

		return 0;
	}

	public void LoadBugTickets()
	{
		var oldMSTime = Time.MSTime;
		_bugTicketList.Clear();

		_lastBugId = 0;
		_openBugTicketCount = 0;

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GM_BUGS);
		var result = DB.Characters.Query(stmt);

		if (result.IsEmpty())
		{
			Log.Logger.Information("Loaded 0 GM bugs. DB table `gm_bug` is empty!");

			return;
		}

		uint count = 0;

		do
		{
			BugTicket bug = new();
			bug.LoadFromDB(result.GetFields());

			if (!bug.IsClosed)
				++_openBugTicketCount;

			var id = bug.Id;

			if (_lastBugId < id)
				_lastBugId = id;

			_bugTicketList[id] = bug;
			++count;
		} while (result.NextRow());

		Log.Logger.Information("Loaded {0} GM bugs in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public void LoadComplaintTickets()
	{
		var oldMSTime = Time.MSTime;
		_complaintTicketList.Clear();

		_lastComplaintId = 0;
		_openComplaintTicketCount = 0;

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GM_COMPLAINTS);
		var result = DB.Characters.Query(stmt);

		if (result.IsEmpty())
		{
			Log.Logger.Information("Loaded 0 GM complaints. DB table `gm_complaint` is empty!");

			return;
		}

		uint count = 0;
		PreparedStatement chatLogStmt;
		SQLResult chatLogResult;

		do
		{
			ComplaintTicket complaint = new();
			complaint.LoadFromDB(result.GetFields());

			if (!complaint.IsClosed)
				++_openComplaintTicketCount;

			var id = complaint.Id;

			if (_lastComplaintId < id)
				_lastComplaintId = id;

			chatLogStmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GM_COMPLAINT_CHATLINES);
			chatLogStmt.AddValue(0, id);
			chatLogResult = DB.Characters.Query(stmt);

			if (!chatLogResult.IsEmpty())
				do
				{
					complaint.LoadChatLineFromDB(chatLogResult.GetFields());
				} while (chatLogResult.NextRow());

			_complaintTicketList[id] = complaint;
			++count;
		} while (result.NextRow());

		Log.Logger.Information("Loaded {0} GM complaints in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public void LoadSuggestionTickets()
	{
		var oldMSTime = Time.MSTime;
		_suggestionTicketList.Clear();

		_lastSuggestionId = 0;
		_openSuggestionTicketCount = 0;

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GM_SUGGESTIONS);
		var result = DB.Characters.Query(stmt);

		if (result.IsEmpty())
		{
			Log.Logger.Information("Loaded 0 GM suggestions. DB table `gm_suggestion` is empty!");

			return;
		}

		uint count = 0;

		do
		{
			SuggestionTicket suggestion = new();
			suggestion.LoadFromDB(result.GetFields());

			if (!suggestion.IsClosed)
				++_openSuggestionTicketCount;

			var id = suggestion.Id;

			if (_lastSuggestionId < id)
				_lastSuggestionId = id;

			_suggestionTicketList[id] = suggestion;
			++count;
		} while (result.NextRow());

		Log.Logger.Information("Loaded {0} GM suggestions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public void AddTicket<T>(T ticket) where T : Ticket
	{
		switch (typeof(T).Name)
		{
			case "BugTicket":
				_bugTicketList[ticket.Id] = ticket as BugTicket;

				if (!ticket.IsClosed)
					++_openBugTicketCount;

				break;
			case "ComplaintTicket":
				_complaintTicketList[ticket.Id] = ticket as ComplaintTicket;

				if (!ticket.IsClosed)
					++_openComplaintTicketCount;

				break;
			case "SuggestionTicket":
				_suggestionTicketList[ticket.Id] = ticket as SuggestionTicket;

				if (!ticket.IsClosed)
					++_openSuggestionTicketCount;

				break;
		}

		ticket.SaveToDB();
	}

	public void RemoveTicket<T>(uint ticketId) where T : Ticket
	{
		var ticket = GetTicket<T>(ticketId);

		if (ticket != null)
		{
			ticket.DeleteFromDB();

			switch (typeof(T).Name)
			{
				case "BugTicket":
					_bugTicketList.Remove(ticketId);

					break;
				case "ComplaintTicket":
					_complaintTicketList.Remove(ticketId);

					break;
				case "SuggestionTicket":
					_suggestionTicketList.Remove(ticketId);

					break;
			}
		}
	}

	public void CloseTicket<T>(uint ticketId, ObjectGuid closedBy) where T : Ticket
	{
		var ticket = GetTicket<T>(ticketId);

		if (ticket != null)
		{
			ticket.SetClosedBy(closedBy);

			if (!closedBy.IsEmpty)
				switch (typeof(T).Name)
				{
					case "BugTicket":
						--_openBugTicketCount;

						break;
					case "ComplaintTicket":
						--_openComplaintTicketCount;

						break;
					case "SuggestionTicket":
						--_openSuggestionTicketCount;

						break;
				}

			ticket.SaveToDB();
		}
	}

	public void ResetTickets<T>() where T : Ticket
	{
		PreparedStatement stmt;

		switch (typeof(T).Name)
		{
			case "BugTicket":
				_bugTicketList.Clear();

				_lastBugId = 0;

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ALL_GM_BUGS);
				DB.Characters.Execute(stmt);

				break;
			case "ComplaintTicket":
				_complaintTicketList.Clear();

				_lastComplaintId = 0;

				SQLTransaction trans = new();
				trans.Append(DB.Characters.GetPreparedStatement(CharStatements.DEL_ALL_GM_COMPLAINTS));
				trans.Append(DB.Characters.GetPreparedStatement(CharStatements.DEL_ALL_GM_COMPLAINT_CHATLOGS));
				DB.Characters.CommitTransaction(trans);

				break;
			case "SuggestionTicket":
				_suggestionTicketList.Clear();

				_lastSuggestionId = 0;

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ALL_GM_SUGGESTIONS);
				DB.Characters.Execute(stmt);

				break;
		}
	}

	public void ShowList<T>(CommandHandler handler) where T : Ticket
	{
		handler.SendSysMessage(CypherStrings.CommandTicketshowlist);

		switch (typeof(T).Name)
		{
			case "BugTicket":
				foreach (var ticket in _bugTicketList.Values)
					if (!ticket.IsClosed)
						handler.SendSysMessage(ticket.FormatViewMessageString(handler));

				break;
			case "ComplaintTicket":
				foreach (var ticket in _complaintTicketList.Values)
					if (!ticket.IsClosed)
						handler.SendSysMessage(ticket.FormatViewMessageString(handler));

				break;
			case "SuggestionTicket":
				foreach (var ticket in _suggestionTicketList.Values)
					if (!ticket.IsClosed)
						handler.SendSysMessage(ticket.FormatViewMessageString(handler));

				break;
		}
	}

	public void ShowClosedList<T>(CommandHandler handler) where T : Ticket
	{
		handler.SendSysMessage(CypherStrings.CommandTicketshowclosedlist);

		switch (typeof(T).Name)
		{
			case "BugTicket":
				foreach (var ticket in _bugTicketList.Values)
					if (ticket.IsClosed)
						handler.SendSysMessage(ticket.FormatViewMessageString(handler));

				break;
			case "ComplaintTicket":
				foreach (var ticket in _complaintTicketList.Values)
					if (ticket.IsClosed)
						handler.SendSysMessage(ticket.FormatViewMessageString(handler));

				break;
			case "SuggestionTicket":
				foreach (var ticket in _suggestionTicketList.Values)
					if (ticket.IsClosed)
						handler.SendSysMessage(ticket.FormatViewMessageString(handler));

				break;
		}
	}

	public bool GetSupportSystemStatus()
	{
		return _supportSystemStatus;
	}

	public bool GetTicketSystemStatus()
	{
		return _supportSystemStatus && _ticketSystemStatus;
	}

	public bool GetBugSystemStatus()
	{
		return _supportSystemStatus && _bugSystemStatus;
	}

	public bool GetComplaintSystemStatus()
	{
		return _supportSystemStatus && _complaintSystemStatus;
	}

	public bool GetSuggestionSystemStatus()
	{
		return _supportSystemStatus && _suggestionSystemStatus;
	}

	public ulong GetLastChange()
	{
		return _lastChange;
	}

	public void SetSupportSystemStatus(bool status)
	{
		_supportSystemStatus = status;
	}

	public void SetTicketSystemStatus(bool status)
	{
		_ticketSystemStatus = status;
	}

	public void SetBugSystemStatus(bool status)
	{
		_bugSystemStatus = status;
	}

	public void SetComplaintSystemStatus(bool status)
	{
		_complaintSystemStatus = status;
	}

	public void SetSuggestionSystemStatus(bool status)
	{
		_suggestionSystemStatus = status;
	}

	public void UpdateLastChange()
	{
		_lastChange = (ulong)GameTime.GetGameTime();
	}

	public uint GenerateBugId()
	{
		return ++_lastBugId;
	}

	public uint GenerateComplaintId()
	{
		return ++_lastComplaintId;
	}

	public uint GenerateSuggestionId()
	{
		return ++_lastSuggestionId;
	}

    private long GetAge(ulong t)
	{
		return (GameTime.GetGameTime() - (long)t) / Time.Day;
	}

    private IEnumerable<KeyValuePair<uint, ComplaintTicket>> GetComplaintsByPlayerGuid(ObjectGuid playerGuid)
	{
		return _complaintTicketList.Where(ticket => ticket.Value.PlayerGuid == playerGuid);
	}
}
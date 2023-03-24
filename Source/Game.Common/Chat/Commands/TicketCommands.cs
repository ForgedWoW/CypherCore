// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Common.Entities.Objects;
using Game.Common.Globals;
using Game.Common.Server;

namespace Game.Common.Chat.Commands;

[CommandGroup("ticket")]
class TicketCommands
{
	[Command("togglesystem", RBACPermissions.CommandTicketTogglesystem, true)]
	static bool HandleToggleGMTicketSystem(CommandHandler handler)
	{
		if (!WorldConfig.GetBoolValue(WorldCfg.SupportTicketsEnabled))
		{
			handler.SendSysMessage(CypherStrings.DisallowTicketsConfig);

			return true;
		}

		var status = !Global.SupportMgr.GetSupportSystemStatus();
		Global.SupportMgr.SetSupportSystemStatus(status);
		handler.SendSysMessage(status ? CypherStrings.AllowTickets : CypherStrings.DisallowTickets);

		return true;
	}

	static bool HandleTicketAssignToCommand<T>(CommandHandler handler, uint ticketId, string targetName) where T : Ticket
	{
		if (targetName.IsEmpty())
			return false;

		if (!ObjectManager.NormalizePlayerName(ref targetName))
			return false;

		var ticket = Global.SupportMgr.GetTicket<T>(ticketId);

		if (ticket == null || ticket.IsClosed)
		{
			handler.SendSysMessage(CypherStrings.CommandTicketnotexist);

			return true;
		}

		var targetGuid = Global.CharacterCacheStorage.GetCharacterGuidByName(targetName);
		var accountId = Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(targetGuid);

		// Target must exist and have administrative rights
		if (!Global.AccountMgr.HasPermission(accountId, RBACPermissions.CommandsBeAssignedTicket, Global.WorldMgr.Realm.Id.Index))
		{
			handler.SendSysMessage(CypherStrings.CommandTicketassignerrorA);

			return true;
		}

		// If already assigned, leave
		if (ticket.IsAssignedTo(targetGuid))
		{
			handler.SendSysMessage(CypherStrings.CommandTicketassignerrorB, ticket.Id);

			return true;
		}

		// If assigned to different player other than current, leave
		//! Console can override though
		var player = handler.Session != null ? handler.Session.Player : null;

		if (player && ticket.IsAssignedNotTo(player.GUID))
		{
			handler.SendSysMessage(CypherStrings.CommandTicketalreadyassigned, ticket.Id);

			return true;
		}

		// Assign ticket
		ticket.SetAssignedTo(targetGuid, Global.AccountMgr.IsAdminAccount(Global.AccountMgr.GetSecurity(accountId, (int)Global.WorldMgr.Realm.Id.Index)));
		ticket.SaveToDB();

		var msg = ticket.FormatViewMessageString(handler, null, targetName, null, null);
		handler.SendGlobalGMSysMessage(msg);

		return true;
	}

	static bool HandleCloseByIdCommand<T>(CommandHandler handler, uint ticketId) where T : Ticket
	{
		var ticket = Global.SupportMgr.GetTicket<T>(ticketId);

		if (ticket == null || ticket.IsClosed)
		{
			handler.SendSysMessage(CypherStrings.CommandTicketnotexist);

			return true;
		}

		// Ticket should be assigned to the player who tries to close it.
		// Console can override though
		var player = handler.Session != null ? handler.Session.Player : null;

		if (player && ticket.IsAssignedNotTo(player.GUID))
		{
			handler.SendSysMessage(CypherStrings.CommandTicketcannotclose, ticket.Id);

			return true;
		}

		var closedByGuid = ObjectGuid.Empty;

		if (player)
			closedByGuid = player.GUID;
		else
			closedByGuid.SetRawValue(0, ulong.MaxValue);

		Global.SupportMgr.CloseTicket<T>(ticket.Id, closedByGuid);

		var msg = ticket.FormatViewMessageString(handler, player ? player.GetName() : "Console", null, null, null);
		handler.SendGlobalGMSysMessage(msg);

		return true;
	}

	static bool HandleClosedListCommand<T>(CommandHandler handler) where T : Ticket
	{
		Global.SupportMgr.ShowClosedList<T>(handler);

		return true;
	}

	static bool HandleCommentCommand<T>(CommandHandler handler, uint ticketId, QuotedString comment) where T : Ticket
	{
		if (comment.IsEmpty())
			return false;

		var ticket = Global.SupportMgr.GetTicket<T>(ticketId);

		if (ticket == null || ticket.IsClosed)
		{
			handler.SendSysMessage(CypherStrings.CommandTicketnotexist);

			return true;
		}

		// Cannot comment ticket assigned to someone else
		//! Console excluded
		var player = handler.Session != null ? handler.Session.Player : null;

		if (player && ticket.IsAssignedNotTo(player.GUID))
		{
			handler.SendSysMessage(CypherStrings.CommandTicketalreadyassigned, ticket.Id);

			return true;
		}

		ticket.SetComment(comment);
		ticket.SaveToDB();
		Global.SupportMgr.UpdateLastChange();

		var msg = ticket.FormatViewMessageString(handler, null, ticket.AssignedToName, null, null);
		msg += string.Format(handler.GetCypherString(CypherStrings.CommandTicketlistaddcomment), player ? player.GetName() : "Console", comment);
		handler.SendGlobalGMSysMessage(msg);

		return true;
	}

	static bool HandleDeleteByIdCommand<T>(CommandHandler handler, uint ticketId) where T : Ticket
	{
		var ticket = Global.SupportMgr.GetTicket<T>(ticketId);

		if (ticket == null)
		{
			handler.SendSysMessage(CypherStrings.CommandTicketnotexist);

			return true;
		}

		if (!ticket.IsClosed)
		{
			handler.SendSysMessage(CypherStrings.CommandTicketclosefirst);

			return true;
		}

		var msg = ticket.FormatViewMessageString(handler, null, null, null, handler.Session != null ? handler.Session.Player.GetName() : "Console");
		handler.SendGlobalGMSysMessage(msg);

		Global.SupportMgr.RemoveTicket<T>(ticket.Id);

		return true;
	}

	static bool HandleListCommand<T>(CommandHandler handler) where T : Ticket
	{
		Global.SupportMgr.ShowList<T>(handler);

		return true;
	}

	static bool HandleResetCommand<T>(CommandHandler handler) where T : Ticket
	{
		if (Global.SupportMgr.GetOpenTicketCount<T>() != 0)
		{
			handler.SendSysMessage(CypherStrings.CommandTicketpending);

			return true;
		}
		else
		{
			Global.SupportMgr.ResetTickets<T>();
			handler.SendSysMessage(CypherStrings.CommandTicketreset);
		}

		return true;
	}

	static bool HandleUnAssignCommand<T>(CommandHandler handler, uint ticketId) where T : Ticket
	{
		var ticket = Global.SupportMgr.GetTicket<T>(ticketId);

		if (ticket == null || ticket.IsClosed)
		{
			handler.SendSysMessage(CypherStrings.CommandTicketnotexist);

			return true;
		}

		// Ticket must be assigned
		if (!ticket.IsAssigned)
		{
			handler.SendSysMessage(CypherStrings.CommandTicketnotassigned, ticket.Id);

			return true;
		}

		// Get security level of player, whom this ticket is assigned to
		AccountTypes security;
		var assignedPlayer = ticket.AssignedPlayer;

		if (assignedPlayer && assignedPlayer.IsInWorld)
		{
			security = assignedPlayer.Session.Security;
		}
		else
		{
			var guid = ticket.AssignedToGUID;
			var accountId = Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(guid);
			security = Global.AccountMgr.GetSecurity(accountId, (int)Global.WorldMgr.Realm.Id.Index);
		}

		// Check security
		//! If no m_session present it means we're issuing this command from the console
		var mySecurity = handler.Session != null ? handler.Session.Security : AccountTypes.Console;

		if (security > mySecurity)
		{
			handler.SendSysMessage(CypherStrings.CommandTicketunassignsecurity);

			return true;
		}

		var assignedTo = ticket.AssignedToName; // copy assignedto name because we need it after the ticket has been unnassigned

		ticket.SetUnassigned();
		ticket.SaveToDB();
		var msg = ticket.FormatViewMessageString(handler, null, assignedTo, handler.Session != null ? handler.Session.Player.GetName() : "Console", null);
		handler.SendGlobalGMSysMessage(msg);

		return true;
	}

	static bool HandleGetByIdCommand<T>(CommandHandler handler, uint ticketId) where T : Ticket
	{
		var ticket = Global.SupportMgr.GetTicket<T>(ticketId);

		if (ticket == null || ticket.IsClosed)
		{
			handler.SendSysMessage(CypherStrings.CommandTicketnotexist);

			return true;
		}

		handler.SendSysMessage(ticket.FormatViewMessageString(handler, true));

		return true;
	}

	[CommandGroup("bug")]
	class TicketBugCommands
	{
		[Command("assign", RBACPermissions.CommandTicketBugAssign, true)]
		static bool HandleTicketBugAssignCommand(CommandHandler handler, uint ticketId, string targetName)
		{
			return HandleTicketAssignToCommand<BugTicket>(handler, ticketId, targetName);
		}

		[Command("close", RBACPermissions.CommandTicketBugClose, true)]
		static bool HandleTicketBugCloseCommand(CommandHandler handler, uint ticketId)
		{
			return HandleCloseByIdCommand<BugTicket>(handler, ticketId);
		}

		[Command("closedlist", RBACPermissions.CommandTicketBugClosedlist, true)]
		static bool HandleTicketBugClosedListCommand(CommandHandler handler)
		{
			return HandleClosedListCommand<BugTicket>(handler);
		}

		[Command("comment", RBACPermissions.CommandTicketBugComment, true)]
		static bool HandleTicketBugCommentCommand(CommandHandler handler, uint ticketId, QuotedString comment)
		{
			return HandleCommentCommand<BugTicket>(handler, ticketId, comment);
		}

		[Command("delete", RBACPermissions.CommandTicketBugDelete, true)]
		static bool HandleTicketBugDeleteCommand(CommandHandler handler, uint ticketId)
		{
			return HandleDeleteByIdCommand<BugTicket>(handler, ticketId);
		}

		[Command("list", RBACPermissions.CommandTicketBugList, true)]
		static bool HandleTicketBugListCommand(CommandHandler handler)
		{
			return HandleListCommand<BugTicket>(handler);
		}

		[Command("unassign", RBACPermissions.CommandTicketBugUnassign, true)]
		static bool HandleTicketBugUnAssignCommand(CommandHandler handler, uint ticketId)
		{
			return HandleUnAssignCommand<BugTicket>(handler, ticketId);
		}

		[Command("view", RBACPermissions.CommandTicketBugView, true)]
		static bool HandleTicketBugViewCommand(CommandHandler handler, uint ticketId)
		{
			return HandleGetByIdCommand<BugTicket>(handler, ticketId);
		}
	}

	[CommandGroup("complaint")]
	class TicketComplaintCommands
	{
		[Command("assign", RBACPermissions.CommandTicketComplaintAssign, true)]
		static bool HandleTicketComplaintAssignCommand(CommandHandler handler, uint ticketId, string targetName)
		{
			return HandleTicketAssignToCommand<ComplaintTicket>(handler, ticketId, targetName);
		}

		[Command("close", RBACPermissions.CommandTicketComplaintClose, true)]
		static bool HandleTicketComplaintCloseCommand(CommandHandler handler, uint ticketId)
		{
			return HandleCloseByIdCommand<ComplaintTicket>(handler, ticketId);
		}

		[Command("closedlist", RBACPermissions.CommandTicketComplaintClosedlist, true)]
		static bool HandleTicketComplaintClosedListCommand(CommandHandler handler)
		{
			return HandleClosedListCommand<ComplaintTicket>(handler);
		}

		[Command("comment", RBACPermissions.CommandTicketComplaintComment, true)]
		static bool HandleTicketComplaintCommentCommand(CommandHandler handler, uint ticketId, QuotedString comment)
		{
			return HandleCommentCommand<ComplaintTicket>(handler, ticketId, comment);
		}

		[Command("delete", RBACPermissions.CommandTicketComplaintDelete, true)]
		static bool HandleTicketComplaintDeleteCommand(CommandHandler handler, uint ticketId)
		{
			return HandleDeleteByIdCommand<ComplaintTicket>(handler, ticketId);
		}

		[Command("list", RBACPermissions.CommandTicketComplaintList, true)]
		static bool HandleTicketComplaintListCommand(CommandHandler handler)
		{
			return HandleListCommand<ComplaintTicket>(handler);
		}

		[Command("unassign", RBACPermissions.CommandTicketComplaintUnassign, true)]
		static bool HandleTicketComplaintUnAssignCommand(CommandHandler handler, uint ticketId)
		{
			return HandleUnAssignCommand<ComplaintTicket>(handler, ticketId);
		}

		[Command("view", RBACPermissions.CommandTicketComplaintView, true)]
		static bool HandleTicketComplaintViewCommand(CommandHandler handler, uint ticketId)
		{
			return HandleGetByIdCommand<ComplaintTicket>(handler, ticketId);
		}
	}

	[CommandGroup("suggestion")]
	class TicketSuggestionCommands
	{
		[Command("assign", RBACPermissions.CommandTicketSuggestionAssign, true)]
		static bool HandleTicketSuggestionAssignCommand(CommandHandler handler, uint ticketId, string targetName)
		{
			return HandleTicketAssignToCommand<SuggestionTicket>(handler, ticketId, targetName);
		}

		[Command("close", RBACPermissions.CommandTicketSuggestionClose, true)]
		static bool HandleTicketSuggestionCloseCommand(CommandHandler handler, uint ticketId)
		{
			return HandleCloseByIdCommand<SuggestionTicket>(handler, ticketId);
		}

		[Command("closedlist", RBACPermissions.CommandTicketSuggestionClosedlist, true)]
		static bool HandleTicketSuggestionClosedListCommand(CommandHandler handler)
		{
			return HandleClosedListCommand<SuggestionTicket>(handler);
		}

		[Command("comment", RBACPermissions.CommandTicketSuggestionComment, true)]
		static bool HandleTicketSuggestionCommentCommand(CommandHandler handler, uint ticketId, QuotedString comment)
		{
			return HandleCommentCommand<SuggestionTicket>(handler, ticketId, comment);
		}

		[Command("delete", RBACPermissions.CommandTicketSuggestionDelete, true)]
		static bool HandleTicketSuggestionDeleteCommand(CommandHandler handler, uint ticketId)
		{
			return HandleDeleteByIdCommand<SuggestionTicket>(handler, ticketId);
		}

		[Command("list", RBACPermissions.CommandTicketSuggestionList, true)]
		static bool HandleTicketSuggestionListCommand(CommandHandler handler)
		{
			return HandleListCommand<SuggestionTicket>(handler);
		}

		[Command("unassign", RBACPermissions.CommandTicketSuggestionUnassign, true)]
		static bool HandleTicketSuggestionUnAssignCommand(CommandHandler handler, uint ticketId)
		{
			return HandleUnAssignCommand<SuggestionTicket>(handler, ticketId);
		}

		[Command("view", RBACPermissions.CommandTicketSuggestionView, true)]
		static bool HandleTicketSuggestionViewCommand(CommandHandler handler, uint ticketId)
		{
			return HandleGetByIdCommand<SuggestionTicket>(handler, ticketId);
		}
	}

	[CommandGroup("reset")]
	class TicketResetCommands
	{
		[Command("all", RBACPermissions.CommandTicketResetAll, true)]
		static bool HandleTicketResetAllCommand(CommandHandler handler)
		{
			if (Global.SupportMgr.GetOpenTicketCount<BugTicket>() != 0 || Global.SupportMgr.GetOpenTicketCount<ComplaintTicket>() != 0 || Global.SupportMgr.GetOpenTicketCount<SuggestionTicket>() != 0)
			{
				handler.SendSysMessage(CypherStrings.CommandTicketpending);

				return true;
			}
			else
			{
				Global.SupportMgr.ResetTickets<BugTicket>();
				Global.SupportMgr.ResetTickets<ComplaintTicket>();
				Global.SupportMgr.ResetTickets<SuggestionTicket>();
				handler.SendSysMessage(CypherStrings.CommandTicketreset);
			}

			return true;
		}

		[Command("bug", RBACPermissions.CommandTicketResetBug, true)]
		static bool HandleTicketResetBugCommand(CommandHandler handler)
		{
			return HandleResetCommand<BugTicket>(handler);
		}

		[Command("complaint", RBACPermissions.CommandTicketResetComplaint, true)]
		static bool HandleTicketResetComplaintCommand(CommandHandler handler)
		{
			return HandleResetCommand<ComplaintTicket>(handler);
		}

		[Command("suggestion", RBACPermissions.CommandTicketResetSuggestion, true)]
		static bool HandleTicketResetSuggestionCommand(CommandHandler handler)
		{
			return HandleResetCommand<SuggestionTicket>(handler);
		}
	}
}

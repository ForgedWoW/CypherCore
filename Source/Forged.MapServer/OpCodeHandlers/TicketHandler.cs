// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Ticket;
using Forged.MapServer.Server;
using Forged.MapServer.SupportSystem;
using Framework.Constants;
using Framework.Database;
using Game.Common.Handlers;

namespace Forged.MapServer.OpCodeHandlers;

public class TicketHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;
    private readonly SupportManager _supportManager;
    private readonly CharacterDatabase _characterDatabase;

    public TicketHandler(WorldSession session, SupportManager supportManager, CharacterDatabase characterDatabase)
    {
        _session = session;
		_supportManager = supportManager;
		_characterDatabase = characterDatabase;
    }

    [WorldPacketHandler(ClientOpcodes.GmTicketGetCaseStatus, Processing = PacketProcessing.Inplace)]
	void HandleGMTicketGetCaseStatus(GMTicketGetCaseStatus packet)
	{
		//TODO: Implement GmCase and handle this packet correctly
		GMTicketCaseStatus status = new();
		_session.SendPacket(status);
	}

	[WorldPacketHandler(ClientOpcodes.GmTicketGetSystemStatus, Processing = PacketProcessing.Inplace)]
	void HandleGMTicketSystemStatusOpcode(GMTicketGetSystemStatus packet)
	{
		// Note: This only disables the ticket UI at client side and is not fully reliable
		// Note: This disables the whole customer support UI after trying to send a ticket in disabled state (MessageBox: "GM Help Tickets are currently unavaiable."). UI remains disabled until the character relogs.
		GMTicketSystemStatusPkt response = new();
		response.Status = _supportManager.GetSupportSystemStatus() ? 1 : 0;
		_session.SendPacket(response);
	}

	[WorldPacketHandler(ClientOpcodes.SubmitUserFeedback)]
	void HandleSubmitUserFeedback(SubmitUserFeedback userFeedback)
	{
		if (userFeedback.IsSuggestion)
		{
			if (!_supportManager.GetSuggestionSystemStatus())
				return;

			SuggestionTicket ticket = new(_session.Player);
			ticket.SetPosition(userFeedback.Header.MapID, userFeedback.Header.Position);
			ticket.SetFacing(userFeedback.Header.Facing);
			ticket.SetNote(userFeedback.Note);

			_supportManager.AddTicket(ticket);
		}
		else
		{
			if (!_supportManager.GetBugSystemStatus())
				return;

			BugTicket ticket = new(_session.Player);
			ticket.SetPosition(userFeedback.Header.MapID, userFeedback.Header.Position);
			ticket.SetFacing(userFeedback.Header.Facing);
			ticket.SetNote(userFeedback.Note);

			_supportManager.AddTicket(ticket);
		}
	}

	[WorldPacketHandler(ClientOpcodes.SupportTicketSubmitComplaint)]
	void HandleSupportTicketSubmitComplaint(SupportTicketSubmitComplaint packet)
	{
		if (!_supportManager.GetComplaintSystemStatus())
			return;

		ComplaintTicket comp = new(_session.Player);
		comp.SetPosition(packet.Header.MapID, packet.Header.Position);
		comp.SetFacing(packet.Header.Facing);
		comp.SetChatLog(packet.ChatLog);
		comp.SetTargetCharacterGuid(packet.TargetCharacterGUID);
		comp.SetReportType((ReportType)packet.ReportType);
		comp.SetMajorCategory((ReportMajorCategory)packet.MajorCategory);
		comp.SetMinorCategoryFlags((ReportMinorCategory)packet.MinorCategoryFlags);
		comp.SetNote(packet.Note);

		_supportManager.AddTicket(comp);
	}

	[WorldPacketHandler(ClientOpcodes.BugReport)]
	void HandleBugReport(BugReport bugReport)
	{
		// Note: There is no way to trigger this with standard UI except /script ReportBug("text")
		if (!_supportManager.GetBugSystemStatus())
			return;

		var stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_BUG_REPORT);
		stmt.AddValue(0, bugReport.Text);
		stmt.AddValue(1, bugReport.DiagInfo);
        _characterDatabase.Execute(stmt);
	}

	[WorldPacketHandler(ClientOpcodes.Complaint)]
	void HandleComplaint(Complaint packet)
	{
		// NOTE: all chat messages from this spammer are automatically ignored by the spam reporter until logout in case of chat spam.
		// if it's mail spam - ALL mails from this spammer are automatically removed by client

		ComplaintResult result = new();
		result.ComplaintType = packet.ComplaintType;
		result.Result = 0;
		_session.SendPacket(result);
	}
}
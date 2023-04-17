// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using System.Text;
using Forged.MapServer.Chat;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking.Packets.Ticket;
using Framework.Constants;
using Framework.Database;

namespace Forged.MapServer.SupportSystem;

public class ComplaintTicket : Ticket
{
    private SupportTicketSubmitComplaint.SupportTicketChatLog _chatLog;
    private float _facing;
    private ReportMajorCategory _majorCategory;
    private ReportMinorCategory _minorCategoryFlags = ReportMinorCategory.TextChat;
    private string _note;
    private ReportType _reportType;
    private ObjectGuid _targetCharacterGuid;

    public ComplaintTicket()
    {
        _note = "";
    }

    public ComplaintTicket(Player player) : base(player)
    {
        _note = "";
        Id = Player.ClassFactory.Resolve<SupportManager>().GenerateComplaintId();
    }

    public override void DeleteFromDB()
    {
        var stmt = Player.CharacterDatabase.GetPreparedStatement(CharStatements.DEL_GM_COMPLAINT);
        stmt.AddValue(0, Id);
        Player.CharacterDatabase.Execute(stmt);

        stmt = Player.CharacterDatabase.GetPreparedStatement(CharStatements.DEL_GM_COMPLAINT_CHATLOG);
        stmt.AddValue(0, Id);
        Player.CharacterDatabase.Execute(stmt);
    }

    public override string FormatViewMessageString(CommandHandler handler, bool detailed = false)
    {
        var curTime = (ulong)GameTime.CurrentTime;

        StringBuilder ss = new();
        ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistguid, Id));
        ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistname, PlayerName));
        ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistagecreate, Time.SecsToTimeString(curTime - CreateTime, TimeFormat.ShortText)));

        if (!AssignedTo.IsEmpty)
            ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistassignedto, AssignedToName));

        if (detailed)
        {
            ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistmessage, _note));

            if (!string.IsNullOrEmpty(Comment))
                ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistcomment, Comment));
        }

        return ss.ToString();
    }

    public void LoadChatLineFromDB(SQLFields fields)
    {
        _chatLog.Lines.Add(new SupportTicketSubmitComplaint.SupportTicketChatLine(fields.Read<long>(0), fields.Read<string>(1)));
    }

    public override void LoadFromDB(SQLFields fields)
    {
        byte idx = 0;
        Id = fields.Read<uint>(idx);
        PlayerGuid = ObjectGuid.Create(HighGuid.Player, fields.Read<ulong>(++idx));
        _note = fields.Read<string>(++idx);
        CreateTime = fields.Read<ulong>(++idx);
        MapId = fields.Read<ushort>(++idx);
        Pos = new Vector3(fields.Read<float>(++idx), fields.Read<float>(++idx), fields.Read<float>(++idx));
        _facing = fields.Read<float>(++idx);
        _targetCharacterGuid = ObjectGuid.Create(HighGuid.Player, fields.Read<ulong>(++idx));
        _reportType = (ReportType)fields.Read<int>(++idx);
        _majorCategory = (ReportMajorCategory)fields.Read<int>(++idx);
        _minorCategoryFlags = (ReportMinorCategory)fields.Read<int>(++idx);
        var reportLineIndex = fields.Read<int>(++idx);

        if (reportLineIndex != -1)
            _chatLog.ReportLineIndex = (uint)reportLineIndex;

        var closedBy = fields.Read<long>(++idx);

        if (closedBy == 0)
            ClosedBy = ObjectGuid.Empty;
        else if (closedBy < 0)
            ClosedBy.SetRawValue(0, (ulong)closedBy);
        else
            ClosedBy = ObjectGuid.Create(HighGuid.Player, (ulong)closedBy);

        var assignedTo = fields.Read<ulong>(++idx);

        AssignedTo = assignedTo == 0 ? ObjectGuid.Empty : ObjectGuid.Create(HighGuid.Player, assignedTo);

        Comment = fields.Read<string>(++idx);
    }

    public override void SaveToDB()
    {
        var trans = new SQLTransaction();

        byte idx = 0;
        var stmt = Player.CharacterDatabase.GetPreparedStatement(CharStatements.REP_GM_COMPLAINT);
        stmt.AddValue(idx, Id);
        stmt.AddValue(++idx, PlayerGuid.Counter);
        stmt.AddValue(++idx, _note);
        stmt.AddValue(++idx, CreateTime);
        stmt.AddValue(++idx, MapId);
        stmt.AddValue(++idx, Pos.X);
        stmt.AddValue(++idx, Pos.Y);
        stmt.AddValue(++idx, Pos.Z);
        stmt.AddValue(++idx, _facing);
        stmt.AddValue(++idx, _targetCharacterGuid.Counter);
        stmt.AddValue(++idx, (int)_reportType);
        stmt.AddValue(++idx, (int)_majorCategory);
        stmt.AddValue(++idx, (int)_minorCategoryFlags);

        if (_chatLog.ReportLineIndex.HasValue)
            stmt.AddValue(++idx, _chatLog.ReportLineIndex.Value);
        else
            stmt.AddValue(++idx, -1); // empty ReportLineIndex

        stmt.AddValue(++idx, ClosedBy.Counter);
        stmt.AddValue(++idx, AssignedTo.Counter);
        stmt.AddValue(++idx, Comment);
        trans.Append(stmt);

        uint lineIndex = 0;

        foreach (var c in _chatLog.Lines)
        {
            idx = 0;
            stmt = Player.CharacterDatabase.GetPreparedStatement(CharStatements.INS_GM_COMPLAINT_CHATLINE);
            stmt.AddValue(idx, Id);
            stmt.AddValue(++idx, lineIndex);
            stmt.AddValue(++idx, c.Timestamp);
            stmt.AddValue(++idx, c.Text);

            trans.Append(stmt);
            ++lineIndex;
        }

        Player.CharacterDatabase.CommitTransaction(trans);
    }

    public void SetChatLog(SupportTicketSubmitComplaint.SupportTicketChatLog log)
    {
        _chatLog = log;
    }

    public void SetFacing(float facing)
    {
        _facing = facing;
    }

    public void SetMajorCategory(ReportMajorCategory majorCategory)
    {
        _majorCategory = majorCategory;
    }

    public void SetMinorCategoryFlags(ReportMinorCategory minorCategoryFlags)
    {
        _minorCategoryFlags = minorCategoryFlags;
    }

    public void SetNote(string note)
    {
        _note = note;
    }

    public void SetReportType(ReportType reportType)
    {
        _reportType = reportType;
    }

    public void SetTargetCharacterGuid(ObjectGuid targetCharacterGuid)
    {
        _targetCharacterGuid = targetCharacterGuid;
    }
}
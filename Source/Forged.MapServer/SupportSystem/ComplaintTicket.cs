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
    private float _facing;
    private ObjectGuid _targetCharacterGuid;
    private ReportType _reportType;
    private ReportMajorCategory _majorCategory;
    private ReportMinorCategory _minorCategoryFlags = ReportMinorCategory.TextChat;
    private SupportTicketSubmitComplaint.SupportTicketChatLog _chatLog;
    private string _note;

    public ComplaintTicket()
    {
        _note = "";
    }

    public ComplaintTicket(Player player) : base(player)
    {
        _note = "";
        IdProtected = Global.SupportMgr.GenerateComplaintId();
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
        _targetCharacterGuid = ObjectGuid.Create(HighGuid.Player, fields.Read<ulong>(++idx));
        _reportType = (ReportType)fields.Read<int>(++idx);
        _majorCategory = (ReportMajorCategory)fields.Read<int>(++idx);
        _minorCategoryFlags = (ReportMinorCategory)fields.Read<int>(++idx);
        var reportLineIndex = fields.Read<int>(++idx);

        if (reportLineIndex != -1)
            _chatLog.ReportLineIndex = (uint)reportLineIndex;

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

    public void LoadChatLineFromDB(SQLFields fields)
    {
        _chatLog.Lines.Add(new SupportTicketSubmitComplaint.SupportTicketChatLine(fields.Read<long>(0), fields.Read<string>(1)));
    }

    public override void SaveToDB()
    {
        var trans = new SQLTransaction();

        byte idx = 0;
        var stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_GM_COMPLAINT);
        stmt.AddValue(idx, IdProtected);
        stmt.AddValue(++idx, PlayerGuidProtected.Counter);
        stmt.AddValue(++idx, _note);
        stmt.AddValue(++idx, CreateTimeProtected);
        stmt.AddValue(++idx, MapIdProtected);
        stmt.AddValue(++idx, PosProtected.X);
        stmt.AddValue(++idx, PosProtected.Y);
        stmt.AddValue(++idx, PosProtected.Z);
        stmt.AddValue(++idx, _facing);
        stmt.AddValue(++idx, _targetCharacterGuid.Counter);
        stmt.AddValue(++idx, (int)_reportType);
        stmt.AddValue(++idx, (int)_majorCategory);
        stmt.AddValue(++idx, (int)_minorCategoryFlags);

        if (_chatLog.ReportLineIndex.HasValue)
            stmt.AddValue(++idx, _chatLog.ReportLineIndex.Value);
        else
            stmt.AddValue(++idx, -1); // empty ReportLineIndex

        stmt.AddValue(++idx, ClosedByProtected.Counter);
        stmt.AddValue(++idx, AssignedToProtected.Counter);
        stmt.AddValue(++idx, CommentProtected);
        trans.Append(stmt);

        uint lineIndex = 0;

        foreach (var c in _chatLog.Lines)
        {
            idx = 0;
            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GM_COMPLAINT_CHATLINE);
            stmt.AddValue(idx, IdProtected);
            stmt.AddValue(++idx, lineIndex);
            stmt.AddValue(++idx, c.Timestamp);
            stmt.AddValue(++idx, c.Text);

            trans.Append(stmt);
            ++lineIndex;
        }

        DB.Characters.CommitTransaction(trans);
    }

    public override void DeleteFromDB()
    {
        var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GM_COMPLAINT);
        stmt.AddValue(0, IdProtected);
        DB.Characters.Execute(stmt);

        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GM_COMPLAINT_CHATLOG);
        stmt.AddValue(0, IdProtected);
        DB.Characters.Execute(stmt);
    }

    public override string FormatViewMessageString(CommandHandler handler, bool detailed = false)
    {
        var curTime = (ulong)GameTime.GetGameTime();

        StringBuilder ss = new();
        ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistguid, IdProtected));
        ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistname, PlayerName));
        ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistagecreate, Time.secsToTimeString(curTime - CreateTimeProtected, TimeFormat.ShortText)));

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

    public void SetTargetCharacterGuid(ObjectGuid targetCharacterGuid)
    {
        _targetCharacterGuid = targetCharacterGuid;
    }

    public void SetReportType(ReportType reportType)
    {
        _reportType = reportType;
    }

    public void SetMajorCategory(ReportMajorCategory majorCategory)
    {
        _majorCategory = majorCategory;
    }

    public void SetMinorCategoryFlags(ReportMinorCategory minorCategoryFlags)
    {
        _minorCategoryFlags = minorCategoryFlags;
    }

    public void SetChatLog(SupportTicketSubmitComplaint.SupportTicketChatLog log)
    {
        _chatLog = log;
    }

    public void SetNote(string note)
    {
        _note = note;
    }

    private ObjectGuid GetTargetCharacterGuid()
    {
        return _targetCharacterGuid;
    }

    private ReportType GetReportType()
    {
        return _reportType;
    }

    private ReportMajorCategory GetMajorCategory()
    {
        return _majorCategory;
    }

    private ReportMinorCategory GetMinorCategoryFlags()
    {
        return _minorCategoryFlags;
    }

    private string GetNote()
    {
        return _note;
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using System.Text;
using Forged.MapServer.Chat;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Framework.Constants;
using Framework.Database;

namespace Forged.MapServer.SupportSystem;

public class SuggestionTicket : Ticket
{
    private float _facing;
    private string _note;

    public SuggestionTicket()
    {
        _note = "";
    }

    public SuggestionTicket(Player player) : base(player)
    {
        _note = "";
        IdProtected = Global.SupportMgr.GenerateSuggestionId();
    }

    public override void DeleteFromDB()
    {
        var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GM_SUGGESTION);
        stmt.AddValue(0, IdProtected);
        DB.Characters.Execute(stmt);
    }

    public override string FormatViewMessageString(CommandHandler handler, bool detailed = false)
    {
        var curTime = (ulong)GameTime.CurrentTime;

        StringBuilder ss = new();
        ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistguid, IdProtected));
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

    public override void LoadFromDB(SQLFields fields)
    {
        byte idx = 0;
        IdProtected = fields.Read<uint>(idx);
        PlayerGuidProtected = ObjectGuid.Create(HighGuid.Player, fields.Read<ulong>(++idx));
        _note = fields.Read<string>(++idx);
        CreateTime = fields.Read<ulong>(++idx);
        MapIdProtected = fields.Read<ushort>(++idx);
        Pos = new Vector3(fields.Read<float>(++idx), fields.Read<float>(++idx), fields.Read<float>(++idx));
        _facing = fields.Read<float>(++idx);

        var closedBy = fields.Read<long>(++idx);

        if (closedBy == 0)
            ClosedBy = ObjectGuid.Empty;
        else if (closedBy < 0)
            ClosedBy.SetRawValue(0, (ulong)closedBy);
        else
            ClosedBy = ObjectGuid.Create(HighGuid.Player, (ulong)closedBy);

        var assignedTo = fields.Read<ulong>(++idx);

        if (assignedTo == 0)
            AssignedTo = ObjectGuid.Empty;
        else
            AssignedTo = ObjectGuid.Create(HighGuid.Player, assignedTo);

        Comment = fields.Read<string>(++idx);
    }

    public override void SaveToDB()
    {
        byte idx = 0;
        var stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_GM_SUGGESTION);
        stmt.AddValue(idx, IdProtected);
        stmt.AddValue(++idx, PlayerGuidProtected.Counter);
        stmt.AddValue(++idx, _note);
        stmt.AddValue(++idx, CreateTime);
        stmt.AddValue(++idx, MapIdProtected);
        stmt.AddValue(++idx, Pos.X);
        stmt.AddValue(++idx, Pos.Y);
        stmt.AddValue(++idx, Pos.Z);
        stmt.AddValue(++idx, _facing);
        stmt.AddValue(++idx, ClosedBy.Counter);
        stmt.AddValue(++idx, AssignedTo.Counter);
        stmt.AddValue(++idx, Comment);

        DB.Characters.Execute(stmt);
    }

    public void SetFacing(float facing)
    {
        _facing = facing;
    }

    public void SetNote(string note)
    {
        _note = note;
    }

    private string GetNote()
    {
        return _note;
    }
}
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

public class BugTicket : Ticket
{
    private float _facing;
    private string _note;

    public BugTicket()
    {
        _note = "";
    }

    public BugTicket(Player player) : base(player)
    {
        _note = "";
        Id = Player.ClassFactory.Resolve<SupportManager>().GenerateBugId();
    }

    public override void DeleteFromDB()
    {
        var stmt = Player.CharacterDatabase.GetPreparedStatement(CharStatements.DEL_GM_BUG);
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

        if (!detailed)
            return ss.ToString();

        ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistmessage, _note));

        if (!string.IsNullOrEmpty(Comment))
            ss.Append(handler.GetParsedString(CypherStrings.CommandTicketlistcomment, Comment));

        return ss.ToString();
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
        byte idx = 0;
        var stmt = Player.CharacterDatabase.GetPreparedStatement(CharStatements.REP_GM_BUG);
        stmt.AddValue(idx, Id);
        stmt.AddValue(++idx, PlayerGuid.Counter);
        stmt.AddValue(++idx, _note);
        stmt.AddValue(++idx, CreateTime);
        stmt.AddValue(++idx, MapId);
        stmt.AddValue(++idx, Pos.X);
        stmt.AddValue(++idx, Pos.Y);
        stmt.AddValue(++idx, Pos.Z);
        stmt.AddValue(++idx, _facing);
        stmt.AddValue(++idx, ClosedBy.Counter);
        stmt.AddValue(++idx, AssignedTo.Counter);
        stmt.AddValue(++idx, Comment);

        Player.CharacterDatabase.Execute(stmt);
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
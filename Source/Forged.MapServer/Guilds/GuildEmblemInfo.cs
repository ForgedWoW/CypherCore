// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage;
using Forged.MapServer.Networking.Packets.Guild;
using Framework.Database;

namespace Forged.MapServer.Guilds;

public class GuildEmblemInfo
{
    private readonly CharacterDatabase _characterDatabase;
    private readonly CliDB _cliDB;

    public GuildEmblemInfo(CharacterDatabase characterDatabase, CliDB cliDB)
    {
        _characterDatabase = characterDatabase;
        _cliDB = cliDB;
    }

    public uint BackgroundColor { get; private set; }

    public uint BorderColor { get; private set; }

    public uint BorderStyle { get; private set; }

    public uint Color { get; private set; }

    public uint Style { get; private set; }
    public bool LoadFromDB(SQLFields field)
    {
        Style = field.Read<byte>(3);
        Color = field.Read<byte>(4);
        BorderStyle = field.Read<byte>(5);
        BorderColor = field.Read<byte>(6);
        BackgroundColor = field.Read<byte>(7);

        return ValidateEmblemColors();
    }

    public void ReadPacket(SaveGuildEmblem packet)
    {
        Style = packet.EStyle;
        Color = packet.EColor;
        BorderStyle = packet.BStyle;
        BorderColor = packet.BColor;
        BackgroundColor = packet.Bg;
    }

    public void SaveToDB(ulong guildId)
    {
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_EMBLEM_INFO);
        stmt.AddValue(0, Style);
        stmt.AddValue(1, Color);
        stmt.AddValue(2, BorderStyle);
        stmt.AddValue(3, BorderColor);
        stmt.AddValue(4, BackgroundColor);
        stmt.AddValue(5, guildId);
        _characterDatabase.Execute(stmt);
    }

    public bool ValidateEmblemColors()
    {
        return _cliDB.GuildColorBackgroundStorage.ContainsKey(BackgroundColor) &&
               _cliDB.GuildColorBorderStorage.ContainsKey(BorderColor) &&
               _cliDB.GuildColorEmblemStorage.ContainsKey(Color);
    }
}
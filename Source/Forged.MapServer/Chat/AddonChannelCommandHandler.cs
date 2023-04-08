// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Text;
using Forged.MapServer.Networking.Packets.Chat;
using Forged.MapServer.Server;
using Framework.Constants;
using Game.Common;

namespace Forged.MapServer.Chat;

internal class AddonChannelCommandHandler : CommandHandler
{
    public const string PREFIX = "ForgedCore";

    private string _echo;
    private bool _hadAck;
    private bool _humanReadable;

    public AddonChannelCommandHandler(ClassFactory classFactory, WorldSession session) : base(classFactory, session) { }

    public override bool IsHumanReadable()
    {
        return _humanReadable;
    }

    public override bool ParseCommands(string str)
    {
        if (str.Length < 5)
            return false;

        var opcode = str[0];
        _echo = str.Substring(1);

        switch (opcode)
        {
            case 'p': // p Ping
                SendAck();

                return true;
            case 'h': // h Issue human-readable command
            case 'i': // i Issue command
                if (str.Length < 6)
                    return false;

                _humanReadable = opcode == 'h';
                var cmd = str.Substring(5);

                if (_ParseCommands(cmd)) // actual command starts at str[5]
                {
                    if (!_hadAck)
                        SendAck();

                    if (HasSentErrorMessage)
                        SendFailed();
                    else
                        SendOK();
                }
                else
                {
                    SendSysMessage(CypherStrings.CmdInvalid, cmd);
                    SendFailed();
                }

                return true;
            default:
                return false;
        }
    }

    public override void SendSysMessage(string str, bool escapeCharacters = false)
    {
        if (!_hadAck)
            SendAck();

        StringBuilder msg = new("m");
        msg.Append(_echo, 0, 4);

        if (escapeCharacters)
            str = str.Replace("|", "||");

        int pos, lastpos;

        for (lastpos = 0, pos = str.IndexOf('\n', lastpos); pos != -1; lastpos = pos + 1, pos = str.IndexOf('\n', lastpos))
        {
            var line = msg;
            line.Append(str, lastpos, pos - lastpos);
            Send(line.ToString());
        }

        msg.Append(str, lastpos, pos - lastpos);
        Send(msg.ToString());
    }
    private void Send(string msg)
    {
        ChatPkt chat = new();
        chat.Initialize(ChatMsg.Whisper, Language.Addon, Session.Player, Session.Player, msg, 0, "", Locale.enUS, PREFIX);
        Session.SendPacket(chat);
    }

    private void SendAck() // a Command acknowledged, no body
    {
        Send($"a{_echo:4}\0");
        _hadAck = true;
    }

    private void SendFailed() // f Command failed, no body
    {
        Send($"f{_echo:4}\0");
    }

    private void SendOK() // o Command OK, no body
    {
        Send($"o{_echo:4}\0");
    }
}
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Text;
using Framework.Constants;
using Game.Common.Networking.Packets.Chat;
using Game.Common.Server;

namespace Game.Common.Chat;

class AddonChannelCommandHandler : CommandHandler
{
	public static string PREFIX = "ForgedCore";

	string _echo;
	bool _hadAck;
	bool _humanReadable;

	public AddonChannelCommandHandler(WorldSession session) : base(session) { }

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

	public override void SendSysMessage(string str, bool escapeCharacters)
	{
		if (!_hadAck)
			SendAck();

		StringBuilder msg = new("m");
		msg.Append(_echo, 0, 4);
		var body = str;

		if (escapeCharacters)
			body.Replace("|", "||");

		int pos, lastpos;

		for (lastpos = 0, pos = body.IndexOf('\n', lastpos); pos != -1; lastpos = pos + 1, pos = body.IndexOf('\n', lastpos))
		{
			var line = msg;
			line.Append(body, lastpos, pos - lastpos);
			Send(line.ToString());
		}

		msg.Append(body, lastpos, pos - lastpos);
		Send(msg.ToString());
	}

	public override bool IsHumanReadable()
	{
		return _humanReadable;
	}

	void Send(string msg)
	{
		ChatPkt chat = new();
		chat.Initialize(ChatMsg.Whisper, Language.Addon, Session.Player, Session.Player, msg, 0, "", Locale.enUS, PREFIX);
		Session.SendPacket(chat);
	}

	void SendAck() // a Command acknowledged, no body
	{
		Send($"a{_echo:4}\0");
		_hadAck = true;
	}

	void SendOK() // o Command OK, no body
	{
		Send($"o{_echo:4}\0");
	}

	void SendFailed() // f Command failed, no body
	{
		Send($"f{_echo:4}\0");
	}
}

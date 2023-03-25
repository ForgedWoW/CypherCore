// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Chat;

class Hyperlink
{
	public static ChatCommandResult TryParse(out dynamic value, Type type, CommandHandler handler, string arg)
	{
		value = default;

		var info = ParseHyperlink(arg);

		// invalid hyperlinks cannot be consumed
		if (info == null)
			return default;

		var errorResult = ChatCommandResult.FromErrorMessage(handler.GetCypherString(CypherStrings.CmdparserLinkdataInvalid));

		// store value
		switch (Type.GetTypeCode(type))
		{
			case TypeCode.UInt32:
			{
				if (!uint.TryParse(info.Data, out var tempValue))
					return errorResult;

				value = tempValue;

				break;
			}
			case TypeCode.UInt64:
			{
				if (!ulong.TryParse(info.Data, out var tempValue))
					return errorResult;

				value = tempValue;

				break;
			}
			case TypeCode.String:
			{
				value = info.Data;

				break;
			}
			default:
				return errorResult;
		}

		// finally, skip any potential delimiters
		var (token, next) = info.Tail.Tokenize();

		if (token.IsEmpty()) /* empty token = first character is delimiter, skip past it */
			return new ChatCommandResult(next);
		else
			return new ChatCommandResult(info.Tail);
	}

	public static bool CheckAllLinks(string str)
	{
		// Step 1: Disallow all control sequences except ||, |H, |h, |c and |r
		{
			var pos = 0;

			while ((pos = str.IndexOf('|', pos)) != -1)
			{
				var next = str[pos + 1];

				if (next == 'H' || next == 'h' || next == 'c' || next == 'r' || next == '|')
					pos += 2;
				else
					return false;
			}
		}

		// Step 2: Parse all link sequences
		// They look like this: |c<color>|H<linktag>:<linkdata>|h[<linktext>]|h|r
		// - <color> is 8 hex characters AARRGGBB
		// - <linktag> is arbitrary length [a-z_]
		// - <linkdata> is arbitrary length, no | contained
		// - <linktext> is printable
		{
			var pos = 0;

			while ((pos = str.IndexOf('|', pos)) != -1)
			{
				if (str[pos + 1] == '|') // this is an escaped pipe character (||)
				{
					pos += 2;

					continue;
				}

				var info = ParseHyperlink(str.Substring(pos));

				if (info == null) // todo fix me || !ValidateLinkInfo(info))
					return false;

				// tag is fine, find the next one
				pos = str.Length - info.Tail.Length;
			}
		}

		// all tags are valid
		return true;
	}

	//|color|Henchant:recipe_spell_id|h[prof_name: recipe_name]|h|r
	public static HyperlinkInfo ParseHyperlink(string currentString)
	{
		if (currentString.IsEmpty())
			return null;

		var pos = 0;

		//color tag
		if (currentString[pos++] != '|' || currentString[pos++] != 'c')
			return null;

		uint color = 0;

		for (byte i = 0; i < 8; ++i)
		{
			var hex = toHex(currentString[pos++]);

			if (hex != 0)
				color = (uint)((int)(color << 4) | (hex & 0xf));
			else
				return null;
		}

		// link data start tag
		if (currentString[pos++] != '|' || currentString[pos++] != 'H')
			return null;

		// link tag, find next : or |
		var tagStart = pos;
		var tagLength = 0;

		while (pos < currentString.Length && currentString[pos] != '|' && currentString[pos++] != ':') // we only advance pointer to one past if the last thing is : (not for |), this is intentional!
			++tagLength;

		// ok, link data, skip to next |
		var dataStart = pos;
		var dataLength = 0;

		while (pos < currentString.Length && currentString[pos++] != '|')
			++dataLength;

		// ok, next should be link data end tag...
		if (currentString[pos++] != 'h')
			return null;

		// then visible link text, starts with [
		if (currentString[pos++] != '[')
			return null;

		// skip until we hit the next ], abort on unexpected |
		var textStart = pos;
		var textLength = 0;

		while (pos < currentString.Length)
		{
			if (currentString[pos] == '|')
				return null;

			if (currentString[pos++] == ']')
				break;

			++textLength;
		}

		// link end tag
		if (currentString[pos++] != '|' || currentString[pos++] != 'h' || currentString[pos++] != '|' || currentString[pos++] != 'r')
			return null;

		// ok, valid hyperlink, return info
		return new HyperlinkInfo(currentString.Substring(pos), color, currentString.Substring(tagStart, tagLength), currentString.Substring(dataStart, dataLength), currentString.Substring(textStart, textLength));
	}

	static byte toHex(char c)
	{
		return (byte)((c >= '0' && c <= '9') ? c - '0' + 0x10 : (c >= 'a' && c <= 'f') ? c - 'a' + 0x1a : 0x00);
	}
}
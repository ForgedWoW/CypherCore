﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Spells;

namespace Game.Chat;

class CommandArgs
{
	public static ChatCommandResult ConsumeFromOffset(dynamic[] tuple, int offset, ParameterInfo[] parameterInfos, CommandHandler handler, string args)
	{
		if (offset < tuple.Length)
			return TryConsumeTo(tuple, offset, parameterInfos, handler, args);
		else if (!args.IsEmpty()) /* the entire string must be consumed */
			return default;
		else
			return new ChatCommandResult(args);
	}

	public static ChatCommandResult TryConsume(out dynamic val, Type type, CommandHandler handler, string args)
	{
		val = default;

		var hyperlinkResult = Hyperlink.TryParse(out val, type, handler, args);

		if (hyperlinkResult.IsSuccessful)
			return hyperlinkResult;

		if (type.IsEnum)
			type = type.GetEnumUnderlyingType();

		var (token, tail) = args.Tokenize();

		switch (Type.GetTypeCode(type))
		{
			case TypeCode.SByte:
			{
				if (token.IsEmpty())
					return default;

				if (sbyte.TryParse(token, out var tempValue))
					val = tempValue;
				else
					return ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserStringValueInvalid, token, Type.GetTypeCode(type)));

				return new ChatCommandResult(tail);
			}
			case TypeCode.Int16:
			{
				if (token.IsEmpty())
					return default;

				if (short.TryParse(token, out var tempValue))
					val = tempValue;
				else
					return ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserStringValueInvalid, token, Type.GetTypeCode(type)));

				return new ChatCommandResult(tail);
			}
			case TypeCode.Int32:
			{
				if (token.IsEmpty())
					return default;

				if (int.TryParse(token, out var tempValue))
					val = tempValue;
				else
					return ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserStringValueInvalid, token, Type.GetTypeCode(type)));

				return new ChatCommandResult(tail);
			}
			case TypeCode.Int64:
			{
				if (token.IsEmpty())
					return default;

				if (long.TryParse(token, out var tempValue))
					val = tempValue;
				else
					return ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserStringValueInvalid, token, Type.GetTypeCode(type)));

				return new ChatCommandResult(tail);
			}
			case TypeCode.Byte:
			{
				if (token.IsEmpty())
					return default;

				if (byte.TryParse(token, out var tempValue))
					val = tempValue;
				else
					return ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserStringValueInvalid, token, Type.GetTypeCode(type)));

				return new ChatCommandResult(tail);
			}
			case TypeCode.UInt16:
			{
				if (token.IsEmpty())
					return default;

				if (ushort.TryParse(token, out var tempValue))
					val = tempValue;
				else
					return ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserStringValueInvalid, token, Type.GetTypeCode(type)));

				return new ChatCommandResult(tail);
			}
			case TypeCode.UInt32:
			{
				if (token.IsEmpty())
					return default;

				if (uint.TryParse(token, out var tempValue))
					val = tempValue;
				else
					return ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserStringValueInvalid, token, Type.GetTypeCode(type)));

				return new ChatCommandResult(tail);
			}
			case TypeCode.UInt64:
			{
				if (token.IsEmpty())
					return default;

				if (ulong.TryParse(token, out var tempValue))
					val = tempValue;
				else
					return ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserStringValueInvalid, token, Type.GetTypeCode(type)));

				return new ChatCommandResult(tail);
			}
			case TypeCode.Single:
			{
				if (token.IsEmpty())
					return default;

				if (float.TryParse(token, out var tempValue))
					val = tempValue;
				else
					return ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserStringValueInvalid, token, Type.GetTypeCode(type)));

				if (!float.IsFinite(val))
					return ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserStringValueInvalid, token, Type.GetTypeCode(type)));

				return new ChatCommandResult(tail);
			}
			case TypeCode.String:
			{
				if (token.IsEmpty())
					return default;

				val = token;

				return new ChatCommandResult(tail);
			}
			case TypeCode.Boolean:
			{
				if (token.IsEmpty())
					return default;

				if (bool.TryParse(token, out var tempValue))
				{
					val = tempValue;
				}
				else
				{
					if ((token == "1") || token.Equals("y", StringComparison.OrdinalIgnoreCase) || token.Equals("on", StringComparison.OrdinalIgnoreCase) || token.Equals("yes", StringComparison.OrdinalIgnoreCase))
						val = true;
					else if ((token == "0") || token.Equals("n", StringComparison.OrdinalIgnoreCase) || token.Equals("off", StringComparison.OrdinalIgnoreCase) || token.Equals("no", StringComparison.OrdinalIgnoreCase))
						val = false;
					else
						return ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserStringValueInvalid, token, Type.GetTypeCode(type)));
				}

				return new ChatCommandResult(tail);
			}
			case TypeCode.Object:
			{
				switch (type.Name)
				{
					case nameof(Tail):
						val = new Tail();

						return val.TryConsume(handler, args);
					case nameof(QuotedString):
						val = new QuotedString();

						return val.TryConsume(handler, args);
					case nameof(PlayerIdentifier):
						val = new PlayerIdentifier();

						return val.TryConsume(handler, args);
					case nameof(AccountIdentifier):
						val = new AccountIdentifier();

						return val.TryConsume(handler, args);
					case nameof(AchievementRecord):
					{
						var result = TryConsume(out var tempVal, typeof(uint), handler, args);

						if (!result.IsSuccessful || (val = CliDB.AchievementStorage.LookupByKey((uint)tempVal)) != null)
							return result;

						return ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserAchievementNoExist, tempVal));
					}
					case nameof(CurrencyTypesRecord):
					{
						var result = TryConsume(out var tempVal, typeof(uint), handler, args);

						if (!result.IsSuccessful || (val = CliDB.CurrencyTypesStorage.LookupByKey((uint)tempVal)) != null)
							return result;

						return ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserCurrencyNoExist, tempVal));
					}
					case nameof(GameTele):
					{
						var result = TryConsume(out var tempVal, typeof(uint), handler, args);

						if (!result.IsSuccessful)
							result = TryConsume(out tempVal, typeof(string), handler, args);

						if (!result.IsSuccessful || (val = Global.ObjectMgr.GetGameTele(tempVal)) != null)
							return result;

						if (tempVal is uint)
							return ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserGameTeleIdNoExist, tempVal));
						else
							return ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserGameTeleNoExist, tempVal));
					}
					case nameof(ItemTemplate):
					{
						var result = TryConsume(out var tempVal, typeof(uint), handler, args);

						if (!result.IsSuccessful || (val = Global.ObjectMgr.GetItemTemplate(tempVal)) != null)
							return result;

						return ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserItemNoExist, tempVal));
					}
					case nameof(SpellInfo):
					{
						var result = TryConsume(out var tempVal, typeof(uint), handler, args);

						if (!result.IsSuccessful || (val = Global.SpellMgr.GetSpellInfo(tempVal, Difficulty.None)) != null)
							return result;

						return ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserSpellNoExist, tempVal));
					}
					case nameof(Quest):
					{
						var result = TryConsume(out var tempVal, typeof(uint), handler, args);

						if (!result.IsSuccessful || (val = Global.ObjectMgr.GetQuestTemplate(tempVal)) != null)
							return result;

						return ChatCommandResult.FromErrorMessage(handler.GetParsedString(CypherStrings.CmdparserQuestNoExist, tempVal));
					}
				}

				break;
			}
		}

		return default;
	}

	public static ChatCommandResult TryConsumeVariant(out dynamic val, Type[] types, CommandHandler handler, string args)
	{
		var result = TryAtIndex(out val, types, 0, handler, args);

		if (result.HasErrorMessage && (result.ErrorMessage.IndexOf('\n') != -1))
			return ChatCommandResult.FromErrorMessage($"{handler.GetCypherString(CypherStrings.CmdparserEither)} {result.ErrorMessage}");

		return result;
	}

	static ChatCommandResult TryConsumeTo(dynamic[] tuple, int offset, ParameterInfo[] parameterInfos, CommandHandler handler, string args)
	{
		var optionalArgAttribute = parameterInfos[offset].GetCustomAttribute<OptionalArgAttribute>(true);

		if (optionalArgAttribute != null || parameterInfos[offset].ParameterType.IsGenericType && parameterInfos[offset].ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
		{
			// try with the argument
			var myArg = Nullable.GetUnderlyingType(parameterInfos[offset].ParameterType) ?? parameterInfos[offset].ParameterType;

			var result1 = TryConsume(out tuple[offset], myArg, handler, args);

			if (result1.IsSuccessful)
				if ((result1 = ConsumeFromOffset(tuple, offset + 1, parameterInfos, handler, result1)).IsSuccessful)
					return result1;

			// try again omitting the argument
			tuple[offset] = default;
			var result2 = ConsumeFromOffset(tuple, offset + 1, parameterInfos, handler, args);

			if (result2.IsSuccessful)
				return result2;

			if (result1.HasErrorMessage && result2.HasErrorMessage)
				return ChatCommandResult.FromErrorMessage($"{handler.GetCypherString(CypherStrings.CmdparserEither)} \"{result2.ErrorMessage}\"\n{handler.GetCypherString(CypherStrings.CmdparserOr)} \"{result1.ErrorMessage}\"");
			else if (result1.HasErrorMessage)
				return result1;
			else
				return result2;
		}
		else
		{
			ChatCommandResult next;

			var variantArgAttribute = parameterInfos[offset].GetCustomAttribute<VariantArgAttribute>(true);

			if (variantArgAttribute != null)
				next = TryConsumeVariant(out tuple[offset], variantArgAttribute.Types, handler, args);
			else
				next = TryConsume(out tuple[offset], parameterInfos[offset].ParameterType, handler, args);

			if (next.IsSuccessful)
				return ConsumeFromOffset(tuple, offset + 1, parameterInfos, handler, next);
			else
				return next;
		}
	}

	static ChatCommandResult TryAtIndex(out dynamic val, Type[] types, int index, CommandHandler handler, string args)
	{
		val = default;

		if (index < types.Length)
		{
			var thisResult = TryConsume(out val, types[index], handler, args);

			if (thisResult.IsSuccessful)
			{
				return thisResult;
			}
			else
			{
				var nestedResult = TryAtIndex(out val, types, index + 1, handler, args);

				if (nestedResult.IsSuccessful || !thisResult.HasErrorMessage)
					return nestedResult;

				if (!nestedResult.HasErrorMessage)
					return thisResult;

				if (nestedResult.ErrorMessage.StartsWith("\""))
					return ChatCommandResult.FromErrorMessage($"\"{thisResult.ErrorMessage}\"\n{handler.GetCypherString(CypherStrings.CmdparserOr)} {nestedResult.ErrorMessage}");
				else
					return ChatCommandResult.FromErrorMessage($"\"{thisResult.ErrorMessage}\"\n{handler.GetCypherString(CypherStrings.CmdparserOr)} \"{nestedResult.ErrorMessage}\"");
			}
		}
		else
		{
			return default;
		}
	}
}
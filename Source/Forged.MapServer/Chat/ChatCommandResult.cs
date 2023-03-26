// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Forged.MapServer.Chat;

public struct ChatCommandResult
{
    private bool _result;
    private readonly dynamic _value;
    private string _errorMessage;

	public ChatCommandResult(string _value = "")
	{
		_result = true;
		this._value = _value;
		_errorMessage = null;
	}

	public bool IsSuccessful => _result;

	public bool HasErrorMessage => !_errorMessage.IsEmpty();

	public string ErrorMessage => _errorMessage;

	public void SetErrorMessage(string _value)
	{
		_result = false;
		_errorMessage = _value;
	}

	public static ChatCommandResult FromErrorMessage(string str)
	{
		var result = new ChatCommandResult();
		result.SetErrorMessage(str);

		return result;
	}

	public static implicit operator string(ChatCommandResult stringResult)
	{
		return stringResult._value;
	}
}
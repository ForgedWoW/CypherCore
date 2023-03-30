// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Forged.MapServer.Chat;

public struct ChatCommandResult
{
    private readonly dynamic _value;

    public ChatCommandResult(string _value = "")
    {
        IsSuccessful = true;
        this._value = _value;
        ErrorMessage = null;
    }

    public bool IsSuccessful { get; private set; }

    public bool HasErrorMessage => !ErrorMessage.IsEmpty();

    public string ErrorMessage { get; private set; }

    public void SetErrorMessage(string _value)
    {
        IsSuccessful = false;
        ErrorMessage = _value;
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
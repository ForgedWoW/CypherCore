// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Linq.Expressions;
using System.Reflection;
using Framework.Constants;
using Forged.RealmServer.Networking.Packets;
using Google.Protobuf;

namespace Forged.RealmServer.Services;

public class WorldServiceHandler
{
	readonly Delegate _methodCaller;
	readonly Type _requestType;
	readonly Type _responseType;

	public WorldServiceHandler(MethodInfo info, ParameterInfo[] parameters)
	{
		_requestType = parameters[0].ParameterType;

		if (parameters.Length > 1)
			_responseType = parameters[1].ParameterType;

		if (_responseType != null)
			_methodCaller = info.CreateDelegate(Expression.GetDelegateType(new[]
			{
				typeof(WorldSession), _requestType, _responseType, info.ReturnType
			}));
		else
			_methodCaller = info.CreateDelegate(Expression.GetDelegateType(new[]
			{
				typeof(WorldSession), _requestType, info.ReturnType
			}));
	}

	public void Invoke(WorldSession session, MethodCall methodCall, CodedInputStream stream)
	{
		var request = (IMessage)Activator.CreateInstance(_requestType);
		request.MergeFrom(stream);

		BattlenetRpcErrorCode status;

		if (_responseType != null)
		{
			var response = (IMessage)Activator.CreateInstance(_responseType);
			status = (BattlenetRpcErrorCode)_methodCaller.DynamicInvoke(session, request, response);
			Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server Method: {1}) Returned: {2} Status: {3}.", session.RemoteAddress, request, response, status);

			if (status == 0)
				session.SendBattlenetResponse(methodCall.GetServiceHash(), methodCall.GetMethodId(), methodCall.Token, response);
			else
				session.SendBattlenetResponse(methodCall.GetServiceHash(), methodCall.GetMethodId(), methodCall.Token, status);
		}
		else
		{
			status = (BattlenetRpcErrorCode)_methodCaller.DynamicInvoke(session, request);
			Log.outDebug(LogFilter.ServiceProtobuf, "{0} Client called server Method: {1}) Status: {2}.", session.RemoteAddress, request, status);

			if (status != 0)
				session.SendBattlenetResponse(methodCall.GetServiceHash(), methodCall.GetMethodId(), methodCall.Token, status);
		}
	}
}
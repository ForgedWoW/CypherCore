// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.IO;
using System.Text;

class FileAppender : Appender, IDisposable
{
	readonly string _fileName;
	readonly string _logDir;
	readonly bool _dynamicName;
	readonly FileStream _logStream;
	readonly object locker = new();

	public FileAppender(byte id, string name, LogLevel level, string fileName, string logDir, AppenderFlags flags) : base(id, name, level, flags)
	{
		Directory.CreateDirectory(logDir);
		_fileName = fileName;
		_logDir = logDir;
		_dynamicName = _fileName.Contains("{0}");

		if (_dynamicName)
		{
			Directory.CreateDirectory(logDir + "/" + _fileName.Substring(0, _fileName.IndexOf('/') + 1));

			return;
		}

		_logStream = OpenFile(_fileName, FileMode.Create);
	}

	public override void _write(LogMessage message)
	{
		lock (locker)
		{
			var logBytes = Encoding.UTF8.GetBytes(message.prefix + message.text + "\r\n");

			if (_dynamicName)
			{
				var logStream = OpenFile(string.Format(_fileName, message.dynamicName), FileMode.Append);
				logStream.Write(logBytes, 0, logBytes.Length);
				logStream.Flush();
				logStream.Close();

				return;
			}

			_logStream.Write(logBytes, 0, logBytes.Length);
			_logStream.Flush();
		}
	}

	public override AppenderType GetAppenderType()
	{
		return AppenderType.File;
	}

	FileStream OpenFile(string filename, FileMode mode)
	{
		return new FileStream(_logDir + "/" + filename, mode, FileAccess.Write, FileShare.ReadWrite);
	}

	#region IDisposable Support

	private bool disposedValue;

	private void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing)
				_logStream.Dispose();

			disposedValue = true;
		}
	}

	public void Dispose()
	{
		Dispose(true);
	}

	#endregion
}
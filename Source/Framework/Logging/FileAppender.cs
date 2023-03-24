// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Framework.Util;
using Microsoft.Extensions.Configuration;

class FileAppender : Appender, IDisposable
{
    const string ONE_DOT_LOG = ".1.log";
    const string DOT_STAR_DOT_LOG = ".*.log";
    const string DOT_LOG = ".log";
    const int LOGGER_TRY = 1000;
    readonly long _maxLogSize;
    readonly string _logFile;
    readonly string _logName;
    readonly string _logDir;
    private readonly IConfiguration _configuration;
    readonly bool _dynamicName;
    FileStream _logStream;
	readonly object _locker = new();
    static readonly char[] _dot = new char[] { '.' };

    public FileAppender(byte id, string name, LogLevel level, string fileName, string logDir, AppenderFlags flags, IConfiguration configuration) : base(id, name, level, flags)
	{
		Directory.CreateDirectory(logDir);
		_logFile = fileName;
        _logName = fileName.Replace(DOT_LOG, string.Empty);
        _logDir = logDir;
        _configuration = configuration;
        _dynamicName = _logFile.Contains("{0}");
        _maxLogSize = (1024L * 1024) * _configuration.GetDefaultValue("MaxLogSize", 50);
        RotateLogs(true);

		if (_dynamicName)
		{
			Directory.CreateDirectory(logDir + "/" + _logFile.Substring(0, _logFile.IndexOf('/') + 1));

			return;
		}
	}

	public override void _write(LogMessage message)
	{
		lock (_locker)
		{
			var logBytes = Encoding.UTF8.GetBytes(message.prefix + message.text + "\r\n");

			if (_dynamicName)
			{
				var logStream = OpenFile(string.Format(_logFile, message.dynamicName), FileMode.Append);
				logStream.Write(logBytes, 0, logBytes.Length);
				logStream.Flush();
				logStream.Close();

				return;
			}

			_logStream.Write(logBytes, 0, logBytes.Length);
			_logStream.Flush();

            RotateLogs();
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

    private void RotateLogs(bool force = false)
    {
        var currentLog = _logDir + "/" + _logFile;
        if (!File.Exists(currentLog))
        {
            _logStream = OpenFile(_logFile, FileMode.Create);
            return;
        }

        foreach (var fi in new DirectoryInfo(_logDir).GetFiles().OrderByDescending(x => x.LastWriteTime).Skip(_configuration.GetDefaultValue("MaxNumberLogRotations", 20)))
            fi.Delete();

        FileInfo fix = new FileInfo(currentLog);
        if (fix.Length >= _maxLogSize || force)
        {
            if (_logStream != null)
            {
                _logStream.Close();
                _logStream.Dispose();
            }

            string oldFilename = _logDir + "/" + _logName + ONE_DOT_LOG;

            if (File.Exists(oldFilename))
            {
                Rotate(_logDir);
            }

            int j = 0;

            while (!LoggerCheck(currentLog, oldFilename, FileAction.Move))
            {
                j++;
                if (j > LOGGER_TRY) break;
            }

            _logStream = OpenFile(_logFile, FileMode.Create);
        }
    }

    private void Rotate(string logFileDir)
    {
        string[] raw = Directory.GetFiles(logFileDir, _logName + DOT_STAR_DOT_LOG);
        ArrayList files = new ArrayList();
        files.AddRange(raw);

        Comparer<string> myComparer = new LogFileSorter();
        files.Sort(myComparer);
        files.Reverse();

        foreach (string f in files)
        {
            int logfnum = GetNumFromFilename(f);
            if (logfnum > 0)
            {
                string newname = string.Format("{0}/{1}.{2}.log", _logDir, _logName, logfnum + 1);

                if (logfnum >= 5)
                {
                    int j = 0;

                    while (!LoggerCheck(f, string.Empty, FileAction.Delete))
                    {
                        j++;
                        if (j > LOGGER_TRY) break;
                    }
                }
                else
                {
                    int j = 0;

                    while (!LoggerCheck(f, newname, FileAction.Move))
                    {
                        j++;
                        if (j > LOGGER_TRY) break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Return the log number for a file in the format xxxx.9999.log
    /// If there is an error return 0
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    public static int GetNumFromFilename(string filename)
    {
        int filenum = 0;
        if (!string.IsNullOrEmpty(filename))
        {
            string[] xtokens = filename.Split(_dot);
            int xtokencount = xtokens.GetLength(0);

            if (xtokencount > 2)
            {
                int xval = 0;
                if (Int32.TryParse(xtokens[xtokencount - 2], out xval))
                {
                    filenum = xval;
                }
            }
        }
        return filenum;
    }

    private bool LoggerCheck(string f, string oldFilename, FileAction action)
    {
        if (File.Exists(f))
        {
            if (action == FileAction.Move)
            {
                try
                {
                    if (File.Exists(oldFilename))
                        File.Delete(oldFilename);
                    File.Move(f, oldFilename);
                    return true;
                }
                catch { }

            }
            else if (action == FileAction.Delete)
            {
                try
                {
                    File.Delete(f);
                    return true;
                }
                catch { }
            }
        }
        return false;
    }

    #region IDisposable Support

    private bool _disposedValue;

	private void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
				_logStream.Dispose();

			_disposedValue = true;
		}
	}

	public void Dispose()
	{
		Dispose(true);
	}

    #endregion

    /// <summary>
    /// Look for file in the format xxxx.99999.log
    /// The number is extracted from both filenames and this number is used for the comparison
    /// </summary>
    public class LogFileSorter : Comparer<string>
    {
        public override int Compare(string x, string y)
        {

            if (string.IsNullOrEmpty(x))
            {
                if (string.IsNullOrEmpty(y))
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(y))
                {
                    return 1;
                }
                else
                {
                    int xval = FileAppender.GetNumFromFilename(x);
                    int yval = FileAppender.GetNumFromFilename(y);
                    return ((new CaseInsensitiveComparer()).Compare(xval, yval));
                }
            }
        }
    }

    /// <summary>
    /// Actions that apply to system files.
    /// </summary>
    public enum FileAction : int
    {
        /// <summary>
        /// A command to move a file.
        /// </summary>
        Move = 0,
        /// <summary>
        /// A command to delete a file.
        /// </summary>
        Delete = 1
    }

}
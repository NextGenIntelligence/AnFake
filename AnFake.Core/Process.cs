﻿using System;
using AnFake.Api;
using Common.Logging;

namespace AnFake.Core
{
	public static class Process
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof (Process).FullName);

		public sealed class Params
		{
			public FileSystemPath FileName;

			public string Arguments;

			public FileSystemPath WorkingDirectory;

			public TimeSpan Timeout;

			public ILog Logger;

			internal Params()
			{
				WorkingDirectory = "".AsPath();
				Timeout = TimeSpan.MaxValue;
				Logger = Log;
			}

			public Params Clone()
			{
				return (Params)MemberwiseClone();
			}
		}

		public static Params Defaults { get; private set; }

		static Process()
		{
			Defaults = new Params();
		}

		public static ProcessExecutionResult Run(Action<Params> setParams)
		{
			if (setParams == null)
				throw new ArgumentNullException("setParams", "Process.Run(setParams): setParams must not be null");

			var parameters = Defaults.Clone();
			setParams(parameters);

			if (parameters.FileName == null)
				throw new ArgumentException("Process.Params.FileName: must not be null");
			if (parameters.WorkingDirectory == null)
				throw new ArgumentException("Process.Params.WorkingDirectory: must not be null");
			if (parameters.Logger == null)
				throw new ArgumentException("Process.Params.Logger: must not be null");

			var process = new System.Diagnostics.Process
			{
				StartInfo =
				{
					FileName = parameters.FileName.Full,
					WorkingDirectory = parameters.WorkingDirectory.Full,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true
				}
			};

			if (!String.IsNullOrEmpty(parameters.Arguments))
			{
				process.StartInfo.Arguments = parameters.Arguments;
			}

			Log.DebugFormat("Starting process...\n  Executable: {0}\n  Arguments: {1}\n  WorkingDirectory: {2}", 
				process.StartInfo.FileName, process.StartInfo.Arguments, process.StartInfo.WorkingDirectory);

			process.OutputDataReceived += (sender, evt) => { if (!String.IsNullOrWhiteSpace(evt.Data)) parameters.Logger.Debug(evt.Data); };
			process.ErrorDataReceived += (sender, evt) => { if (!String.IsNullOrWhiteSpace(evt.Data)) parameters.Logger.Error(evt.Data); };

			IToolExecutionResult external;
			Tracer.MessageReceived += OnTraceMessage;
			Tracer.StartTrackExternal();			
			try
			{
				process.Start();

				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

				if (parameters.Timeout == TimeSpan.MaxValue)
				{
					process.WaitForExit();
				}
				else if (!process.WaitForExit((int) parameters.Timeout.TotalMilliseconds))
				{
					process.Kill();
					throw new TimeoutException(String.Format("Process isn't completed in specified time.\n  Executable: {0}\n  Timeout: {1}", process.StartInfo.FileName, parameters.Timeout));
				}
			}
			finally
			{
				external = Tracer.StopTrackExternal();
				Tracer.MessageReceived -= OnTraceMessage;
			}

			Log.DebugFormat("Process finished. ExitCode = {0} Errors = {1} Warnings = {2} Time = {3}", 
				process.ExitCode, external.ErrorsCount, external.WarningsCount, process.ExitTime - process.StartTime);

			return new ProcessExecutionResult(process.ExitCode, external.ErrorsCount, external.WarningsCount);
		}		

		public static ArgumentsBuilder Args(string optionMarker, string nameValueMarker)
		{
			return new ArgumentsBuilder(optionMarker, nameValueMarker);
		}

		private static void OnTraceMessage(object sender, TraceMessage message)
		{
			Logger.TraceMessage(message);
		}
	}
}
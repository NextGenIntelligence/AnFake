﻿using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using AnFake.Api;

namespace AnFake.Core
{
	/// <summary>
	///		Represents NuGet package manager tool.
	/// </summary>
	/// <seealso cref="http://docs.nuget.org/docs/reference/command-line-reference"/>
	public static class NuGet
	{
		private static readonly string[] Locations =
		{
			".nuget/NuGet.exe",
			"tools/NuGet/NuGet.exe",
			"packages/NuGet.CommandLine.*/tools/NuGet.exe",
			"[AnFake]/NuGet.exe"
		};

		/// <summary>
		///		NuGet parameters.
		/// </summary>
		public sealed class Params
		{
			/// <summary>
			///		Version of package to be installed.
			/// </summary>
			public string Version;

			/// <summary>
			///		Output folder for installed package. Default: 'packages'.
			/// </summary>
			/// <remarks>
			///		Can be set via command line as "NuGet.OutputDirectory=&lt;value&gt;" or settings file as "NuGet.OutputDirectory": "&lt;value&gt;".
			/// </remarks>
			public FileSystemPath OutputDirectory;

			/// <summary>
			///		Specifies the solution directory. Used by <c>Restore</c> command.
			/// </summary>
			public FileSystemPath SolutionDirectory;

			/// <summary>
			///		Whether to include referenced projects into package or not.
			/// </summary>
			public bool IncludeReferencedProjects;

			/// <summary>
			///		Do not perform package analysis (i.e. disables warnings).
			/// </summary>
			public bool NoPackageAnalysis;

			/// <summary>
			///		Do not exclude folders started from dot.
			/// </summary>			
			public bool NoDefaultExcludes;

			/// <summary>
			///		If set, the destination directory will contain only the package name, not the version number.
			/// </summary>
			public bool ExcludeVersion;

			/// <summary>
			///		Disable looking up packages from local machine cache.
			/// </summary>
			public bool NoCache;

			/// <summary>
			///		Access key for package push.
			/// </summary>
			/// <remarks>
			///		Can be set via command line as "NuGet.AccessKey=&lt;value&gt;" or settings file as "NuGet.AccessKey": "&lt;value&gt;".
			/// </remarks>
			public string AccessKey;

			/// <summary>
			///		Package source URL.
			/// </summary>
			/// <remarks>
			///		<para>Can be set via command line as "NuGet.SourceUrl=&lt;value&gt;" or settings file as "NuGet.SourceUrl": "&lt;value&gt;".</para>
			///		<para>If ommitted then default NuGet source is used.</para>
			/// </remarks>
			public string SourceUrl;

			/// <summary>
			///		(v2.5) The NuGet configuation file. If not specified, file %AppData%\NuGet\NuGet.config is used as configuration file.
			/// </summary>
			/// <remarks>
			///		Can be set via command line as "NuGet.ConfigFile=&lt;value&gt;" or settings file as "NuGet.ConfigFile": "&lt;value&gt;".
			/// </remarks>
			public FileSystemPath ConfigFile;

			/// <summary>
			///		Timeout for NuGet operation. Default: TimeSpan.MaxValue
			/// </summary>
			public TimeSpan Timeout;

			/// <summary>
			///		Path to 'nuget.exe'. Default: '.nuget/NuGet.exe'
			/// </summary>
			public FileSystemPath ToolPath;

			/// <summary>
			///		Additional nuget arguments passed as is.
			/// </summary>
			public string ToolArguments;

			internal Params()
			{
				OutputDirectory = "packages".AsPath();
				Timeout = TimeSpan.MaxValue;				
			}

			/// <summary>
			///		Clones parameters.
			/// </summary>
			/// <returns>copy of original parameters</returns>
			public Params Clone()
			{
				return (Params) MemberwiseClone();
			}
		}

		/// <summary>
		///		Default NuGet parameters.
		/// </summary>
		public static Params Defaults { get; private set; }

		static NuGet()
		{
			Defaults = new Params();

			MyBuild.Initialized += (s, p) =>
			{
				Defaults.ToolPath = Locations.AsFileSet().Select(x => x.Path).FirstOrDefault();

				string value;
				if (p.Properties.TryGetValue("NuGet.SourceUrl", out value))
				{
					Defaults.SourceUrl = value;
				}
				if (p.Properties.TryGetValue("NuGet.AccessKey", out value))
				{
					Defaults.AccessKey = value;
				}
				if (p.Properties.TryGetValue("NuGet.OutputDirectory", out value))
				{
					Defaults.OutputDirectory = value.AsPath();
				}
				if (p.Properties.TryGetValue("NuGet.ConfigFile", out value))
				{
					Defaults.ConfigFile = value.AsPath();
				}
			};
		}

		/// <summary>
		///		Equals to 'nuget.exe install'.
		/// </summary>		
		/// <param name="packageId">id of package to be installed</param>
		/// <param name="setParams">action which overrides default parameters</param>
		/// <seealso cref="http://docs.nuget.org/docs/reference/command-line-reference"/>
		/// <example>
		/// <code>
		///		NuGet.Install(
        ///			"NUnitTestAdapter",
        ///			(fun p -> 
        ///				p.Version &lt;- "1.2"))
		/// </code>
		/// </example>
		public static void Install(string packageId, Action<Params> setParams)
		{
			if (String.IsNullOrEmpty(packageId))
				throw new ArgumentException("NuGet.Install(packageId[, setParams]): packageId must not be null or empty");			
			if (setParams == null)
				throw new ArgumentException("NuGet.Install(packageId, setParams): setParams must not be null");
			
			DoInstall(packageId, setParams);
		}

		/// <summary>
		///		Equals to 'nuget.exe install'.
		/// </summary>		
		/// <param name="packagesConfig">packages.config file</param>
		/// <param name="setParams">action which overrides default parameters</param>
		/// <seealso cref="http://docs.nuget.org/docs/reference/command-line-reference"/>		
		public static void Install(FileItem packagesConfig, Action<Params> setParams)
		{
			if (packagesConfig == null)
				throw new ArgumentException("NuGet.Install(packagesConfig[, setParams]): packagesConfig must not be null or empty");
			if (setParams == null)
				throw new ArgumentException("NuGet.Install(packagesConfig, setParams): setParams must not be null");

			DoInstall(packagesConfig.Path.Full, setParams);
		}

		private static void DoInstall(string packageIdOrConfigPath, Action<Params> setParams)
		{
			var parameters = Defaults.Clone();
			setParams(parameters);

			EnsureToolPath(parameters);

			Trace.InfoFormat("NuGet.Install => {0}", packageIdOrConfigPath);

			var args = new Args("-", " ")
				.Command("install")
				.Param(packageIdOrConfigPath)
				.Option("Version", parameters.Version)
				.Option("Source", parameters.SourceUrl)
				.Option("OutputDirectory", parameters.OutputDirectory)
				.Option("ConfigFile", parameters.ConfigFile)
				.Option("ExcludeVersion", parameters.ExcludeVersion)
				.Option("NoCache", parameters.NoCache)
				.Option("NonInteractive", true)
				.Other(parameters.ToolArguments);

			var result = Process.Run(p =>
			{
				p.FileName = parameters.ToolPath;
				p.Timeout = parameters.Timeout;
				p.Arguments = args.ToString();
			});

			result
				.FailIfAnyError("Target terminated due to NuGet errors.")
				.FailIfExitCodeNonZero(String.Format("NuGet.Install failed with exit code {0}. Package: {1}", result.ExitCode, packageIdOrConfigPath));
		}

		///  <summary>
		/// 		Equals to 'NuGet.exe restore' with no additional arguments.
		///  </summary>		
		///  <seealso cref="http://docs.nuget.org/docs/reference/command-line-reference"/>		
		public static void Restore()
		{
			DoRestore(null, p => { });
		}

		///  <summary>
		/// 		Equals to <see cref="Restore(AnFake.Core.FileItem,System.Action{AnFake.Core.NuGet.Params})">Restore(slnOrConfigFile, p => {})</see>.
		///  </summary>				
		///  <param name="slnOrConfigFile">sln or packages.config file</param>		
		///  <seealso cref="http://docs.nuget.org/docs/reference/command-line-reference"/>
		///  <example>
		///  <code>
		/// 		NuGet.Restore("MySolution.sln".AsFile())
		///  </code>
		///  </example>
		public static void Restore(FileItem slnOrConfigFile)
		{
			if (slnOrConfigFile == null)
				throw new ArgumentException("NuGet.Restore(slnOrConfigFile): slnOrConfigFile must not be null");

			DoRestore(slnOrConfigFile, p => { });
		}

		///  <summary>
		/// 		Equals to 'nuget.exe restore'.
		///  </summary>				
		///  <remarks>
		///		Requires NuGet 2.7 or above.
		///	 </remarks>
		///  <param name="slnOrConfigFile">sln or packages.config file</param>
		///  <param name="setParams">action which overrides default parameters</param>
		///  <seealso cref="http://docs.nuget.org/docs/reference/command-line-reference"/>		
		public static void Restore(FileItem slnOrConfigFile, Action<Params> setParams)
		{
			if (slnOrConfigFile == null)
				throw new ArgumentException("NuGet.Restore(slnOrConfigFile[, setParams]): slnOrConfigFile must not be null");
			if (setParams == null)
				throw new ArgumentException("NuGet.Restore(slnOrConfigFile, setParams): setParams must not be null");

			DoRestore(slnOrConfigFile, setParams);
		}

		private static void DoRestore(FileItem slnOrConfigFile, Action<Params> setParams)
		{
			var parameters = Defaults.Clone();
			setParams(parameters);

			EnsureToolPath(parameters);

			if (slnOrConfigFile != null)
			{
				Trace.InfoFormat("NuGet.Restore => {0}", slnOrConfigFile);
			}
			else
			{
				Trace.Info("NuGet.Restore");
			}			

			var args = new Args("-", " ")
				.Command("restore");

			if (slnOrConfigFile != null)
			{
				args.Param(slnOrConfigFile.Path.Full);
			}
				
			args.Option("Source", parameters.SourceUrl)
				.Option("OutputDirectory", parameters.OutputDirectory)
				.Option("SolutionDirectory", parameters.SolutionDirectory)
				.Option("ConfigFile", parameters.ConfigFile)
				.Option("NoCache", parameters.NoCache)
				.Option("NonInteractive", true)
				.Other(parameters.ToolArguments);

			var result = Process.Run(p =>
			{
				p.FileName = parameters.ToolPath;
				p.Timeout = parameters.Timeout;
				p.Arguments = args.ToString();
			});

			result
				.FailIfAnyError("Target terminated due to NuGet errors.")
				.FailIfExitCodeNonZero(
					slnOrConfigFile != null
						? String.Format("NuGet.Restore failed with exit code {0}. Solution: {1}", result.ExitCode, slnOrConfigFile)
						: String.Format("NuGet.Restore failed with exit code {0}.", result.ExitCode));
		}

		/// <summary>
		///		Creates package spec of version 2.0
		/// </summary>
		/// <param name="setMeta">action which sets package metadata</param>
		/// <returns>package spec instance</returns>
		public static NuSpec.v20.Package Spec20(Action<NuSpec.v20.Metadata> setMeta)
		{
			var pkg = new NuSpec.v20.Package { Metadata = new NuSpec.v20.Metadata() };

			setMeta(pkg.Metadata);

			return pkg;
		}

		/// <summary>
		///		Creates package spec of version 2.5
		/// </summary>
		/// <param name="setMeta">action which sets package metadata</param>
		/// <returns>package spec instance</returns>
		public static NuSpec.v25.Package Spec25(Action<NuSpec.v25.Metadata> setMeta)
		{
			var pkg = new NuSpec.v25.Package { Metadata = new NuSpec.v25.Metadata() };

			setMeta(pkg.Metadata);

			return pkg;			
		}

		/// <summary>
		///		Equals to 'nuget.exe pack'.
		/// </summary>
		/// <remarks>
		///		Files to be packed must be specified via <c>AddFiles</c> on package spec.
		///		Package will be created in given destination folder.
		/// </remarks>
		/// <param name="nuspec">package spec returned by <see cref="Spec20"/> or <see cref="Spec25"/></param>
		/// <param name="dstFolder">output folder for created package</param>
		/// <returns>file item representing created package</returns>
		/// <seealso cref="http://docs.nuget.org/docs/reference/command-line-reference"/>
		public static FileItem Pack(NuSpec.IPackage nuspec, FileSystemPath dstFolder)
		{
			return Pack(nuspec, dstFolder, dstFolder, p => { });
		}

		/// <summary>
		///		Equals to 'nuget.exe pack'.
		/// </summary>
		/// <remarks>
		///		Files to be packed must be specified via <c>AddFiles</c> on package spec.
		///		Package will be created in given destination folder.
		/// </remarks>
		/// <param name="nuspec">package spec returned by <see cref="Spec20"/> or <see cref="Spec25"/></param>
		/// <param name="dstFolder">output folder for created package</param>
		/// <param name="setParams">action which overrides default parameters</param>
		/// <returns>file item representing created package</returns>
		/// <seealso cref="http://docs.nuget.org/docs/reference/command-line-reference"/>
		public static FileItem Pack(NuSpec.IPackage nuspec, FileSystemPath dstFolder, Action<Params> setParams)
		{
			return Pack(nuspec, dstFolder, dstFolder, setParams);
		}

		/// <summary>
		///		Equals to 'nuget.exe pack'.
		/// </summary>
		/// <remarks>
		///		Files to be packed will be taken from specified source folder.
		///		Package will be created in given destination folder.
		/// </remarks>
		/// <param name="nuspec">package spec returned by <see cref="Spec20"/> or <see cref="Spec25"/></param>
		/// <param name="srcFolder">source folder containing files to be packed</param>
		/// <param name="dstFolder">output folder for created package</param>
		/// <returns>file item representing created package</returns>
		/// <seealso cref="http://docs.nuget.org/docs/reference/command-line-reference"/>
		public static FileItem Pack(NuSpec.IPackage nuspec, FileSystemPath srcFolder, FileSystemPath dstFolder)
		{
			return Pack(nuspec, srcFolder, dstFolder, p => { });
		}

		/// <summary>
		///		Equals to 'nuget.exe pack'.
		/// </summary>
		/// <remarks>
		///		Files to be packed will be taken from specified source folder.
		///		Package will be created in given destination folder.
		/// </remarks>
		/// <param name="nuspec">package spec returned by <see cref="Spec20"/> or <see cref="Spec25"/></param>
		/// <param name="srcFolder">source folder containing files to be packed</param>
		/// <param name="dstFolder">output folder for created package</param>
		/// <param name="setParams">action which overrides default parameters</param>
		/// <returns>file item representing created package</returns>
		/// <seealso cref="http://docs.nuget.org/docs/reference/command-line-reference"/>
		/// <example>
		/// <code>
		///		let nugetFiles = 
		///			~~".out" % "*.dll"
		///			+ "*.exe"
		///			+ "*.config"
		/// 
		///		let nuspec = NuGet.Spec25(fun meta -> 
        ///			meta.Id &lt;- "AnFake"
        ///			meta.Version &lt;- version
        ///			meta.Authors &lt;- "Ilya A. Ivanov"
        ///			meta.Description &lt;- "AnFake: Another F# Make"
		///		)
		///
		///		nuspec.AddFiles(nugetFiles, "")
		///
		///		NuGet.Pack(nuspec, ~~".out", fun p -> 
        ///			p.NoPackageAnalysis &lt;- true
        ///			p.NoDefaultExcludes &lt;- true)
        ///		|> ignore
		/// </code>
		/// </example>
		public static FileItem Pack(NuSpec.IPackage nuspec, FileSystemPath srcFolder, FileSystemPath dstFolder, Action<Params> setParams)
		{
			if (nuspec == null)
				throw new ArgumentException("NuGet.Pack(nuspec, srcFolder, dstFolder, setParams): nuspec must not be null");
			if (srcFolder == null)
				throw new ArgumentException("NuGet.Pack(nuspec, srcFolder, dstFolder, setParams): srcFolder must not be null");
			if (dstFolder == null)
				throw new ArgumentException("NuGet.Pack(nuspec, srcFolder, dstFolder, setParams): dstFolder must not be null");
			if (setParams == null)
				throw new ArgumentException("NuGet.Pack(nuspec, srcFolder, dstFolder, setParams): setParams must not be null");

			nuspec.Validate();			

			var parameters = Defaults.Clone();
			setParams(parameters);

			EnsureToolPath(parameters);
			// TODO: check other parameters			

			var nuspecFile = GenerateNuspecFile(nuspec, srcFolder);

			Trace.InfoFormat("NuGet.Pack => {0}", nuspecFile);

			var args = new Args("-", " ")
				.Command("pack")
				.Param(nuspecFile.Path.Full)
				.Option("OutputDirectory", dstFolder)
				.Option("NoPackageAnalysis", parameters.NoPackageAnalysis)
				.Option("NoDefaultExcludes", parameters.NoDefaultExcludes)
				.Option("IncludeReferencedProjects", parameters.IncludeReferencedProjects)
				.Other(parameters.ToolArguments);

			Folders.Create(dstFolder);

			var result = Process.Run(p =>
			{
				p.FileName = parameters.ToolPath;
				p.Timeout = parameters.Timeout;
				p.Arguments = args.ToString();				
			});

			result
				.FailIfAnyError("Target terminated due to NuGet errors.")
				.FailIfExitCodeNonZero(
					String.Format("NuGet.Pack failed with exit code {0}. Package: {1}", result.ExitCode, nuspecFile));

			var pkgPath = dstFolder / String.Format("{0}.{1}.nupkg", nuspec.Id, nuspec.Version);

			return pkgPath.AsFile();
		}		

		/// <summary>
		///		Equals to 'nuget.exe push'.
		/// </summary>
		/// <param name="package">path to package to be pushed</param>
		/// <param name="setParams">action which overrides default parameters</param>
		/// <seealso cref="http://docs.nuget.org/docs/reference/command-line-reference"/>
		/// <example>
		/// <code>
		///		NuGet.Push(
        ///			~~".out" / "AnFake.0.9.nupkg",
        ///			fun p -> 
        ///				p.AccessKey &lt;- "YOUR ACCESS KEY"
        ///				p.SourceUrl &lt;- "SOURCE URL HERE")		
		/// </code>
		/// </example>
		public static void Push(FileItem package, Action<Params> setParams)
		{
			if (package == null)
				throw new ArgumentException("NuGet.Push(package, setParams): package must not be null");
			if (setParams == null)
				throw new ArgumentException("NuGet.Push(package, setParams): setParams must not be null");

			var parameters = Defaults.Clone();
			setParams(parameters);

			EnsureToolPath(parameters);

			if (String.IsNullOrEmpty(parameters.AccessKey))
				throw new ArgumentException("NuGet.Params.AccessKey must not be null or empty");

			// TODO: check other parameters

			Trace.InfoFormat("NuGet.Push => {0}", package);

			var args = new Args("-", " ")
				.Command("push")
				.Param(package.Path.Full)
				.Param(parameters.AccessKey)
				.Option("s", parameters.SourceUrl)
				.Other(parameters.ToolArguments);

			var result = Process.Run(p =>
			{
				p.FileName = parameters.ToolPath;
				p.Timeout = parameters.Timeout;
				p.Arguments = args.ToString();				
			});

			result
				.FailIfAnyError("Target terminated due to NuGet errors.")
				.FailIfExitCodeNonZero(String.Format("NuGet.Push failed with exit code {0}. Package: {1}", result.ExitCode, package));

			Trace.SummaryFormat("NuGet.Push: {0} @ {1}", package.Name, parameters.SourceUrl);
		}

		private static void EnsureToolPath(Params parameters)
		{
			if (parameters.ToolPath == null)
				throw new ArgumentException(
					String.Format(
						"NuGet.Params.ToolPath must not be null.\nHint: probably, NuGet.exe not found.\nSearch path:\n  {0}",
						String.Join("\n  ", Locations)));
		}

		private static FileItem GenerateNuspecFile(NuSpec.IPackage nuspec, FileSystemPath srcFolder)
		{
			var nuspecFile = (srcFolder / nuspec.Id + ".nuspec").AsFile();
			
			Folders.Create(nuspecFile.Folder);
			using (var stm = new FileStream(nuspecFile.Path.Full, FileMode.Create, FileAccess.Write))
			{
				new XmlSerializer(nuspec.GetType()).Serialize(stm, nuspec);
			}

			return nuspecFile;
		}
	}
}
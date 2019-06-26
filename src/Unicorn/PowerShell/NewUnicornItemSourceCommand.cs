using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Commandlets.Packages;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.Install;
using Sitecore.Install.Configuration;
using Sitecore.Install.Items;
using Sitecore.Install.Utils;
using Unicorn.Configuration;
using Unicorn.Predicates;

namespace Unicorn.PowerShell
{
	/// <summary>
	/// $foo = $config | New-UnicornItemSource [-Project] [-Name] [-InstallMode] [-MergeMode] [-SkipVersions]
	/// 
	/// Complete example, packaging several Unicorn configurations:
	/// $pkg = New-Package
	/// Get-UnicornConfiguration "Foundation.*" | New-UnicornItemSource -Project $pkg
	/// Export-Package -Project $pkg -Path "C:\foo.zip"
	/// 
	/// NOTE: This cmdlet generates the package based off the database state, not serialized state.
	/// Make sure you sync your database with serialized before generating packages with this.
	/// </summary>
	[Cmdlet(VerbsCommon.New, "UnicornItemSource")]
	[OutputType(typeof(ExplicitItemSource))]
	public class NewUnicornItemSourceCommand : BasePackageCommand
	{
		[Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, Mandatory = true)]
		public IConfiguration Configuration { get; set; }

		[Parameter(Position = 0)]
		public string Name { get; set; }

		[Parameter(Position = 1)]
		public SwitchParameter SkipVersions { get; set; }

		[Parameter]
		public InstallMode InstallMode { get; set; }

		[Parameter]
		public MergeMode MergeMode { get; set; }

		/// <summary>
		/// If set adds the source(s) generated to a package project created with New-Package
		/// </summary>
		[Parameter]
		public PackageProject Project { get; set; }

		protected override void ProcessRecord()
		{
			var source = new ExplicitItemSource { Name = Name ?? Configuration.Name, SkipVersions = SkipVersions.IsPresent };

			if (InstallMode != InstallMode.Undefined)
			{
				source.Converter.Transforms.Add(
					new InstallerConfigurationTransform(new BehaviourOptions(InstallMode, MergeMode)));
			}

			var predicate = Configuration.Resolve<IPredicate>();
			var roots = predicate.GetRootPaths();

			var processingQueue = new Queue<Item>();

			foreach (var root in roots)
			{
				
				var db = Factory.GetDatabase(root.DatabaseName);
				var item = db.GetItem(root.Path);

				if (item == null) continue;

				processingQueue.Enqueue(item);
			}

			while (processingQueue.Count > 0)
			{
				var item = processingQueue.Dequeue();

				source.Entries.Add(new ItemReference(item.Database.Name, item.Paths.Path, item.ID, Language.Invariant, Version.Latest).ToString());

				foreach (var child in item.Children.Where(chd => predicate.Includes(new Rainbow.Storage.Sc.ItemData(chd)).IsIncluded))
				{
					processingQueue.Enqueue(child);
				}
			}
			
			if(Project == null) WriteObject(source, false);
			Project?.Sources.Add(source);
		}
	}
}
using System.Management.Automation;
using System.Xml;
using Cognifide.PowerShell.Commandlets;
using Rainbow.Filtering;
using Rainbow.Storage.Yaml;

namespace Unicorn.PowerShell
{
	public abstract class YamlCommandBase : BaseCommand
	{
		protected virtual IFieldFilter CreateFieldFilter()
		{
			// shut yer gob :D
			var config = @"<fieldFilter type=""Rainbow.Filtering.ConfigurationFieldFilter, Rainbow"" singleInstance=""true"">
					<exclude fieldID=""{B1E16562-F3F9-4DDD-84CA-6E099950ECC0}"" note=""'Last run' field on Schedule template (used to register tasks)"" />
					<exclude fieldID=""{52807595-0F8F-4B20-8D2A-CB71D28C6103}"" note=""'__Owner' field on Standard Template"" />
					<exclude fieldID=""{F6D8A61C-2F84-4401-BD24-52D2068172BC}"" note=""'__Originator' field on Standard Template"" />
					<exclude fieldID=""{8CDC337E-A112-42FB-BBB4-4143751E123F}"" note=""'__Revision' field on Standard Template"" />
					<exclude fieldID=""{D9CF14B1-FA16-4BA6-9288-E8A174D4D522}"" note=""'__Updated' field on Standard Template"" />
					<exclude fieldID=""{BADD9CF9-53E0-4D0C-BCC0-2D784C282F6A}"" note=""'__Updated by' field on Standard Template"" />
					<exclude fieldID=""{001DD393-96C5-490B-924A-B0F25CD9EFD8}"" note=""'__Lock' field on Standard Template"" />
				</fieldFilter>";

			var rawConfig = @"<fieldFilter type=""Rainbow.Filtering.ConfigurationFieldFilter, Rainbow"" singleInstance=""true""></fieldFilter>";

			var doc = new XmlDocument();
			doc.LoadXml(Raw.IsPresent ? rawConfig : config);

			return new ConfigurationFieldFilter(doc.DocumentElement);
		}

		protected virtual YamlSerializationFormatter CreateFormatter(IFieldFilter filter)
		{
			// shut yer gob again :D
			var config = @"<serializationFormatter type=""Rainbow.Storage.Yaml.YamlSerializationFormatter, Rainbow.Storage.Yaml"" singleInstance=""true"">
					<fieldFormatter type=""Rainbow.Formatting.FieldFormatters.MultilistFormatter, Rainbow"" />
					<fieldFormatter type=""Rainbow.Formatting.FieldFormatters.XmlFieldFormatter, Rainbow"" />
					<fieldFormatter type=""Rainbow.Formatting.FieldFormatters.CheckboxFieldFormatter, Rainbow"" />
				</serializationFormatter>";

			var rawConfig = @"<serializationFormatter type=""Rainbow.Storage.Yaml.YamlSerializationFormatter, Rainbow.Storage.Yaml"" singleInstance=""true""></serializationFormatter>";

			var doc = new XmlDocument();
			doc.LoadXml(Raw.IsPresent ? rawConfig : config);

			return new YamlSerializationFormatter(doc.DocumentElement, filter);
		}

		/// <summary>
		/// If set, all field filtering and formatting is disabled and complete raw item data is dumped
		/// </summary>
		[Parameter]
		public SwitchParameter Raw { get; set; }
	}
}

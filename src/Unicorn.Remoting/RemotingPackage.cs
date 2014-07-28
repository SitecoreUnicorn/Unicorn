using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using Newtonsoft.Json;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Unicorn.Configuration;
using Unicorn.Predicates;
using Unicorn.Serialization;
using Unicorn.Serialization.Sitecore;

namespace Unicorn.Remoting
{
	public class RemotingPackage
	{
		private IConfiguration _configuration;
		private SitecoreSerializationProvider _serializationProvider;

		public RemotingPackage(IConfiguration configuration)
		{
			Assert.ArgumentNotNull(configuration, "configuration");

			_configuration = configuration;
			Configuration = _configuration.Name;
			Manifest = new RemotingPackageManifest();
			TempDirectory = GenerateTempDirectory();
		}

		public RemotingPackage()
		{

		}

		public static RemotingPackage FromStream(Stream zipStream)
		{
			Assert.ArgumentNotNull(zipStream, "zipStream");

			var tempDirectory = GenerateTempDirectory();

			Compression.DecompressZipFileFromStream(zipStream, tempDirectory);

			var package = JsonConvert.DeserializeObject<RemotingPackage>(File.ReadAllText(Path.Combine(tempDirectory, "manifest.json")));

			package.TempDirectory = tempDirectory;

			return package;
		}

		public RemotingPackageManifest Manifest { get; set; }

		[JsonProperty] // TODO: fix reading packages from disk - this is not being written to the json manifest
		public string Configuration
		{
			get { return _configuration.Name; }
			private set { _configuration = UnicornConfigurationManager.Configurations.First(x => x.Name.Equals(value, StringComparison.Ordinal)); }
		}

		public ISerializationProvider SerializationProvider
		{
			get
			{
				if (_serializationProvider == null) _serializationProvider = new SitecoreSerializationProvider(_configuration.Resolve<IPredicate>(), Path.Combine(TempDirectory, "serialization"), "UnicornRemotingPackageSerialization");

				return _serializationProvider;
			}
		}

		public void WriteToHttpResponse(HttpResponseBase httpResponse)
		{	
			httpResponse.Clear();
			httpResponse.ContentType = "application/zip";
			httpResponse.AddHeader("Content-Disposition", "attachment;filename=remoting-package.zip");

			WriteToStream(httpResponse.OutputStream); ;

			httpResponse.End();
		}

		public void WriteToStream(Stream stream)
		{
			Manifest.WriteToPackage(TempDirectory);

			Compression.CompressDirectoryToStream(TempDirectory, stream);
			
			stream.Flush();
			
			Directory.Delete(TempDirectory, true);
		}

		private static string GenerateTempDirectory()
		{
			return Path.Combine(HostingEnvironment.MapPath(Settings.TempFolderPath), Guid.NewGuid().ToString());
		}

		private string TempDirectory { get; set; }
	}
}

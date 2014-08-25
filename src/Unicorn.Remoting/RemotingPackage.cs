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
	public class RemotingPackage : IDisposable
	{
		private readonly IConfiguration _configuration;
		private SitecoreSerializationProvider _serializationProvider;

		public RemotingPackage(IConfiguration configuration)
		{
			Assert.ArgumentNotNull(configuration, "configuration");

			_configuration = configuration;

			Manifest = new RemotingPackageManifest();
			Manifest.ConfigurationName = _configuration.Name;

			TempDirectory = GenerateTempDirectory();
		}

		private RemotingPackage(RemotingPackageManifest manifest, string tempDirectory)
		{
			Assert.ArgumentNotNull(manifest, "manifest");

			_configuration = UnicornConfigurationManager.Configurations.First(x => x.Name.Equals(manifest.ConfigurationName, StringComparison.Ordinal));
			Manifest = manifest;

			TempDirectory = tempDirectory;
		}

		public static RemotingPackage FromStream(Stream zipStream)
		{
			Assert.ArgumentNotNull(zipStream, "zipStream");

			var tempDirectory = GenerateTempDirectory();

			Compression.DecompressZipFileFromStream(zipStream, tempDirectory);

			var manifest = JsonConvert.DeserializeObject<RemotingPackageManifest>(File.ReadAllText(Path.Combine(tempDirectory, "manifest.json")));

			var package = new RemotingPackage(manifest, tempDirectory);

			return package;
		}

		public RemotingPackageManifest Manifest { get; private set; }

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

			WriteToStream(httpResponse.OutputStream);

			httpResponse.End();
		}

		public void WriteToStream(Stream stream)
		{
			Manifest.WriteToPackage(TempDirectory);

			Compression.CompressDirectoryToStream(TempDirectory, stream);

			stream.Flush();
		}

		private static string GenerateTempDirectory()
		{
			var tempDirectory = Path.Combine(HostingEnvironment.MapPath(Settings.TempFolderPath), Guid.NewGuid().ToString());
			Directory.CreateDirectory(tempDirectory);

			return tempDirectory;
		}

		public string TempDirectory { get; private set; }

		public void Dispose()
		{
			if(Directory.Exists(TempDirectory)) Directory.Delete(TempDirectory, true);
		}
	}
}

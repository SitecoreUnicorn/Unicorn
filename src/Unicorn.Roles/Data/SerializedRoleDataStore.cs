using System.Collections.Generic;
using System.IO;
using System.Web.Hosting;
using Sitecore.Data.Serialization;
using Sitecore.Data.Serialization.ObjectModel;
using Sitecore.Diagnostics;
using Sitecore.Security.Accounts;

namespace Unicorn.Roles.Data
{
  using Unicorn.Roles.Model;
  using Unicorn.Roles.Serealizer;

  /// <summary>
	/// Stores roles on disk using Sitecore's built in role serialization APIs.
	/// </summary>
	public class SerializedRoleDataStore : IRoleDataStore
	{
		private readonly string _physicalRootPath;

		/// <summary>
		/// Default constructor
		/// </summary>
		/// <param name="physicalRootPath">The physical root path. May be site-root-relative by using "~/" as the prefix.</param>
		public SerializedRoleDataStore(string physicalRootPath)
		{
			// ReSharper disable once DoNotCallOverridableMethodsInConstructor
			_physicalRootPath = InitializeRootPath(physicalRootPath);
		}

		public virtual IEnumerable<SyncRoleFile> GetAll()
		{
			if (!Directory.Exists(_physicalRootPath)) yield break;

			var roles = Directory.GetFiles(_physicalRootPath, "*.role", SearchOption.AllDirectories);

			foreach (var role in roles)
			{
				yield return ReadSyncRole(role);
			}
		}

		public virtual void Save(Role role)
		{
			var path = GetPathForRole(role);

			//Manager.DumpRole(path, role);
      var serializer = new RoleSerializer();
      serializer.DumpRole(path, role);
		}

		public virtual void Remove(Role role)
		{
			var path = GetPathForRole(role);

			if(File.Exists(path)) File.Delete(path);
		}

		public virtual void Clear()
		{
			if (!Directory.Exists(_physicalRootPath)) return;

			Directory.Delete(_physicalRootPath, true);

			Directory.CreateDirectory(_physicalRootPath);
		}

		protected virtual string GetPathForRole(Role role)
		{
			return Path.Combine(_physicalRootPath, role.LocalName + ".role");
		}

		protected virtual SyncRoleFile ReadSyncRole(string path)
		{
			Assert.ArgumentNotNullOrEmpty(path, nameof(path));

			using (TextReader reader = new StreamReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)))
			{
				return new SyncRoleFile(SyncRole.ReadRole(new Tokenizer(reader)), path);
			}
		}

		protected virtual string InitializeRootPath(string rootPath)
		{
			if (rootPath.StartsWith("~") || rootPath.StartsWith("/"))
			{
				rootPath = HostingEnvironment.MapPath("~/") + rootPath.Substring(1).Replace("/", Path.DirectorySeparatorChar.ToString());
			}

			if (!Directory.Exists(rootPath)) Directory.CreateDirectory(rootPath);

			return rootPath;
		}
	}
}

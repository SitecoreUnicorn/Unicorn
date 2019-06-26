using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Hosting;
using Sitecore.Diagnostics;
using Unicorn.Roles.Formatting;
using Unicorn.Roles.Model;

namespace Unicorn.Roles.Data
{
	public class FilesystemRoleDataStore : IRoleDataStore
	{
		private readonly IRoleSerializationFormatter _formatter;
		private readonly string _physicalRootPath;

		/// <summary>
		/// Default constructor
		/// </summary>
		/// <param name="physicalRootPath">The physical root path. May be site-root-relative by using "~/" as the prefix.</param>
		/// <param name="roleFormatter"></param>
		public FilesystemRoleDataStore(string physicalRootPath, IRoleSerializationFormatter roleFormatter)
		{
			Assert.ArgumentNotNull(roleFormatter, nameof(roleFormatter));
			
			_formatter = roleFormatter;

			// ReSharper disable once DoNotCallOverridableMethodsInConstructor
			_physicalRootPath = InitializeRootPath(physicalRootPath);
		}

		public virtual IEnumerable<IRoleData> GetAll()
		{
			if (!Directory.Exists(_physicalRootPath)) yield break;

			var roles = Directory.GetFiles(_physicalRootPath, "*" + _formatter.FileExtension, SearchOption.AllDirectories);

			foreach (var role in roles)
			{
				yield return ReadSyncRole(role);
			}
		}

		public virtual void Save(IRoleData role)
		{
			var path = GetPathForRole(role);

			var parent = Path.GetDirectoryName(path);
			if (!Directory.Exists(parent)) Directory.CreateDirectory(parent);

			using (var writer = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None))
			{
				_formatter.WriteSerializedRole(role, writer);
			}
		}

		public virtual void Remove(IRoleData role)
		{
			var path = GetPathForRole(role);

			if (File.Exists(path)) File.Delete(path);
		}

		public virtual void Clear()
		{
			if (!Directory.Exists(_physicalRootPath)) return;

			Directory.Delete(_physicalRootPath, true);

			Directory.CreateDirectory(_physicalRootPath);
		}

		protected virtual string GetPathForRole(IRoleData role)
		{
			return Path.Combine(_physicalRootPath, role.RoleName + _formatter.FileExtension);
		}

		protected virtual IRoleData ReadSyncRole(string path)
		{
			Assert.ArgumentNotNullOrEmpty(path, nameof(path));

			using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				return _formatter.ReadSerializedRole(stream, path);
			}
		}

		protected virtual string InitializeRootPath(string rootPath)
		{
			if (rootPath.StartsWith("~") || rootPath.StartsWith("/"))
			{
				var cleanRootPath = rootPath.TrimStart('~', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				cleanRootPath = cleanRootPath.Replace("/", Path.DirectorySeparatorChar.ToString());

				var basePath = HostingEnvironment.IsHosted ? HostingEnvironment.MapPath("~/") : AppDomain.CurrentDomain.BaseDirectory;
				rootPath = Path.Combine(basePath, cleanRootPath);
			}

			// convert root path to canonical form, so subsequent transformations can do string comparison
			// http://stackoverflow.com/questions/970911/net-remove-dots-from-the-path
			if (rootPath.Contains(".."))
				rootPath = Path.GetFullPath(rootPath);

			if (!Directory.Exists(rootPath)) Directory.CreateDirectory(rootPath);

			return rootPath;
		}
	}
}

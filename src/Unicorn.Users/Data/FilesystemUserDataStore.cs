using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Hosting;
using Sitecore.Diagnostics;
using Sitecore.Security.Serialization.ObjectModel;
using Unicorn.Users.Formatting;

namespace Unicorn.Users.Data
{
	public class FilesystemUserDataStore : IUserDataStore
	{
		private readonly IUserSerializationFormatter _userFormatter;
		private readonly string _physicalRootPath;

		/// <summary>
		/// Default constructor
		/// </summary>
		/// <param name="physicalRootPath">The physical root path. May be site-root-relative by using "~/" as the prefix.</param>
		public FilesystemUserDataStore(string physicalRootPath, IUserSerializationFormatter userFormatter)
		{
			Assert.ArgumentNotNull(userFormatter, nameof(userFormatter));

			_userFormatter = userFormatter;
			// ReSharper disable once DoNotCallOverridableMethodsInConstructor
			_physicalRootPath = InitializeRootPath(physicalRootPath);
		}

		public virtual IEnumerable<SyncUserFile> GetAll()
		{
			if (!Directory.Exists(_physicalRootPath)) yield break;

			var users = Directory.GetFiles(_physicalRootPath, "*" + _userFormatter.FileExtension, SearchOption.AllDirectories);

			foreach (var user in users)
			{
				yield return ReadSyncUser(user);
			}
		}

		public virtual void Save(SyncUser user)
		{
			var path = GetPathForUser(user.UserName);

			var parent = Path.GetDirectoryName(path);
			if (!Directory.Exists(parent)) Directory.CreateDirectory(parent);

			using (var writer = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None))
			{
				_userFormatter.WriteSerializedUser(user, writer);
			}
		}

		public virtual void Remove(string userName)
		{
			var path = GetPathForUser(userName);

			if (File.Exists(path)) File.Delete(path);
		}

		public virtual void Clear()
		{
			if (!Directory.Exists(_physicalRootPath)) return;

			Directory.Delete(_physicalRootPath, true);

			Directory.CreateDirectory(_physicalRootPath);
		}

		protected virtual string GetPathForUser(string userName)
		{
			return Path.Combine(_physicalRootPath, userName + _userFormatter.FileExtension);
		}

		protected virtual SyncUserFile ReadSyncUser(string path)
		{
			Assert.ArgumentNotNullOrEmpty(path, nameof(path));

			using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				return new SyncUserFile(_userFormatter.ReadSerializedUser(stream, path), path);
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
			// https://stackoverflow.com/questions/970911/net-remove-dots-from-the-path
			if (rootPath.Contains(".."))
				rootPath = Path.GetFullPath(rootPath);

			if (!Directory.Exists(rootPath)) Directory.CreateDirectory(rootPath);

			return rootPath;
		}
	}
}

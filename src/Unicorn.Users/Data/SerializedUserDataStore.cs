namespace Unicorn.Users.Data
{
  using System.Collections.Generic;
  using System.IO;
  using System.Web.Hosting;
  using Sitecore.Data.Serialization;
  using Sitecore.Data.Serialization.ObjectModel;
  using Sitecore.Diagnostics;
  using Sitecore.Security.Accounts;
  using Sitecore.Security.Serialization;
  using Sitecore.Security.Serialization.ObjectModel;

  public class SerializedUserDataStore : IUserDataStore
  {
    private readonly string _physicalRootPath;

    /// <summary>
    /// Default constructor
    /// </summary>
    /// <param name="physicalRootPath">The physical root path. May be site-root-relative by using "~/" as the prefix.</param>
    public SerializedUserDataStore(string physicalRootPath)
    {
      // ReSharper disable once DoNotCallOverridableMethodsInConstructor
      _physicalRootPath = InitializeRootPath(physicalRootPath);
    }

    public virtual IEnumerable<SyncUserFile> GetAll()
    {
      if (!Directory.Exists(_physicalRootPath)) yield break;

      var users = Directory.GetFiles(_physicalRootPath, "*.user", SearchOption.AllDirectories);

      foreach (var user in users)
      {
        yield return ReadSyncUser(user);
      }
    }

    public virtual void Save(User user)
    {
      var path = this.GetPathForUser(user);
      var userReference = new UserReference(user.Name);
      Manager.DumpUser(path, userReference.User);
    }

    public virtual void Remove(User user)
    {
      var path = this.GetPathForUser(user);

      if (File.Exists(path)) File.Delete(path);
    }

    public virtual void Clear()
    {
      if (!Directory.Exists(_physicalRootPath)) return;

      Directory.Delete(_physicalRootPath, true);

      Directory.CreateDirectory(_physicalRootPath);
    }

    protected virtual string GetPathForUser(User user)
    {
      return Path.Combine(this._physicalRootPath, user.LocalName + ".user");
    }

    protected virtual SyncUserFile ReadSyncUser(string path)
    {
      Assert.ArgumentNotNullOrEmpty(path, nameof(path));

      using (TextReader reader = new StreamReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)))
      {
        return new SyncUserFile(SyncUser.ReadUser(new Tokenizer(reader)), path);
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

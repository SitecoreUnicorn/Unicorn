namespace Unicorn.Roles.Model
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using Sitecore.Data.Serialization;
  using Sitecore.Data.Serialization.ObjectModel;
  using Sitecore.Security.Accounts;

  public class SyncRole
  {

    public SyncRole()
    {
      this.ParentRoles = new List<string>();
    }
    public string Name { get; set; }

    public List<string> ParentRoles { get; set; }

    public static SyncRole ReadRole(Tokenizer reader)
    {
      while (reader.Line != null && reader.Line.Length == 0)
        reader.NextLine();
      if (reader.Line == null || reader.Line != "----role----")
        throw new Exception("Format error: serialized stream does not start with ----role----");
      var syncRole = new SyncRole();
      reader.NextLine();
      var entry = ReadEntry(reader);
      syncRole.Name = entry.Item2;
      reader.NextLine();
      while (reader.Line != "----parent-roles----" && reader.Line != null)
      {
        reader.NextLine();
      }

      reader.NextLine();

      while (reader.Line!= null)
      {
        string str = ReadEntry(reader).Item2;
        syncRole.ParentRoles.Add(str);
        reader.NextLine();
      }
      
      return syncRole;
    }

    public static SyncRole Create(Role role)
    {
      var syncRole = new SyncRole
      {
        Name = role.Name,
        ParentRoles = RolesInRolesManager.GetRolesForRole(role, false).Select(parentRole => parentRole.Name).ToList()
      };

      return syncRole;
    }

    public void Serialize(string path)
    {
      if (!Role.Exists(this.Name))
      {
        return;
      }

      var role = Role.FromName(this.Name);

      Manager.DumpRole(path, role);
      this.AppendParrentRoles(path, role);
    }

    protected void AppendParrentRoles(string path, Role role)
    {
      var roleFile = new FileInfo(path);
      var writer = roleFile.AppendText();
      writer.WriteLine("----parent-roles----");
      foreach (var parentRole in this.ParentRoles)
      {
        var roleName = $"rolename: {parentRole}";
        writer.WriteLine(roleName);
      }

      writer.Close();
    }


    protected static Tuple<string, string> ReadEntry(Tokenizer reader)
    {
      var nameValue = reader.Line.Split(new [] {':'}, StringSplitOptions.RemoveEmptyEntries);
      var entry = new Tuple<string, string>(nameValue[0].Trim(), nameValue[1].Trim());
      return entry;
    }
}

  

}

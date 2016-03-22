using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unicorn.Roles.Model
{
  using System.Runtime.CompilerServices;
  using Sitecore.Data.Serialization.ObjectModel;

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
      //Dictionary<string, string> dictionary = SerializationUtils.ReadHeaders(reader);
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

    protected static Tuple<string, string> ReadEntry(Tokenizer reader)
    {
      var nameValue = reader.Line.Split(new [] {':'}, StringSplitOptions.RemoveEmptyEntries);
      var entry = new Tuple<string, string>(nameValue[0].Trim(), nameValue[1].Trim());
      return entry;
    }
}

  

}

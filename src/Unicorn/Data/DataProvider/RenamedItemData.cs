using Rainbow.Model;

namespace Unicorn.Data.DataProvider
{
	public class RenamedItemData : ItemDecorator
	{
		public RenamedItemData(IItemData innerItem, string newName) : base(innerItem)
		{
			Name = newName;
		}

		public override string Name { get; }

		public override string Path => base.Path.Substring(0, base.Path.TrimEnd('/').LastIndexOf('/') + 1) + Name; // /foo/bar/ => /foo/, then + Name = /foo/Name
	}
}

namespace Unicorn.Predicates
{
	public class PredicateRootPath
	{
		public string Database { get; private set; }
		public string Path { get; private set; }

		public PredicateRootPath(string database, string path)
		{
			Database = database;
			Path = path;
		}
	}
}

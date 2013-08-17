namespace Unicorn.Serialization
{
	public class SerializedVersion
	{
		public SerializedVersion(string language, int versionNumber)
		{
			Language = language;
			VersionNumber = versionNumber;
			Fields = new SerializedFieldDictionary();
		}

		public int VersionNumber { get; private set; }
		public string Language { get; private set; }
		public SerializedFieldDictionary Fields { get; private set; }
	}
}

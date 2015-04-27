using System.IO;
using Sitecore.Data.Serialization.ObjectModel;
using Unicorn.Data;

namespace Unicorn.Serialization.Sitecore.Formatting
{
	public interface ISitecoreSerializationFormatter
	{
		void Serialize(SyncItem item, Stream outputStream);
		ISourceItem Deserialize(ISerializedItem serializedItem, bool ignoreMissingTemplateFields);
		SyncItem Read(Stream sourceStream);
	}
}

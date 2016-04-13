using System.IO;
using Sitecore.Security.Serialization.ObjectModel;

namespace Unicorn.Users.Formatting
{
	public interface IUserSerializationFormatter
	{
		SyncUser ReadSerializedUser(Stream dataStream, string serializedItemId);

		void WriteSerializedUser(SyncUser userData, Stream outputStream);

		string FileExtension { get; }
	}
}

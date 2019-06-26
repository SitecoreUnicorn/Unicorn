using System.IO;
using Unicorn.Roles.Model;

namespace Unicorn.Roles.Formatting
{
	public interface IRoleSerializationFormatter
	{
		IRoleData ReadSerializedRole(Stream dataStream, string serializedItemId);

		void WriteSerializedRole(IRoleData roleData, Stream outputStream);

		string FileExtension { get; }
	}
}

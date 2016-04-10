using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rainbow.Storage.Yaml;
using Sitecore.Diagnostics;
using Unicorn.Roles.Model;

namespace Unicorn.Roles.Formatting
{
	public class YamlRoleSerializationFormatter : IRoleSerializationFormatter
	{
		public virtual IRoleData ReadSerializedRole(Stream dataStream, string serializedItemId)
		{
			Assert.ArgumentNotNull(dataStream, nameof(dataStream));

			try
			{
				using (var reader = new YamlReader(dataStream, 4096, true))
				{
					string roleName = reader.ReadExpectedMap("Role");

					var parentRoles = new List<string>();
					
					var parentRoleNode = reader.PeekMap();
					if (parentRoleNode.HasValue && parentRoleNode.Value.Key.Equals("MemberOf"))
					{
						reader.ReadMap();
						while (true)
						{
							var parentRole = reader.PeekMap();
							if (!parentRole.HasValue || !parentRole.Value.Key.Equals("Role", StringComparison.Ordinal)) break;

							reader.ReadMap();

							parentRoles.Add(parentRole.Value.Value);
						}
					}

					return new SerializedRoleData(roleName, parentRoles.ToArray(), serializedItemId);
				}
			}
			catch (Exception exception)
			{
				throw new YamlFormatException("Error parsing YAML " + serializedItemId, exception);
			}
		}

		public virtual void WriteSerializedRole(IRoleData roleData, Stream outputStream)
		{
			Assert.ArgumentNotNull(roleData, nameof(roleData));
			Assert.ArgumentNotNull(outputStream, "outputStream");

			var parentRoles = roleData.ParentRoleNames;

			using (var writer = new YamlWriter(outputStream, 4096, true))
			{
				writer.WriteMap("Role", roleData.RoleName);

				if (parentRoles.Any())
				{
					writer.WriteMap("MemberOf");
					writer.IncreaseIndent();

					foreach (var parentRole in parentRoles)
					{
						writer.WriteMap("Role", parentRole);
					}

					writer.DecreaseIndent();
				}
			}
		}

		public string FileExtension => ".yml";
	}
}

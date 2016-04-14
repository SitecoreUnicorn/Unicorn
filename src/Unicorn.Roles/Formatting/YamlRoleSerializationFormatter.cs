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

					var memberOfRoles = new List<string>();
					
					var roleMembershipNode = reader.PeekMap();
					if (roleMembershipNode.HasValue && roleMembershipNode.Value.Key.Equals("MemberOf"))
					{
						reader.ReadMap();
						while (true)
						{
							var memberOfRole = reader.PeekMap();
							if (!memberOfRole.HasValue || !memberOfRole.Value.Key.Equals("Role", StringComparison.Ordinal)) break;

							reader.ReadMap();

							memberOfRoles.Add(memberOfRole.Value.Value);
						}
					}

					return new SerializedRoleData(roleName, memberOfRoles.ToArray(), serializedItemId);
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

			var memberOfRoles = roleData.MemberOfRoles;

			using (var writer = new YamlWriter(outputStream, 4096, true))
			{
				writer.WriteMap("Role", roleData.RoleName);

				if (memberOfRoles.Any())
				{
					writer.WriteMap("MemberOf");
					writer.IncreaseIndent();

					foreach (var memberOfRole in memberOfRoles)
					{
						writer.WriteMap("Role", memberOfRole);
					}

					writer.DecreaseIndent();
				}
			}
		}

		public string FileExtension => ".yml";
	}
}

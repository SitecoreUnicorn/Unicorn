using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Rainbow.Storage.Yaml;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Security.Serialization.ObjectModel;

namespace Unicorn.Users.Formatting
{
	public class YamlUserSerializationFormatter : IUserSerializationFormatter
	{
		public virtual SyncUser ReadSerializedUser(Stream dataStream, string serializedItemId)
		{
			Assert.ArgumentNotNull(dataStream, nameof(dataStream));

			try
			{
				using (var reader = new YamlReader(dataStream, 4096, true))
				{
					var user = new SyncUser();

					user.UserName = reader.ReadExpectedMap("Username");
					user.Email = reader.ReadExpectedMap("Email");
					user.Comment = reader.ReadExpectedMap("Comment");
					user.CreationDate = DateTime.ParseExact(reader.ReadExpectedMap("Created"), "o", CultureInfo.InvariantCulture, DateTimeStyles.None);
					user.IsApproved = bool.Parse(reader.ReadExpectedMap("IsApproved"));

					var propertiesNode = reader.PeekMap();
					if (propertiesNode.HasValue && propertiesNode.Value.Key.Equals("Properties"))
					{
						reader.ReadMap();
						while (true)
						{
							var propertyName = reader.PeekMap();

							if (propertyName == null || !propertyName.Value.Key.Equals("Key")) break;

							reader.ReadMap();

							var rawValue = reader.ReadExpectedMap("Value");
							var valueTypeString = reader.ReadExpectedMap("ValueType");

							var value = ReadPropertyValueObject(propertyName.Value.Value, valueTypeString, rawValue);

							bool propertyIsCustom = bool.Parse(reader.ReadExpectedMap("IsCustom"));

							user.ProfileProperties.Add(new SyncProfileProperty(propertyName.Value.Value, value, propertyIsCustom));
						}
					}

					var rolesNode = reader.PeekMap();

					if (rolesNode.HasValue && rolesNode.Value.Key.Equals("Roles"))
					{
						reader.ReadMap();

						while (true)
						{
							var roleName = reader.ReadMap();

							if (string.IsNullOrWhiteSpace(roleName?.Value)) break;

							user.Roles.Add(roleName.Value.Value);
						}
					}

					return user;
				}
			}
			catch (Exception exception)
			{
				throw new YamlFormatException("Error parsing YAML " + serializedItemId, exception);
			}
		}

		public virtual void WriteSerializedUser(SyncUser userData, Stream outputStream)
		{
			Assert.ArgumentNotNull(userData, nameof(userData));
			Assert.ArgumentNotNull(outputStream, "outputStream");

			using (var writer = new YamlWriter(outputStream, 4096, true))
			{
				writer.WriteMap("Username", userData.UserName);
				writer.WriteMap("Email", userData.Email);
				writer.WriteMap("Comment", userData.Comment ?? string.Empty);
				writer.WriteMap("Created", userData.CreationDate.ToString("O"));
				writer.WriteMap("IsApproved", userData.IsApproved.ToString());

				if (userData.ProfileProperties.Any())
				{
					writer.WriteMap("Properties");
					writer.IncreaseIndent();

					userData.ProfileProperties.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
					userData.ProfileProperties.ForEach(profileProperty =>
					{
						writer.WriteBeginListItem("Key", profileProperty.Name);
						writer.WriteMap("Value", GetSerializedProfileContent(profileProperty));
						writer.WriteMap("ValueType", profileProperty.Content.GetType().AssemblyQualifiedName);
						writer.WriteMap("IsCustom", profileProperty.IsCustomProperty.ToString());
					});

					writer.DecreaseIndent();
				}

				if (userData.Roles.Any())
				{
					userData.Roles.Sort();

					writer.WriteMap("Roles");
					writer.IncreaseIndent();

					userData.Roles.ForEach(roleName =>
					{
						writer.WriteMap("MemberOf", roleName);
					});

					writer.DecreaseIndent();
				}
			}
		}

		protected virtual object ReadPropertyValueObject(string propertyName, string typeString, string encodedValue)
		{
			Type type = Type.GetType(typeString);

			if(type == null) throw new InvalidOperationException("Unable to resolve profile item type " + typeString);

			if (type.IsPrimitive || type == typeof(string))
			{
				switch (Type.GetTypeCode(type))
				{
					case TypeCode.Int32:
						return int.Parse(encodedValue);
					case TypeCode.Double:
						return double.Parse(encodedValue);
					case TypeCode.Decimal:
						return decimal.Parse(encodedValue);
					case TypeCode.DateTime:
						return DateUtil.ToUniversalTime(DateTime.Parse(encodedValue));
					case TypeCode.String:
						return encodedValue;
					case TypeCode.Empty:
						return null;
					case TypeCode.Boolean:
						return bool.Parse(encodedValue);
					default:
						throw new Exception($"Can't read the content of the property '{propertyName}'");
				}
			}

			var binaryFormatter = new BinaryFormatter();
			var memoryStream = new MemoryStream(System.Convert.FromBase64String(encodedValue));

			try
			{
				return memoryStream.Length != 0 ? binaryFormatter.Deserialize(memoryStream) : null;
			}
			catch (SerializationException ex)
			{
				throw new Exception($"Can't deserialize the content of the property '{propertyName}'", ex);
			}
			finally
			{
				memoryStream.Close();
			}
		}

		protected virtual string GetSerializedProfileContent(SyncProfileProperty profileProperty)
		{
			if (profileProperty.Content == null)
				return string.Empty;

			if (profileProperty.Content.GetType().IsPrimitive || profileProperty.Content is string)
				return SerializePrimitiveProfileValue(profileProperty);

			if (profileProperty.Content.GetType().IsSerializable)
				return SerializeSerializableProfileValue(profileProperty);

			throw new Exception($"Can't serialize the profile property. Property name: {profileProperty.Name}");
		}

		protected virtual string SerializePrimitiveProfileValue(SyncProfileProperty profileProperty)
		{
			string value = profileProperty.Content.ToString();

			if (value == null)
				throw new SerializationException($"Can't serialize the profile property. Property name: {profileProperty.Name}");

			return value;
		}

		protected virtual string SerializeSerializableProfileValue(SyncProfileProperty profileProperty)
		{
			MemoryStream memoryStream = new MemoryStream();
			try
			{
				new BinaryFormatter().Serialize(memoryStream, profileProperty.Content);
				return System.Convert.ToBase64String(memoryStream.ToArray());
			}
			catch (SerializationException ex)
			{
				throw new SerializationException($"Can't serialize the profile property. Property name: {profileProperty.Name}", ex);
			}
			finally
			{
				memoryStream.Close();
			}
		}

		public string FileExtension => ".yml";
	}
}

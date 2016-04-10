using System.IO;
using FluentAssertions;
using Unicorn.Roles.Formatting;
using Unicorn.Roles.Model;
using Xunit;

namespace Unicorn.Roles.Tests.Formatting
{
	public class YamlRoleSerializationFormatterTests
	{
		[Fact]
		public void ShouldFormatSingleRole_AsExpected()
		{
			var sut = new YamlRoleSerializationFormatter();

			using (var ms = new MemoryStream())
			{
				sut.WriteSerializedRole(new SerializedRoleData("Test", new string[0], "Foo"), ms);

				ms.Seek(0, SeekOrigin.Begin);

				using (var reader = new StreamReader(ms))
				{
					var yml = reader.ReadToEnd();

					yml.Should().Be(@"---
Role: Test
");
				}
			}
		}

		[Fact]
		public void ShouldFormatRoleWithParents_AsExpected()
		{
			var sut = new YamlRoleSerializationFormatter();

			using (var ms = new MemoryStream())
			{
				sut.WriteSerializedRole(new SerializedRoleData("Test", new [] { "Foo", "Foo-Bar" }, "Foo"), ms);

				ms.Seek(0, SeekOrigin.Begin);

				using (var reader = new StreamReader(ms))
				{
					var yml = reader.ReadToEnd();

					yml.Should().Be(@"---
Role: Test
MemberOf:
  Role: Foo
  Role: ""Foo-Bar""
");
				}
			}
		}
	}
}

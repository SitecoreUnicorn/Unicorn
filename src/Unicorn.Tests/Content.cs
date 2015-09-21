using System.Reflection;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.Xunit2;
using Sitecore.FakeDb.AutoFixture;

namespace Unicorn.Tests
{
	internal class ContentAttribute : CustomizeAttribute
	{
		public override ICustomization GetCustomization(ParameterInfo parameter)
		{
			return new AutoContentCustomization();
		}
	}
}

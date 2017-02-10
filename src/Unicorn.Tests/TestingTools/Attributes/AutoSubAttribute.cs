using Ploeh.AutoFixture.AutoNSubstitute;
using Ploeh.AutoFixture.Xunit2;

namespace Unicorn.Tests.TestingTools.Attributes
{
	public class AutoSubAttribute : AutoDataAttribute
	{
		public AutoSubAttribute()
		{
			Fixture.Customize(new AutoNSubstituteCustomization());
		}
	}
}
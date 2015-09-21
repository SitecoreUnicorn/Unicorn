using Ploeh.AutoFixture;
using Ploeh.AutoFixture.Xunit2;
using Sitecore.FakeDb.AutoFixture;

namespace Unicorn.Tests
{
	internal class AutoDbDataAttribute : AutoDataAttribute
	{
		public AutoDbDataAttribute()
		  : base(new Fixture().Customize(new AutoDbCustomization()))
		{
		}
	}
}

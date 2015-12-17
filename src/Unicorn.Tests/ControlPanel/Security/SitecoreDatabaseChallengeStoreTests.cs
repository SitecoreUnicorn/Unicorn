using System.Threading;
using FluentAssertions;
using MicroCHAP.Server;
using Sitecore.FakeDb;
using Unicorn.ControlPanel.Security;
using Xunit;

namespace Unicorn.Tests.ControlPanel.Security
{
	public class SitecoreDatabaseChallengeStoreTests
	{
		[Theory, AutoDbData]
		public void ConsumeChallenge_ShouldReturnFalseIfChallengeDoesNotExist(Db db)
		{
			var store = CreateTestStore(db);

			store.ConsumeChallenge("FAKE").Should().BeFalse();
		}

		[Theory, AutoDbData]
		public void ConsumeChallenge_ShouldReturnFalseIfChallengeIsTooOld(Db db)
		{
			var store = CreateTestStore(db);

			store.AddChallenge("FAKE", 500);

			Thread.Sleep(550);

			store.ConsumeChallenge("FAKE").Should().BeFalse();
		}

		[Theory, AutoDbData]
		public void ConsumeChallenge_ShouldReturnTrue_IfTokenIsValid(Db db)
		{
			var store = CreateTestStore(db);

			store.AddChallenge("FAKE", 1000);

			store.ConsumeChallenge("FAKE").Should().BeTrue();
		}

		[Theory, AutoDbData]
		public void ConsumeChallenge_ShouldNotAllowReusingTokens(Db db)
		{
			var store = CreateTestStore(db);

			store.AddChallenge("FAKE", 1000);

			store.ConsumeChallenge("FAKE").Should().BeTrue();
			store.ConsumeChallenge("FAKE").Should().BeFalse();
		}

		private IChallengeStore CreateTestStore(Db db)
		{
			// fakedb doesn't like it when we create our own template with the item API. So we'll prep that in advance.
			db.Add(new DbTemplate("Authentication Challenge")
			{
				new DbField("Expires")
			});

			return new TestSitecoreDatabaseChallengeStore();
		}

		private class TestSitecoreDatabaseChallengeStore : SitecoreDatabaseChallengeStore
		{
			public TestSitecoreDatabaseChallengeStore() : base("master")
			{
			}

			protected override string TemplateParent { get { return "/sitecore/templates"; } }
		}
	}
}

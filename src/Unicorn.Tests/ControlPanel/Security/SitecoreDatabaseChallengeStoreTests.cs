// Unfortunately FakeDb doesn't understand it when you're creating templates dynamically
// so it doesn't know how to test this challenge store

//using System.Threading;
//using FluentAssertions;
//using MicroCHAP.Server;
//using Sitecore.Data;
//using Unicorn.ControlPanel.Security;
//using Xunit;

//namespace Unicorn.Tests.ControlPanel.Security
//{
//	public class InMemoryChallengeStoreTests
//	{
//		[Theory, AutoDbData]
//		public void ConsumeChallenge_ShouldReturnFalseIfChallengeDoesNotExist(Database db)
//		{
//			var store = CreateTestStore(db);

//			store.ConsumeChallenge("FAKE").Should().BeFalse();
//		}

//		[Theory, AutoDbData]
//		public void ConsumeChallenge_ShouldReturnFalseIfChallengeIsTooOld(Database db)
//		{
//			var store = CreateTestStore(db);

//			store.AddChallenge("FAKE", 100);

//			Thread.Sleep(150);

//			store.ConsumeChallenge("FAKE").Should().BeFalse();
//		}

//		[Theory, AutoDbData]
//		public void ConsumeChallenge_ShouldReturnTrue_IfTokenIsValid(Database db)
//		{
//			var store = CreateTestStore(db);

//			store.AddChallenge("FAKE", 100);

//			store.ConsumeChallenge("FAKE").Should().BeTrue();
//		}

//		[Theory, AutoDbData]
//		public void ConsumeChallenge_ShouldNotAllowReusingTokens(Database db)
//		{
//			var store = CreateTestStore(db);

//			store.AddChallenge("FAKE", 100);

//			store.ConsumeChallenge("FAKE").Should().BeTrue();
//			store.ConsumeChallenge("FAKE").Should().BeFalse();
//		}

//		private IChallengeStore CreateTestStore(Database db)
//		{

//			return new TestSitecoreDatabaseChallengeStore(db.n);
//		}

//		private class TestSitecoreDatabaseChallengeStore : SitecoreDatabaseChallengeStore
//		{
//			public TestSitecoreDatabaseChallengeStore(string databaseName) : base(databaseName)
//			{
//			}

//			// path that exists in FakeDb by default
//			protected override string TemplateParent => "/sitecore/templates";
//		}
//	}
//}
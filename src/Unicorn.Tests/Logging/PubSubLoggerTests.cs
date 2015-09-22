using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NSubstitute;
using Unicorn.Logging;
using Xunit;

namespace Unicorn.Tests.Logging
{
	public class PubSubLoggerTests
	{
		[Fact]
		public void ShouldPushInfoToSubscriber()
		{
			var log = new PubSubLogger();
			var target = Substitute.For<ILogger>();

			log.RegisterSubscriber(target);

			log.Info("LOL");

			target.Received().Info("LOL");
		}

		[Fact]
		public void ShouldPushInfoToSubscribers()
		{
			var log = new PubSubLogger();
			var target = Substitute.For<ILogger>();
			var target2 = Substitute.For<ILogger>();

			log.RegisterSubscriber(target);
			log.RegisterSubscriber(target2);

			log.Info("LOL");

			target.Received().Info("LOL");
			target2.Received().Info("LOL");
		}

		[Fact]
		public void ShouldPushDebugToSubscriber()
		{
			var log = new PubSubLogger();
			var target = Substitute.For<ILogger>();

			log.RegisterSubscriber(target);

			log.Debug("LOL");

			target.Received().Debug("LOL");
		}

		[Fact]
		public void ShouldPushWarnToSubscriber()
		{
			var log = new PubSubLogger();
			var target = Substitute.For<ILogger>();

			log.RegisterSubscriber(target);

			log.Warn("LOL");

			target.Received().Warn("LOL");
		}

		[Fact]
		public void ShouldPushErrorToSubscriber()
		{
			var log = new PubSubLogger();
			var target = Substitute.For<ILogger>();

			log.RegisterSubscriber(target);

			log.Error("LOL");

			target.Received().Error("LOL");
		}

		[Fact]
		public void ShouldPushExceptionToSubscriber()
		{
			var log = new PubSubLogger();
			var target = Substitute.For<ILogger>();
			var exception = new Exception();

			log.RegisterSubscriber(target);

			log.Error(exception);

			target.Received().Error(exception);
		}

		[Fact]
		public void ShouldNotPushInfoToSubscriber_WhenTransactionInScope()
		{
			var log = new PubSubLogger();
			var target = Substitute.For<ILogger>();

			log.RegisterSubscriber(target);

			using (new LogTransaction(log))
			{
				log.Info("LOL");
				target.DidNotReceive().Info("LOL");
			}

			target.Received().Info("LOL");
		}
	}
}

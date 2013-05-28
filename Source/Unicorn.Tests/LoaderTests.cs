using Kamsar.WebConsole;
using Moq;
using NUnit.Framework;
using Sitecore.Configuration;
using Unicorn.Evaluators;
using Unicorn.Predicates;
using Unicorn.Serialization;

namespace Unicorn.Tests
{
	[TestFixture]
	public class LoaderTests
	{
		[Test]
		public void Loader_ReportsError_WhenRootDoesNotExist()
		{
			const string root = "/fake/path";
			const string db = "master";

			var serializationProvider = new Mock<ISerializationProvider>();
			serializationProvider.Setup(x => x.GetReference(root, db)).Returns((ISerializedReference)null);

			var loader = new SerializationLoader(serializationProvider.Object, new Mock<IPredicate>().Object, new Mock<IEvaluator>().Object);

			var progress = new StringProgressStatus();

			loader.LoadTree(Factory.GetDatabase(db), root, progress);

			Assert.IsTrue(progress.Output.Contains("was unable to find a root serialized item for"));
		}

		[Test]
		public void Loader_Retries_RetryableSingleItemFailure()
		{
			
		}

		[Test]
		public void Loader_Retries_RetryableReferenceFailure()
		{
			
		}

		[Test]
		public void Loader_Retries_StopsOnUnresolvableError()
		{
			
		}

		[Test]
		public void Loader_SkipsRootWhenExcluded()
		{
			
		}

		[Test]
		public void Loader_SkipsChildOfRootWhenExcluded()
		{
			
		}

		[Test]
		public void Loader_WarnsIfSkippedItemExistsInSerializationProvider()
		{

		}

		[Test]
		public void Loader_LoadsTemplatesFirst()
		{
			
		}

		[Test]
		public void Loader_IdentifiesOrphanChildItem()
		{
			
		}

		[Test]
		public void Loader_DoesNotIdentifyValidChildrenAsOrphans()
		{
			
		}

		[Test] 
		public void Loader_DoesNotIdentifyStandardValuesItemsAsOrphans()
		{

		}

		[Test]
		public void Loader_DoesNotIdentifySkippedItemsAsOrphans()
		{

		}

		[Test]
		public void Loader_UpdatesItemWhenEvaluatorAllows()
		{
			
		}

		[Test]
		public void Loader_DoesNotUpdateItemWhenEvaluatorDenies()
		{
			
		}

		[Test]
		public void Loader_UpdatesWhenItemDoesNotExistInSitecore()
		{
			
		}
	}
}

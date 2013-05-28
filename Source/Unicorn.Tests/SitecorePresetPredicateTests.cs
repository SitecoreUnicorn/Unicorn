using NUnit.Framework;

namespace Unicorn.Tests
{
	[TestFixture]
	public class SitecorePresetPredicateTests
	{
		/*
		 config:
		 <include database="master" path="/sitecore/layout/Simulators">
					<exclude path="/sitecore/layout/Simulators/Android Phone" />
					<exclude template="No Silverlight Support Trait" />
					<exclude templateid="{317ADE1D-337A-464A-B7D0-06B4424FC0EA}" /> <!-- no flash support trait -->
					<exclude id="{E1DC505A-F86F-4C05-B409-AE2246AD3441}" /> <!-- blackberry sim item -->
		 </include>
		  
		 example code:
		 var predicate = SerializationUtility.GetDefaultPreset();
			var provider = new SitecoreSerializationProvider();

			var roots = predicate.GetRootItems();

			// check if root paths included
			foreach (var root in roots)
			{
				Assert.IsTrue(predicate.Includes(root).IsIncluded, "Root path was not included");
			}

			// check if exclude by path works
			var includes = predicate.Includes(provider.GetReference("/sitecore/layout/Simulators/Android Phone", "master"));
			Assert.IsFalse(includes.IsIncluded, "Exclude by path failed.");

			// check if exclude by template ID works
			var includes2 = predicate.Includes(provider.GetItem(provider.GetReference("/sitecore/layout/Simulators/iPad/No Flash Support", "master")));
			Assert.IsFalse(includes2.IsIncluded, "Exclude by template ID failed.");

			// check if exclude by template name works
			var includes3 = predicate.Includes(provider.GetItem(provider.GetReference("/sitecore/layout/Simulators/iPad/No Silverlight Support", "master")));
			Assert.IsFalse(includes3.IsIncluded, "Exclude by template name failed.");

			// check if exclude by item id works
			var includes4 = predicate.Includes(provider.GetItem(provider.GetReference("/sitecore/layout/Simulators/Blackberry", "master")));
			Assert.IsFalse(includes4.IsIncluded, "Exclude by item id failed.");*/
	}
}

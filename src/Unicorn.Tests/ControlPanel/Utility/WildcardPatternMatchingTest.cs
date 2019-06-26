using FluentAssertions;
using Unicorn.ControlPanel;
using Xunit;

namespace Unicorn.Tests.ControlPanel.Utility
{
	public class WildcardPatternMatchingTest
	{
		[Fact]
		public void EnsurePatternMatchingTest()
		{
			// Currently, a case-sensitive matching is expected throughout the solution

			ControlPanelUtility.WildcardMatch("Test", "Test").Should().BeTrue();
			ControlPanelUtility.WildcardMatch("test", "Test").Should().BeFalse();
			ControlPanelUtility.WildcardMatch("Test", "Te*").Should().BeTrue();
			ControlPanelUtility.WildcardMatch("test", "Te*").Should().BeFalse();
			ControlPanelUtility.WildcardMatch("Test", "Tes?").Should().BeTrue();
			ControlPanelUtility.WildcardMatch("test", "Tes?").Should().BeFalse();
			ControlPanelUtility.WildcardMatch("Test", "?e*").Should().BeTrue();
			ControlPanelUtility.WildcardMatch("Test", "????").Should().BeTrue();
			ControlPanelUtility.WildcardMatch("Test", "???").Should().BeFalse();
			ControlPanelUtility.WildcardMatch("Test", "*").Should().BeTrue();
			ControlPanelUtility.WildcardMatch("Test", "*t").Should().BeTrue();
			ControlPanelUtility.WildcardMatch("Foundation.Serialization.FancyPants", "Foundation.*").Should().BeTrue();
			ControlPanelUtility.WildcardMatch("Foundation.Serialization.FancyPants", "*.FancyPants").Should().BeTrue();
			ControlPanelUtility.WildcardMatch("Foundation.Serialization.FancyPants", "*.Fancy?ants").Should().BeTrue();
			ControlPanelUtility.WildcardMatch("Foundation.Serialization.FancyPants", "*.Fancypants").Should().BeFalse();
		}
	}
}

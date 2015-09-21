using FluentAssertions;
using Unicorn.ControlPanel.Headings;
using Xunit;

namespace Unicorn.Tests.ControlPanel.Headings
{
	public class HeadingServiceTests
	{
		[Fact]
		public void ShouldReturnValidControlPanelHeading()
		{
			var service = new HeadingService();

			service.GetControlPanelHeadingHtml().Should().NotBeNullOrWhiteSpace();
		}

		[Fact]
		public void ShouldReturnValidHeading()
		{
			var service = new HeadingService();

			service.GetHeadingHtml().Should().NotBeNullOrWhiteSpace();
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kamsar.WebConsole;
using NSubstitute;
using Ploeh.AutoFixture.AutoNSubstitute;
using Unicorn.ControlPanel;
using Xunit;

namespace Unicorn.Tests.ControlPanel.Utility
{
	public class ProgressBarTests
	{
		[Fact]
		public void ProgressBarCalculationTests()
		{
			var progress = Substitute.For<IProgressStatus>();
			WebConsoleUtility.SetTaskProgress(progress, 1, 201, (int)((1 / (double)201) * 100));
		}
	}
}

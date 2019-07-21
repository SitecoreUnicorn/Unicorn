using System;
using Kamsar.WebConsole;

namespace Unicorn.ControlPanel
{
	public static class WebConsoleUtility
	{
		/// <summary>
		/// Sets the progress of the whole based on the progress within a sub-task of the main progress (e.g. 0-100% of a task within the global range of 0-20%)
		/// </summary>
		/// <param name="progress"></param>
		/// <param name="taskNumber">The index of the current sub-task</param>
		/// <param name="totalTasks">The total number of sub-tasks</param>
		/// <param name="taskPercent">The percentage complete of the sub-task (0-100)</param>
		public static void SetTaskProgress(IProgressStatus progress, int taskNumber, int totalTasks, int taskPercent)
		{
			if (taskNumber < 1) throw new ArgumentException("taskNumber must be 1 or more");
			if (totalTasks < 1) throw new ArgumentException("totalTasks must be 1 or more");
			if (taskNumber > totalTasks) throw new ArgumentException("taskNumber was greater than the number of totalTasks!");

			int start = (int)Math.Round(((taskNumber - 1) / (double)totalTasks) * 100d);
			int end = start + (int)Math.Round(0.5d + ((1d / totalTasks) * 100d));

			SetRangeTaskProgress(progress, Math.Max(start, 0), Math.Min(end, 100), taskPercent);
		}

		/// <summary>
		/// Sets the progress of the whole based on the progress within a percentage range of the main progress (e.g. 0-100% of a task within the global range of 0-20%)
		/// </summary>
		/// <param name="progress"></param>
		/// <param name="startPercentage">The percentage the task began at</param>
		/// <param name="endPercentage">The percentage the task ends at</param>
		/// <param name="taskPercent">The percentage complete of the sub-task (0-100)</param>
		private static void SetRangeTaskProgress(IProgressStatus progress, int startPercentage, int endPercentage, int taskPercent)
		{
			int range = endPercentage - startPercentage;

			if (range <= 0) throw new ArgumentException("endPercentage must be greater than startPercentage");

			int offset = (int)Math.Round(range * (taskPercent / 100d));

			progress.Report(Math.Min(startPercentage + offset, 100));
		}
	}
}

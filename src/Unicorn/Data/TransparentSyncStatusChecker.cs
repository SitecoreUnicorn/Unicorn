using System;
using System.Configuration;
using System.Data.SqlClient;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;

namespace Unicorn.Data
{
	public static class TransparentSyncStatusChecker
	{
		/// <summary>
		/// Determines if an item, which could be in Transparent Sync, is in the SC DB as well.
		/// This direct SQL is necessary because DatabaseCacheDisabler appears to no longer work,
		/// and always returns that the item is in the DB when it's active even if it doesn't.
		/// </summary>
		public static bool IsInDatabase(Item item)
		{
			try
			{
				using (var sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings[item.Database.ConnectionStringName].ConnectionString))
				{
					sqlConnection.Open();
					using (var sqlCommand = new SqlCommand("SELECT COUNT(*) FROM Items WHERE Id = @ItemId"))
					{
						sqlCommand.Parameters.AddWithValue("ItemId", item.ID.Guid.ToString());

						sqlCommand.Connection = sqlConnection;

						return (int) sqlCommand.ExecuteScalar() > 0;
					}
				}
			}
			catch(Exception ex)
			{
				Log.Warn("Error determining if Transparent Sync item was present in the database", ex, typeof(TransparentSyncStatusChecker));
				return false;
			}
		}
	}
}

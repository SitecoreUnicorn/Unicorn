using System;
using MicroCHAP.Server;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.SecurityModel;

namespace Unicorn.ControlPanel.Security
{
	/// <summary>
	/// Stores MicroCHAP authentication challenges in the Sitecore database. Completely self-bootstrapping, will make its own template and all.
	/// Database storage is desirable because in a server environment with more than one server (like a clustered authoring environment in Azure), in memory challenge storage will not work well.
	/// Assumptions:
	/// 1. You're issuing challenges that are valid Sitecore item names (e.g. guids)
	/// 2. Your challenge issuance rate is not ridiculously high (this won't scale to massive constant usage, but is fine for dev/deploy stuff)
	/// </summary>
	public class SitecoreDatabaseChallengeStore : IChallengeStore
	{
		private readonly string _databaseName;
		private readonly IChallengeStoreLogger _challengeStoreLogger;

		protected virtual string ParentPath => "/sitecore/system";

		protected virtual string ChildName => "Authentication Challenges";

		protected virtual string TemplateParent => "/sitecore/templates/system";

		protected TemplateID ChallengeTemplateId { get; set; }

		public SitecoreDatabaseChallengeStore(string databaseName)
		{
			_databaseName = databaseName;

			EnsureTemplateExists();
			EnsurePathExists();
		}

		public SitecoreDatabaseChallengeStore(string databaseName, IChallengeStoreLogger challengeStoreLogger) : this(databaseName)
		{
			_challengeStoreLogger = challengeStoreLogger;
		}

		public void AddChallenge(string challenge, int expirationTimeInMsec)
		{
			using (new SecurityDisabler())
			{
				EnsureTemplateExists();

				var challengeItem = RootItem.Add("AUTH" + challenge, ChallengeTemplateId);
				challengeItem.Editing.BeginEdit();
				
				challengeItem["Expires"] = DateTime.UtcNow.AddMilliseconds(expirationTimeInMsec).Ticks.ToString();

				challengeItem.Editing.EndEdit();
			}
		}

		public bool ConsumeChallenge(string challenge)
		{
			CleanupExpiredTokens();

			using (new SecurityDisabler())
			{
				var existingChallenge = RootItem.Children["AUTH" + challenge];

				if (existingChallenge == null)
				{
					_challengeStoreLogger?.ChallengeUnknown(challenge);
					return false;
				}

				// we know the token's timestamp was valid because we cleaned up expired tokens before getting it

				existingChallenge.Delete(); // prevent reuse

				return true;
			}
		}

		protected virtual void CleanupExpiredTokens()
		{
			using (new SecurityDisabler())
			{
				var challenges = RootItem.GetChildren();
				foreach (Item challenge in challenges)
				{
					long expiresTicks;

					// if the value in the field is not a valid long (ticks) value, or the value is valid but too old, we kill it
					if (!long.TryParse(challenge["Expires"], out expiresTicks) || expiresTicks < DateTime.UtcNow.Ticks)
					{
						// challenge name starts with 'AUTH'
						_challengeStoreLogger?.ChallengeExpired(challenge.Name.Substring(4));
						challenge.Delete();
					}
				}
			}
		}

		protected virtual Item EnsurePathExists()
		{
			using (new SecurityDisabler())
			{
				var parent = Database.GetItem(ParentPath);
				if (parent == null) throw new InvalidOperationException("Parent path did not exist.");

				var child = parent.Children[ChildName];
				if (child == null) return parent.Add(ChildName, new TemplateID(TemplateIDs.Folder));

				return child;
			}
		}

		protected virtual void EnsureTemplateExists()
		{
			using (new SecurityDisabler())
			{
				var parent = Database.GetItem(TemplateParent);
				if (parent == null) throw new InvalidOperationException("Template parent path did not exist.");

				var existingTemplate = parent.Children["Authentication Challenge"];

				if (existingTemplate != null)
				{
					ChallengeTemplateId = new TemplateID(existingTemplate.ID);
					return;
				}

				var template = parent.Add("Authentication Challenge", new TemplateID(TemplateIDs.Template));
				var section = template.Add("Challenge", new TemplateID(TemplateIDs.TemplateSection));
				var expirationField = section.Add("Expires", new TemplateID(TemplateIDs.TemplateField));

				expirationField.Editing.BeginEdit();
				{
					expirationField[TemplateFieldIDs.Shared] = "1";
				}
				expirationField.Editing.EndEdit();

				ChallengeTemplateId = new TemplateID(template.ID);
			}
		}

		protected virtual Item RootItem
		{
			get
			{
				var rootItem = Database.GetItem(ParentPath + "/" + ChildName);
				if (rootItem == null) return EnsurePathExists();

				return rootItem;
			}
		}

		protected virtual Database Database => Factory.GetDatabase(_databaseName);
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Configuration;
using Unicorn.Predicates.Fields;
using Xunit;

namespace Unicorn.Tests.Predicates
{
	public class FieldTransformsTests
	{
		[Fact]
		public void ExcludeFieldFilterTests()
		{
			var fieldFilter = "-Electronic Arts";
			var ffCollection = MagicTokenTransformer.GetFieldTransforms(fieldFilter);

			Assert.NotNull(ffCollection.Transforms);
			Assert.Equal(1, ffCollection.Transforms.Length);

			var ff = ffCollection.Transforms.First();
			Assert.Equal("Electronic Arts", ff.FieldName);

			Assert.False(ff.ShouldDeployFieldValue("P2W", "Lootboxes"), "ExcludeFilter wanted to deploy field (value,value)");
			Assert.False(ff.ShouldDeployFieldValue(null, "Lootboxes"), "ExcludeFilter wanted to deploy field (null, value)");
			Assert.False(ff.ShouldDeployFieldValue("P2W", null), "ExcludeFilter wanted to deploy field (value,null)");
			Assert.False(ff.ShouldDeployFieldValue(null, null), "ExcludeFilter wanted to deploy field (null, null)");

			// Calling GetResult on an explicitly ignored field should unconditionally fail as all calls to ShouldDeployFieldValue() would have returned false
			Assert.Throws(typeof(InvalidOperationException), () => ff.GetFieldValue("P2W", "Lootboxes"));
			Assert.Throws(typeof(InvalidOperationException), () => ff.GetFieldValue("P2W", null));
			Assert.Throws(typeof(InvalidOperationException), () => ff.GetFieldValue(null, "Lootboxes"));
			Assert.Throws(typeof(InvalidOperationException), () => ff.GetFieldValue(null, null));
		}

		[Fact]
		public void OnlyIfNullOrEmptyFieldFilterTests()
		{
			var fieldFilter = "?Electronic Arts";
			var ffCollection = MagicTokenTransformer.GetFieldTransforms(fieldFilter);

			Assert.NotNull(ffCollection.Transforms);
			Assert.Equal(1, ffCollection.Transforms.Length);

			var ff = ffCollection.Transforms.First();
			Assert.Equal("Electronic Arts", ff.FieldName);

			Assert.False(ff.ShouldDeployFieldValue("P2W", "Lootboxes"), "OnlyIfNullOrEmptyFilter wanted to deploy field (value,value)");
			Assert.True(ff.ShouldDeployFieldValue(null, "Lootboxes"), "OnlyIfNullOrEmptyFilter did not want to deploy field (null, value)");
			Assert.False(ff.ShouldDeployFieldValue("P2W", null), "OnlyIfNullOrEmptyFilter wanted to deploy field (value,null)");
			Assert.True(ff.ShouldDeployFieldValue(null, null), "OnlyIfNullOrEmptyFilter did not want to deploy field (null, null)");

			Assert.Throws(typeof(InvalidOperationException), () =>  ff.GetFieldValue("P2W", "Lootboxes"));
			Assert.Equal("Lootboxes", ff.GetFieldValue(null, "Lootboxes"));
			Assert.Throws(typeof(InvalidOperationException), () => ff.GetFieldValue("P2W", null));
			Assert.Null(ff.GetFieldValue(null, null));
		}

		[Fact]
		public void ClearFieldFilterTests()
		{
			var fieldFilter = "!Electronic Arts";
			var ffCollection = MagicTokenTransformer.GetFieldTransforms(fieldFilter);

			Assert.NotNull(ffCollection.Transforms);
			Assert.Equal(1, ffCollection.Transforms.Length);

			var ff = ffCollection.Transforms.First();
			Assert.Equal("Electronic Arts", ff.FieldName);

			Assert.True(ff.ShouldDeployFieldValue("P2W", "Lootboxes"), "ClearFilter did not want to deploy field (value,value)");
			Assert.True(ff.ShouldDeployFieldValue(null, "Lootboxes"), "ClearFilter did not want to deploy field (null, value)");
			Assert.True(ff.ShouldDeployFieldValue("P2W", null), "ClearFilter did not want to deploy field (value,null)");
			Assert.True(ff.ShouldDeployFieldValue(null, null), "ClearFilter did not want to deploy field (null, null)");

			Assert.Null(ff.GetFieldValue("P2W", "Lootboxes"));
			Assert.Null(ff.GetFieldValue(null, "Lootboxes"));
			Assert.Null(ff.GetFieldValue("P2W", null));
			Assert.Null(ff.GetFieldValue(null, null));
		}

		[Fact]
		public void ScreamingSnakeFilterTests()
		{
			var fieldFilter = "~Electronic Arts";
			var ffCollection = MagicTokenTransformer.GetFieldTransforms(fieldFilter);

			Assert.NotNull(ffCollection.Transforms);
			Assert.Equal(1, ffCollection.Transforms.Length);

			var ff = ffCollection.Transforms.First();
			Assert.Equal("Electronic Arts", ff.FieldName);

			// Screaming Snake only alters the field value, it should always deploy
			Assert.True(ff.ShouldDeployFieldValue("P2W", "Lootboxes"), "ScreamingSnakeFilter did not want to deploy field (value,value)");
			Assert.True(ff.ShouldDeployFieldValue(null, "Lootboxes"), "ScreamingSnakeFilter did not want to deploy field (null, value)");
			Assert.True(ff.ShouldDeployFieldValue("P2W", null), "ScreamingSnakeFilter did not want to deploy field (value,null)");
			Assert.True(ff.ShouldDeployFieldValue(null, null), "ScreamingSnakeFilter did not want to deploy field (null, null)");

			Assert.Equal("LOOTBOXES", ff.GetFieldValue("P2W", "Lootboxes"));
			Assert.Equal("LOOTBOXES", ff.GetFieldValue(null, "Lootboxes"));
			Assert.Null(ff.GetFieldValue("P2W", null));
			Assert.Null(ff.GetFieldValue(null, null));
			Assert.Equal("LOOTBOXES_R_BAD_MKAY", ff.GetFieldValue("P2W", "Lootboxes r bad mkay"));
		}

		[Fact]
		public void ForcedFieldValueFilterTests()
		{
			var fieldFilter = "+Electronic Arts[${settings:Lootboxes}]";
			var ffCollection = MagicTokenTransformer.GetFieldTransforms(fieldFilter);

			Assert.NotNull(ffCollection.Transforms);
			Assert.Equal(1, ffCollection.Transforms.Length);

			var ff = ffCollection.Transforms.First();
			Assert.Equal("Electronic Arts", ff.FieldName);
			Assert.Equal("${settings:Lootboxes}", ff.ForcedValue);

			// ForcedFieldValue always forces a value (duh), it should always deploy
			Assert.True(ff.ShouldDeployFieldValue("P2W", "Lootboxes"), "ForcedFieldValueFilter did not want to deploy field (value,value)");
			Assert.True(ff.ShouldDeployFieldValue(null, "Lootboxes"), "ForcedFieldValueFilter did not want to deploy field (null, value)");
			Assert.True(ff.ShouldDeployFieldValue("P2W", null), "ForcedFieldValueFilter did not want to deploy field (value,null)");
			Assert.True(ff.ShouldDeployFieldValue(null, null), "ForcedFieldValueFilter did not want to deploy field (null, null)");

			Assert.Equal("${settings:Lootboxes}", ff.GetFieldValue("P2W", "Lootboxes"));
			Assert.Equal("${settings:Lootboxes}", ff.GetFieldValue(null, "Lootboxes"));
			Assert.Equal("${settings:Lootboxes}", ff.GetFieldValue("P2W", null));
			Assert.Equal("${settings:Lootboxes}", ff.GetFieldValue(null, null));
		}

		[Fact]
		public void SitecoreSettingsFilterTests()
		{
			var fieldFilter = "$Electronic Arts[Configuration.Lootboxes]";
			var ffCollection = MagicTokenTransformer.GetFieldTransforms(fieldFilter);

			Assert.NotNull(ffCollection.Transforms);
			Assert.Equal(1, ffCollection.Transforms.Length);

			var ff = ffCollection.Transforms.First();
			Assert.Equal("Electronic Arts", ff.FieldName);
			Assert.Equal("Configuration.Lootboxes", ff.ForcedValue);

			// SitecoreSettings always forces a value (duh), it should always deploy
			Assert.True(ff.ShouldDeployFieldValue("P2W", "Lootboxes"), "ForcedFieldValueFilter did not want to deploy field (value,value)");
			Assert.True(ff.ShouldDeployFieldValue(null, "Lootboxes"), "ForcedFieldValueFilter did not want to deploy field (null, value)");
			Assert.True(ff.ShouldDeployFieldValue("P2W", null), "ForcedFieldValueFilter did not want to deploy field (value,null)");
			Assert.True(ff.ShouldDeployFieldValue(null, null), "ForcedFieldValueFilter did not want to deploy field (null, null)");

			using (new SettingsSwitcher("Configuration.Lootboxes", "false"))
			{
				Assert.Equal("false", ff.GetFieldValue("P2W", "Lootboxes"));
				Assert.Equal("false", ff.GetFieldValue(null, "Lootboxes"));
				Assert.Equal("false", ff.GetFieldValue("P2W", null));
				Assert.Equal("false", ff.GetFieldValue(null, null));
			}
		}

		[Fact]
		public void LoremIpsumBodyFilterTests()
		{
			var fieldFilter = ":Electronic Arts";
			var ffCollection = MagicTokenTransformer.GetFieldTransforms(fieldFilter);

			Assert.NotNull(ffCollection.Transforms);
			Assert.Equal(1, ffCollection.Transforms.Length);

			var ff = ffCollection.Transforms.First();
			Assert.Equal("Electronic Arts", ff.FieldName);

			// LoremIpsumBody always forces a value (duh), it should always deploy
			Assert.True(ff.ShouldDeployFieldValue("P2W", "Lootboxes"), "LoremIpsumBodyFilterTests did not want to deploy field (value,value)");
			Assert.True(ff.ShouldDeployFieldValue(null, "Lootboxes"), "LoremIpsumBodyFilterTests did not want to deploy field (null, value)");
			Assert.True(ff.ShouldDeployFieldValue("P2W", null), "LoremIpsumBodyFilterTests did not want to deploy field (value,null)");
			Assert.True(ff.ShouldDeployFieldValue(null, null), "LoremIpsumBodyFilterTests did not want to deploy field (null, null)");

			Assert.Equal(MagicTokenTransformer.LoremIpsumBody, ff.GetFieldValue("P2W", "Lootboxes"));
			Assert.Equal(MagicTokenTransformer.LoremIpsumBody, ff.GetFieldValue(null, "Lootboxes"));
			Assert.Equal(MagicTokenTransformer.LoremIpsumBody, ff.GetFieldValue("P2W", null));
			Assert.Equal(MagicTokenTransformer.LoremIpsumBody, ff.GetFieldValue(null, null));
		}

		[Fact]
		public void LoremIpsumTitleFilterTests()
		{
			var fieldFilter = ";Electronic Arts";
			var ffCollection = MagicTokenTransformer.GetFieldTransforms(fieldFilter);

			Assert.NotNull(ffCollection.Transforms);
			Assert.Equal(1, ffCollection.Transforms.Length);

			var ff = ffCollection.Transforms.First();
			Assert.Equal("Electronic Arts", ff.FieldName);

			// LoremIpsumTitle always forces a value (duh), it should always deploy
			Assert.True(ff.ShouldDeployFieldValue("P2W", "Lootboxes"), "LoremIpsumBodyFilterTests did not want to deploy field (value,value)");
			Assert.True(ff.ShouldDeployFieldValue(null, "Lootboxes"), "LoremIpsumBodyFilterTests did not want to deploy field (null, value)");
			Assert.True(ff.ShouldDeployFieldValue("P2W", null), "LoremIpsumBodyFilterTests did not want to deploy field (value,null)");
			Assert.True(ff.ShouldDeployFieldValue(null, null), "LoremIpsumBodyFilterTests did not want to deploy field (null, null)");

			Assert.Equal(MagicTokenTransformer.LoremIpsumTitle, ff.GetFieldValue("P2W", "Lootboxes"));
			Assert.Equal(MagicTokenTransformer.LoremIpsumTitle, ff.GetFieldValue(null, "Lootboxes"));
			Assert.Equal(MagicTokenTransformer.LoremIpsumTitle, ff.GetFieldValue("P2W", null));
			Assert.Equal(MagicTokenTransformer.LoremIpsumTitle, ff.GetFieldValue(null, null));
		}

		[Fact]
		public void FieldFilterParsingTests()
		{
			var fieldFilter = "+Electronic Arts[${settings:Lootboxes}]";
			var ffCollection = MagicTokenTransformer.GetFieldTransforms(fieldFilter);
			Assert.NotNull(ffCollection.Transforms);
			Assert.Equal(1, ffCollection.Transforms.Length);

			fieldFilter = "!Fisk,+Electronic Arts[${settings:Lootboxes}]";
			ffCollection = MagicTokenTransformer.GetFieldTransforms(fieldFilter);
			Assert.NotNull(ffCollection.Transforms);
			Assert.Equal(2, ffCollection.Transforms.Length);
			Assert.Equal("Fisk", ffCollection.Transforms[0].FieldName);
			Assert.Equal("Electronic Arts", ffCollection.Transforms[1].FieldName);

			fieldFilter = "~Activision,+Electronic Arts[${settings:Lootboxes}],~Ubisoft";
			ffCollection = MagicTokenTransformer.GetFieldTransforms(fieldFilter);
			Assert.NotNull(ffCollection.Transforms);
			Assert.Equal(3, ffCollection.Transforms.Length);
			Assert.Equal("Activision", ffCollection.Transforms[0].FieldName);
			Assert.Equal("Electronic Arts", ffCollection.Transforms[1].FieldName);
			Assert.Equal("Ubisoft", ffCollection.Transforms[2].FieldName);

			fieldFilter = "~Activision,+Electronic Arts[${settings:Lootboxes},~Ubisoft";
			Assert.Throws(typeof(MalformedFieldFilterException), () => MagicTokenTransformer.GetFieldTransforms(fieldFilter));

			fieldFilter = "~Activision,+Electronic Arts,~Ubisoft";
			Assert.Throws(typeof(MalformedFieldFilterException), () => MagicTokenTransformer.GetFieldTransforms(fieldFilter));

			fieldFilter = "~Activision,+Electronic Arts],~Ubisoft";
			Assert.Throws(typeof(MalformedFieldFilterException), () => MagicTokenTransformer.GetFieldTransforms(fieldFilter));

			fieldFilter = "~Activision,!Electronic Arts,~Ubisoft,~Activision";
			Assert.Throws(typeof(DuplicateFieldsException), () => MagicTokenTransformer.GetFieldTransforms(fieldFilter));

			fieldFilter = "   ~Activision,! Electronic Arts,  ~Ubisoft,  ~InterPlay      ";
			ffCollection = MagicTokenTransformer.GetFieldTransforms(fieldFilter);
			Assert.NotNull(ffCollection.Transforms);
			Assert.Equal(4, ffCollection.Transforms.Length);
			Assert.Equal("Activision", ffCollection.Transforms[0].FieldName);
			Assert.Equal("Electronic Arts", ffCollection.Transforms[1].FieldName);
			Assert.Equal("Ubisoft", ffCollection.Transforms[2].FieldName);
			Assert.Equal("InterPlay", ffCollection.Transforms[3].FieldName);
		}
	}
}

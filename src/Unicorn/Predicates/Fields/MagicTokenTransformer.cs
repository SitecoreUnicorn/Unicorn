using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rainbow.Storage;
using Sitecore.Pipelines.LoggingIn;
using Sitecore.StringExtensions;

namespace Unicorn.Predicates.Fields
{
	public enum FieldTransformDeployRule
	{
		OnlyIfNullOrEmpty,		// ?  Never overwrite an explicit field value
		Ignore,					// -  Ignore this field. (will still get serialized)
		Clear,					// !  Always reset this field
		ForceValue,				// +  Force [] value into field
		ScreamingSnake,			// ~  Force value into SCREAMING_SNAKE_CASE (undocumented)
		LoremIpsumTitle,        // ;  Force "Lorem ipsum dolor" value
		LoremIpsumBody,			// :  Force long Lorem Ipsom HTML value
		SitecoreSetting,		// $  Grab the Sitecore setting defined inside [] as the value
	}

	public class MagicTokenTransformer : IFieldValueTransformer
	{
		public const string LoremIpsumTitle = "Lorem ipsum dolor";
		public const string LoremIpsumBody = "<p>Lorem ipsum dolor. Sit amet condimentum. Rutrum perspiciatis et. Sagittis ultrices curabitur ut pellentesque massa. Fringilla urna placerat est suspendisse vitae neque eget sed nam.</p><p>Tempor feugiat et sed eros dictum. Nullam quis vivamus diam nam velit. Interdum praesent lacus. Enim gravida iaculis augue dolor lobortis. In lorem eu aliquam.</p><p>Quam porttitor lobortis ac id pede maecenas placerat sem hendrerit viverra leo velit ut convallis. Pharetra odio donec vehicula maecenas a quam semper morbi. Lorem velit sapien imperdiet eget vel eget interdum ut. Malesuada non mattis. Sed pellentesque proin. Sed nulla quis. Laoreet etiam sapien. Etiam lectus nunc. Aliquam facilisi et turpis metus mauris. Est felis magna. Ornare dolor elementum. Orci dolor dolor iaculis odio orci tortor nunc praesent mauris nulla nonummy. Ante mi luctus.</p><p>Excepturi posuere morbi pulvinar sodales orci. Mollis libero posuere. Morbi in nibh tellus vestibulum risus ac ac dui. Orci amet vel. Quam suspendisse diam adipiscing.</p><p>Nec sed id. Vivamus habitasse molestie egestas ante elit. Et ultricies placerat. Felis lacus ut lectus pulvinar mauris. Deleniti pellentesque etiam a vitae donec sollicitudin est duis ac potenti nec in sed ultricies. Consectetur id dolor dolor tellus pede. A montes vestibulum. Purus ac est. Metus nonummy tortor tellus velit.</p><p>Justo aliquam eu. In aenean nulla lobortis arcu lacinia. Lorem ipsum facilisis aliquam tempus nunc. Ut morbi et. Morbi tincidunt eros a ut nam ligula.</p><p>Tortor ullamcorper est elementum in purus. Ultricies viverra aenean. Pellentesque justo non. Mauris vestibulum in etiam nibh vitae et dignissim tellus. Ante luctus non odio ultrices sit bibendum donec auctor quis tellus feugiat. Omnis tortor lectus. Pede nunc praesent. At omnis etiam. Et rhoncus turpis risus platea metus risus consequat.</p><p>Vestibulum class malesuada potenti semper quia. Dolor condimentum quam. Donec nunc quisque. Nam sem sem a magna quis bibendum ultricies nam. Libero aliquam nibh nulla.</p>";

		public FieldTransformDeployRule FieldTransformDeployRule { get; set; }
		public string FieldName { get; set; }
		public string ForcedValue { get; set; }

		public static FieldTransformsCollection GetFieldTransforms(string fieldFilterDefinition, List<MagicTokenTransformer> filters = null)
		{
			List<MagicTokenTransformer> f = filters ?? new List<MagicTokenTransformer>();

			fieldFilterDefinition = fieldFilterDefinition.Trim();

			if (fieldFilterDefinition.Length < 1)
				return new FieldTransformsCollection(f.ToArray());

			MagicTokenTransformer ff = new MagicTokenTransformer();
			int commaIndex;

			switch (fieldFilterDefinition[0])
			{
				case '?':
					ff.FieldTransformDeployRule = FieldTransformDeployRule.OnlyIfNullOrEmpty;
					ff.FieldName = ReadUntilCommaOrEnd(fieldFilterDefinition.Substring(1), out commaIndex).Trim();
					break;
				case '-':
					ff.FieldTransformDeployRule = FieldTransformDeployRule.Ignore;
					ff.FieldName = ReadUntilCommaOrEnd(fieldFilterDefinition.Substring(1), out commaIndex).Trim();
					break;
				case '!':
					ff.FieldTransformDeployRule = FieldTransformDeployRule.Clear;
					ff.FieldName = ReadUntilCommaOrEnd(fieldFilterDefinition.Substring(1), out commaIndex).Trim();
					break;
				case '~':
					ff.FieldTransformDeployRule = FieldTransformDeployRule.ScreamingSnake;
					ff.FieldName = ReadUntilCommaOrEnd(fieldFilterDefinition.Substring(1), out commaIndex).Trim();
					break;
				case '+':
					ff.FieldTransformDeployRule = FieldTransformDeployRule.ForceValue;
					var fieldNameAndValue = ReadFieldNameAndValue(fieldFilterDefinition.Substring(1), out commaIndex);
					ff.FieldName = fieldNameAndValue.Item1.Trim();
					ff.ForcedValue = fieldNameAndValue.Item2;
					break;
				case '$':
					ff.FieldTransformDeployRule = FieldTransformDeployRule.SitecoreSetting;
					fieldNameAndValue = ReadFieldNameAndValue(fieldFilterDefinition.Substring(1), out commaIndex);
					ff.FieldName = fieldNameAndValue.Item1.Trim();
					ff.ForcedValue = fieldNameAndValue.Item2;
					break;
				// These two COULD also be set up internally as ForcedFieldValue, but I prefer to keep them distinguishable
				case ';':
					ff.FieldTransformDeployRule = FieldTransformDeployRule.LoremIpsumTitle;
					ff.FieldName = ReadUntilCommaOrEnd(fieldFilterDefinition.Substring(1), out commaIndex).Trim();
					break;
				case ':':
					ff.FieldTransformDeployRule = FieldTransformDeployRule.LoremIpsumBody;
					ff.FieldName = ReadUntilCommaOrEnd(fieldFilterDefinition.Substring(1), out commaIndex).Trim();
					break;
				default:
					throw new MalformedFieldFilterException($"Invalid Field Filter definition: \"{fieldFilterDefinition}\". Fields must be prefixed with a valid Delployment Operation Token ['?', '-', '!', '+']"); // Screaming Snake Case deliberately omitted
			}

			if (!ff.FieldName.IsNullOrEmpty())
			{
				if(f.Any(x => string.Equals(x.FieldName, ff.FieldName, StringComparison.OrdinalIgnoreCase)))
					throw new DuplicateFieldsException($"Field '{ff.FieldName}' included multiple times in the field filter. This is not allowed, make up your mind.");

				f.Add(ff);
			}

			if (commaIndex != -1)
				return GetFieldTransforms(fieldFilterDefinition.Substring(fieldFilterDefinition.IndexOf(",", commaIndex, StringComparison.OrdinalIgnoreCase) + 1), f);

			return new FieldTransformsCollection(f.ToArray());
		}

		private static Tuple<string,string> ReadFieldNameAndValue(string fieldFilterDefinition, out int commaIndex)
		{
			var fieldName = ReadUntilCharacter(fieldFilterDefinition, '[', out commaIndex);
			if(commaIndex == -1)
				throw new MalformedFieldFilterException($"Malformed forced field. Syntax is 'fieldName[forcedValue]'.");

			var fieldValue = ReadUntilCharacter(fieldFilterDefinition.Substring(commaIndex + 1), ']', out commaIndex);
			if(commaIndex == -1)
				throw new MalformedFieldFilterException($"Malformed forced field. Syntax is 'fieldName[forcedValue]'.");

			commaIndex = fieldFilterDefinition.IndexOf(",", commaIndex, StringComparison.OrdinalIgnoreCase);

			return new Tuple<string, string>(fieldName,fieldValue);
		}

		private static string ReadUntilCommaOrEnd(string fieldFilterDefinition, out int commaIndex)
		{
			return ReadUntilCharacter(fieldFilterDefinition, ',', out commaIndex);
		}

		private static string ReadUntilCharacter(string inputString, char character, out int foundAtIndex)
		{
			var sb = new StringBuilder();
			foundAtIndex = -1;

			for (int i = 0; i < inputString.Length; i++)
			{
				if (inputString[i] == character)
				{
					foundAtIndex = i;
					break;
				}

				sb.Append(inputString[i]);
			}

			return sb.ToString();
		}

		public bool ShouldDeployFieldValue(string existingValue, string proposedValue)
		{
			switch (FieldTransformDeployRule)
			{
				case FieldTransformDeployRule.ForceValue:
				case FieldTransformDeployRule.ScreamingSnake:
				case FieldTransformDeployRule.Clear:
				case FieldTransformDeployRule.LoremIpsumTitle:
				case FieldTransformDeployRule.LoremIpsumBody:
				case FieldTransformDeployRule.SitecoreSetting:
					return true;
				case FieldTransformDeployRule.Ignore:
					return false;
				case FieldTransformDeployRule.OnlyIfNullOrEmpty:
					if (string.IsNullOrEmpty(existingValue))
						return true;
					return false;
			}

			// Unknown deploy rule...   
			return true;
		}

		public string GetFieldValue(string existingValue, string proposedValue)
		{
			switch (FieldTransformDeployRule)
			{
				case FieldTransformDeployRule.Ignore:
					throw new InvalidOperationException("GetResult() should not be called without prior approval from ShouldDeployFieldValue()");
				case FieldTransformDeployRule.OnlyIfNullOrEmpty:
					if (!string.IsNullOrEmpty(existingValue)) throw new InvalidOperationException("GetResult() should not be called without prior approval from ShouldDeployFieldValue()");
					return proposedValue;
				case FieldTransformDeployRule.ScreamingSnake:
					return proposedValue?.ToUpperInvariant().Replace(" ", "_");
				case FieldTransformDeployRule.ForceValue:
					return ForcedValue;
				case FieldTransformDeployRule.Clear:
					return null;
				case FieldTransformDeployRule.LoremIpsumTitle:
					return LoremIpsumTitle;
				case FieldTransformDeployRule.LoremIpsumBody:
					return LoremIpsumBody;
				case FieldTransformDeployRule.SitecoreSetting:
					return Sitecore.Configuration.Settings.GetSetting(ForcedValue);
			}

			return proposedValue;
		}

		public string Description
		{
			get
			{
				char prefix;

				switch (FieldTransformDeployRule)
				{
					case FieldTransformDeployRule.ForceValue:
						prefix = '+';
						break;
					case FieldTransformDeployRule.ScreamingSnake:
						prefix = '~';
						break;
					case FieldTransformDeployRule.Clear:
						prefix = '!';
						break;
					case FieldTransformDeployRule.LoremIpsumTitle:
						prefix = ';';
						break;
					case FieldTransformDeployRule.LoremIpsumBody:
						prefix = ':';
						break;
					case FieldTransformDeployRule.SitecoreSetting:
						prefix = '$';
						break;
					case FieldTransformDeployRule.Ignore:
						prefix = '-';
						break;
					case FieldTransformDeployRule.OnlyIfNullOrEmpty:
						prefix = '?';
						break;
					default:
						throw new InvalidOperationException($"Invalid Field Transform Prefix: {FieldTransformDeployRule.ToString()}");
				}

				if (!string.IsNullOrEmpty(ForcedValue))
					return $"{prefix}{FieldName}[{ForcedValue}]";
				return $"{prefix}{FieldName}";
			}
		}
	}
}

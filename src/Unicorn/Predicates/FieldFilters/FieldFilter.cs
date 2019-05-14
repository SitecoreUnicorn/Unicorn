using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.StringExtensions;

namespace Unicorn.Predicates.FieldFilters
{
	public enum FieldFilterDeployRule
	{
		Normal,					// (no prefix) Always include and deploy
		OnlyIfNullOrEmpty,		// ?  Never overwrite an explicit field value
		Ignore,					// - or !  Ignore this field. (will still get serialized)
		ForceValue,				// +  Force [] value into field
		ScreamingSnake,			// _  Force value into SCREAMING_SNAKE_CASE (undocumented)
	}

	public class FieldFilter
	{
		public FieldFilterDeployRule FieldFilterDeployRule { get; set; }
		public string FieldName { get; set; }
		public string ForcedValue { get; set; }

		public bool ShouldDeployFieldValue(string existingValue, string serializedValue)
		{
			switch (FieldFilterDeployRule)
			{
				case FieldFilterDeployRule.Normal:
				case FieldFilterDeployRule.ForceValue:
				case FieldFilterDeployRule.ScreamingSnake:
					return true;
				case FieldFilterDeployRule.Ignore:
					return false;
				case FieldFilterDeployRule.OnlyIfNullOrEmpty:
					if (string.IsNullOrEmpty(existingValue))
						return true;
					return false;
			}

			// Need to work in the "Standard Fields" logic to this decision. Just need to figure out where this is best positioned.
			return true;
		}

		public string GetResult(string existingValue, string serializedValue)
		{
			switch (FieldFilterDeployRule)
			{
				case FieldFilterDeployRule.Normal:
				case FieldFilterDeployRule.OnlyIfNullOrEmpty:
					return serializedValue;
				case FieldFilterDeployRule.ScreamingSnake:
					return serializedValue.ToUpperInvariant().Replace(" ", "_");
				case FieldFilterDeployRule.ForceValue:
					return ForcedValue;
			}

			return serializedValue;
		}

		public static FieldFilter[] GetFieldFilters(string fieldFilterDefinition, List<FieldFilter> filters = null)
		{
			List<FieldFilter> f = filters ?? new List<FieldFilter>();

			fieldFilterDefinition = fieldFilterDefinition.Trim();

			if (fieldFilterDefinition.Length < 1)
				return f.ToArray();

			FieldFilter ff = new FieldFilter();
			int commaIndex;

			switch (fieldFilterDefinition[0])
			{
				case '?':
					ff.FieldFilterDeployRule = FieldFilterDeployRule.OnlyIfNullOrEmpty;
					ff.FieldName = ReadUntilCommaOrEnd(fieldFilterDefinition.Substring(1), out commaIndex).Trim();
					break;
				case '-':
				case '!':
					ff.FieldFilterDeployRule = FieldFilterDeployRule.Ignore;
					ff.FieldName = ReadUntilCommaOrEnd(fieldFilterDefinition.Substring(1), out commaIndex).Trim();
					break;
				case '_':
					ff.FieldFilterDeployRule = FieldFilterDeployRule.ScreamingSnake;
					ff.FieldName = ReadUntilCommaOrEnd(fieldFilterDefinition.Substring(1), out commaIndex).Trim();
					break;
				case '+':
					ff.FieldFilterDeployRule = FieldFilterDeployRule.ForceValue;
					var fieldNameAndValue = ReadFieldNameAndValue(fieldFilterDefinition.Substring(1), out commaIndex);
					ff.FieldName = fieldNameAndValue.Item1.Trim();
					ff.ForcedValue = fieldNameAndValue.Item2;
					break;
				default:
					ff.FieldFilterDeployRule = FieldFilterDeployRule.Normal;
					ff.FieldName = ReadUntilCommaOrEnd(fieldFilterDefinition, out commaIndex).Trim();
					break;
			}

			if (!ff.FieldName.IsNullOrEmpty())
			{
				if(f.Any(x => string.Equals(x.FieldName, ff.FieldName, StringComparison.OrdinalIgnoreCase)))
					throw new Exception($"Field '{ff.FieldName} included multiple times in the field filter. This is not allowed, make up your mind.");

				f.Add(ff);
			}

			if (commaIndex != -1)
				return GetFieldFilters(fieldFilterDefinition.Substring(commaIndex), f);

			return f.ToArray();
		}

		private static Tuple<string,string> ReadFieldNameAndValue(string fieldFilterDefinition, out int commaIndex)
		{
			var fieldName = ReadUntilCharacter(fieldFilterDefinition, '[', out commaIndex);
			if(commaIndex == -1)
				throw new Exception($"Malformed forced field. Syntax is 'fieldName[forcedValue]'.");

			var fieldValue = ReadUntilCharacter(fieldFilterDefinition.Substring(commaIndex + 1), ']', out commaIndex);
			if(commaIndex == -1)
				throw new Exception($"Malformed forced field. Syntax is 'fieldName[forcedValue]'.");

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
	}
}

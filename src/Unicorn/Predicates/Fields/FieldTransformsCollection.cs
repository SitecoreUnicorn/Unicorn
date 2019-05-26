using System;
using System.Collections.Generic;
using System.Linq;
using Rainbow.Storage;

namespace Unicorn.Predicates.Fields
{
	public class FieldTransformsCollection : IFieldValueManipulator
	{
		public FieldTransformsCollection(MagicTokenTransformer[] transforms)
		{
			Transforms = transforms;
		}

		public MagicTokenTransformer[] Transforms { get; }

		public FieldTransformsCollection MergeFilters(FieldTransformsCollection localFieldFilters)
		{
			List<MagicTokenTransformer> filters = new List<MagicTokenTransformer>(Transforms);

			// Take everything from this Filters collection, but substitute from localFieldFilters
			foreach (MagicTokenTransformer ff in localFieldFilters.Transforms)
			{
				var existingFilter = GetFilterByFieldName(ff.FieldName);
				if (existingFilter != null) filters.Remove(existingFilter);
				filters.Add(ff);
			}

			return new FieldTransformsCollection(filters.ToArray());
		}

		public MagicTokenTransformer GetFilterByFieldName(string fieldName)
		{
			return Transforms.FirstOrDefault(f => f.FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
		}

		public IFieldValueTransformer GetFieldValueTransformer(string fieldName)
		{
			return GetFilterByFieldName(fieldName);
		}

		public IFieldValueTransformer[] GetFieldValueTransformers()
		{
			return Transforms.Cast<IFieldValueTransformer>().ToArray();
		}

		public string[] GetFieldNamesInManipulator()
		{
			return Array.ConvertAll(GetFieldValueTransformers(), a => a.FieldName);
		}
	}
}

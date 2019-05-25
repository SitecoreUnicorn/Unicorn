using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unicorn.Predicates.FieldFilters
{
	public class FieldTransformsCollection
	{
		public FieldTransformsCollection(FieldTransforms[] transforms)
		{
			Transforms = transforms;
		}

		public FieldTransforms[] Transforms { get; }

		public FieldTransformsCollection MergeFilters(FieldTransformsCollection localFieldFilters)
		{
			List<FieldTransforms> filters = new List<FieldTransforms>(Transforms);

			// Take everything from this Filters collection, but substitute from localFieldFilters
			foreach (FieldTransforms ff in localFieldFilters.Transforms)
			{
				var existingFilter = GetFilterByFieldName(ff.FieldName);
				if (existingFilter != null) filters.Remove(existingFilter);
				filters.Add(ff);
			}

			return new FieldTransformsCollection(filters.ToArray());
		}

		public FieldTransforms GetFilterByFieldName(string fieldName)
		{
			return Transforms.FirstOrDefault(f => f.FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
		}
	}
}

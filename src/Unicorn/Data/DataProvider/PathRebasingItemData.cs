using System;
using System.Collections.Generic;
using System.Linq;
using Rainbow.Model;
using Sitecore.Diagnostics;

namespace Unicorn.Data.DataProvider
{
	/// <summary>
	/// Overrides the path and parent ID of an item - and its children - to something else
	/// This allows you to execute a move or rename of an item without needing Sitecore intervention
	/// </summary>
	public class PathRebasingItemData : ItemDecorator
	{
		private readonly string _newParentPath;
		private readonly Guid _newParentId;

		public PathRebasingItemData(IItemData innerItem, string newParentPath, Guid newParentId) : base(innerItem)
		{
			Assert.ArgumentNotNull(innerItem, "innerItem");
			Assert.ArgumentNotNull(newParentPath, "newParentPath");

			_newParentPath = newParentPath;
			_newParentId = newParentId;
		}

		public override Guid ParentId
		{
			get { return _newParentId; }
		}

		public override string Path
		{
			get { return _newParentPath + "/" + Name; }
		}

		public override IEnumerable<IItemData> GetChildren()
		{
			return base.GetChildren().Select(child => new PathRebasingItemData(child, Path, Id));
		}
	}
}

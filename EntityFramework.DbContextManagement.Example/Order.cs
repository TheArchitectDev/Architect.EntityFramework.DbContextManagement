using System;
using System.Collections.Generic;

namespace EntityFramework.DbContextManagement.Example
{
	public class Order
	{
		public int Id { get; set; }
		public string Name { get; set; }

		public DateTime? DateOfBirth { get; set; }

		public DateTime UpdateDateTime { get; set; } = DateTime.UnixEpoch;

		public IReadOnlyCollection<Child> Children { get; set; } = new List<Child>();

		public void AddChild(Child child)
		{
			(this.Children as ICollection<Child>).Add(child);
			//child.Attach(this);
		}
	}

	public class Child
	{
		public int Id { get; set; }
		public int OrderId { get; set; }
		public string Name { get; set; }

		public Child(int id, string name)
		{
			this.Id = id;
			this.Name = name;
		}

		internal void Attach(Order order)
		{
			this.OrderId = order.Id;
		}
	}
}

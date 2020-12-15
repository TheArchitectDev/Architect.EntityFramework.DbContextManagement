using System;

namespace Architect.EntityFramework.DbContextManagement.Example
{
	public class Order
	{
		public override string ToString() => $"{{{nameof(Order)} Id={this.Id} Name={this.Name}}}";

		public int Id { get; }
		public string Name { get; private set; }

		public Order(int id, string name)
		{
			this.Id = id;

			this.Name = name ?? throw new ArgumentNullException(nameof(name));

			if (String.IsNullOrWhiteSpace(this.Name))
				throw new ArgumentException($"{nameof(this.Name)} must not be empty.");
		}

		public void Rename(string name)
		{
			if (String.IsNullOrWhiteSpace(name))
				throw new ArgumentException($"{nameof(this.Name)} must not be empty.");

			this.Name = name;
		}
	}
}

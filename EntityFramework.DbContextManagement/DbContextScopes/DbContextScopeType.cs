namespace Architect.EntityFramework.DbContextManagement.DbContextScopes
{
	// #TODO: Move into UnitOfWork instead?
	/// <summary>
	/// Allows a <see cref="DbContextScope"/> to be marked as being used in a specific way.
	/// </summary>
	internal enum DbContextScopeType : byte
	{
		/// <summary>
		/// No specific usage pattern.
		/// </summary>
		Regular = 0,

		/// <summary>
		/// Used for scoped execution.
		/// </summary>
		ScopedExecution = 1,
	}
}

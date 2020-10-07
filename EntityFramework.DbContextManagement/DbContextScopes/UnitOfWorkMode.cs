namespace Architect.EntityFramework.DbContextManagement.DbContextScopes
{
	/// <summary>
	/// Allows a <see cref="UnitOfWork"/> to be marked as operating in a specific mode.
	/// </summary>
	internal enum UnitOfWorkMode : byte
	{
		/// <summary>
		/// Manually operated.
		/// The default.
		/// </summary>
		Manual = 0,

		/// <summary>
		/// Used for scoped execution.
		/// </summary>
		ScopedExecution = 1,
	}
}

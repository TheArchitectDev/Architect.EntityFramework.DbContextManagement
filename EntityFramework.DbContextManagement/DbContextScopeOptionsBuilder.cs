using Architect.AmbientContexts;

namespace Architect.EntityFramework.DbContextManagement
{
	public sealed class DbContextScopeOptionsBuilder
	{
		public AutoFlushMode AutoFlushMode { get; set; } = AutoFlushMode.DetectExplicitAndImplicitChanges;
		public ExecutionStrategyOptions ExecutionStrategyOptions { get; set; } = ExecutionStrategyOptions.None;
		public AmbientScopeOption DefaultScopeOption { get; set; } = AmbientScopeOption.JoinExisting;

		public DbContextScopeOptions Build()
		{
			var options = new DbContextScopeOptions(
				this.AutoFlushMode,
				this.ExecutionStrategyOptions,
				this.DefaultScopeOption);

			return options;
		}
	}
}

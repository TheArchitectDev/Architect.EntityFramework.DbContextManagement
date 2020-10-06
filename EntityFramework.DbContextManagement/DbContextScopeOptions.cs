using System;
using Architect.AmbientContexts;

namespace Architect.EntityFramework.DbContextManagement
{
	public sealed class DbContextScopeOptions
	{
		public static DbContextScopeOptions Default { get; } = new DbContextScopeOptionsBuilder().Build();

		public AutoFlushMode AutoFlushMode { get; }
		public ExecutionStrategyOptions ExecutionStrategyOptions { get; }
		public AmbientScopeOption DefaultScopeOption { get; }

		internal DbContextScopeOptions(
			AutoFlushMode autoFlushMode,
			ExecutionStrategyOptions executionStrategyOptions,
			AmbientScopeOption defaultScopeOption)
		{
			if (!Enum.IsDefined(typeof(AutoFlushMode), autoFlushMode))
				throw new ArgumentException($"Undefined {nameof(DbContextManagement.AutoFlushMode)}: {autoFlushMode}.");
			if (!Enum.IsDefined(typeof(AmbientScopeOption), defaultScopeOption))
				throw new ArgumentException($"Undefined {nameof(AmbientScopeOption)}: {defaultScopeOption}.");

			this.AutoFlushMode = autoFlushMode;
			this.ExecutionStrategyOptions = executionStrategyOptions;
			this.DefaultScopeOption = defaultScopeOption;
		}
	}
}

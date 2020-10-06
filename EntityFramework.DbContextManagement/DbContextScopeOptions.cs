using System;
using Architect.AmbientContexts;

namespace Architect.EntityFramework.DbContextManagement
{
	public sealed class DbContextScopeOptions
	{
		public static DbContextScopeOptions Default { get; } = new DbContextScopeOptionsBuilder().Build();

		public ExecutionStrategyOptions ExecutionStrategyOptions { get; }
		public AmbientScopeOption DefaultScopeOption { get; }

		internal DbContextScopeOptions(
			ExecutionStrategyOptions executionStrategyOptions,
			AmbientScopeOption defaultScopeOption)
		{
			if (!Enum.IsDefined(typeof(AmbientScopeOption), defaultScopeOption))
				throw new ArgumentException($"Undefined {nameof(AmbientScopeOption)}: {defaultScopeOption}.");

			this.ExecutionStrategyOptions = executionStrategyOptions;
			this.DefaultScopeOption = defaultScopeOption;
		}
	}
}

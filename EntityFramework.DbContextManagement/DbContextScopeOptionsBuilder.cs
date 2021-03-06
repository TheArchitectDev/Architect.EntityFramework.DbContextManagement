﻿using Architect.AmbientContexts;

namespace Architect.EntityFramework.DbContextManagement
{
	public sealed class DbContextScopeOptionsBuilder
	{
		public ExecutionStrategyOptions ExecutionStrategyOptions { get; set; } = ExecutionStrategyOptions.None;
		public AmbientScopeOption DefaultScopeOption { get; set; } = AmbientScopeOption.JoinExisting;
		public bool AvoidFailureOnCommitRetries { get; set; } = true;

		public DbContextScopeOptions Build()
		{
			var options = new DbContextScopeOptions(
				this.ExecutionStrategyOptions,
				this.DefaultScopeOption,
				this.AvoidFailureOnCommitRetries);

			return options;
		}
	}
}

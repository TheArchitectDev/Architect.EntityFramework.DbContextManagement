using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Architect.EntityFramework.DbContextManagement.Observers
{
	internal sealed class CommandInterceptor : DbCommandInterceptor
	{
		private Action WillCreateCommand { get; }
		private Func<bool, DbCommand, CancellationToken, Task> WillExecuteNonQuery { get; }

		public CommandInterceptor(
			Action willCreateCommand,
			Func<bool, DbCommand, CancellationToken, Task> willExecuteNonQuery)
		{
			System.Diagnostics.Debug.Assert(willCreateCommand is not null);
			System.Diagnostics.Debug.Assert(willExecuteNonQuery is not null);

			this.WillCreateCommand = willCreateCommand;
			this.WillExecuteNonQuery = willExecuteNonQuery;
		}

		public override InterceptionResult<DbCommand> CommandCreating(CommandCorrelatedEventData eventData, InterceptionResult<DbCommand> result)
		{
			this.WillCreateCommand();

			return base.CommandCreating(eventData, result);
		}

		public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
		{
			this.WillExecuteNonQuery(false, command, default).RequireCompleted();

			return base.NonQueryExecuting(command, eventData, result);
		}

		public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
			CancellationToken cancellationToken = default)
		{
			await this.WillExecuteNonQuery(true, command, cancellationToken).ConfigureAwait(false);

			return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken).ConfigureAwait(false);
		}
	}
}

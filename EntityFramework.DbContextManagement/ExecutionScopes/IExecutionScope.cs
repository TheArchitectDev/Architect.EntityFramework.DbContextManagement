using System;
using System.Data;
using Microsoft.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Architect.EntityFramework.DbContextManagement
{
	/// <summary>
	/// <para>
	/// Available to a set of operations that should be completed only as a single unit of work.
	/// </para>
	/// <para>
	/// Nested scopes may be joined, in which case the unit of work is bounded by the outermost one.
	/// </para>
	/// </summary>
	public interface IExecutionScope
	{
		/// <summary>
		/// <para>
		/// The <see cref="Microsoft.EntityFrameworkCore.DbContext"/> available in the current scope.
		/// </para>
		/// </summary>
		DbContext DbContext { get; }

		/// <summary>
		/// Whether the scope has joined an outer scope.
		/// </summary>
		bool IsNested { get; }

		/// <summary>
		/// <para>
		/// The <see cref="System.Data.IsolationLevel"/> to use when a new transaction is started automatically.
		/// </para>
		/// <para>
		/// Null to use the provider's default.
		/// </para>
		/// <para>
		/// To avoid mixing isolation levels, once a non-null value is set, it cannot be changed by the current scope or its nested scopes. Doing so results in an <see cref="InvalidOperationException"/>.
		/// </para>
		/// </summary>
		IsolationLevel? IsolationLevel { get; set; }

		/// <summary>
		/// <para>
		/// Marks the current execution scope as aborted.
		/// </para>
		/// <para>
		/// Once the aborted scope ends, any ongoing transaction is rolled back.
		/// </para>
		/// <para>
		/// Further attempts to use the <see cref="Microsoft.EntityFrameworkCore.DbContext"/> will result in a <see cref="System.Transactions.TransactionAbortedException"/>.
		/// This affects any joined parent scopes as well.
		/// </para>
		/// </summary>
		void Abort();
	}

	/// <summary>
	/// <para>
	/// Available to a set of operations that should be completed only as a single unit of work.
	/// </para>
	/// <para>
	/// Nested scopes may be joined, in which case the unit of work is bounded by the outermost one.
	/// </para>
	/// </summary>
	public interface IExecutionScope<TState> : IExecutionScope
	{
		/// <summary>
		/// The state provided to the <see cref="IExecutionScope"/> when it was started.
		/// </summary>
		TState State { get; }
	}
}

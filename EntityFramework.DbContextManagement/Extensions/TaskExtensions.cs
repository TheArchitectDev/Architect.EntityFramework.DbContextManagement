using System.Runtime.CompilerServices;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Architect.EntityFramework.DbContextManagement
{
	/// <summary>
	/// Provides extensions on <see cref="Task"/> and related types.
	/// </summary>
	internal static class TaskExtensions
	{
		/// <summary>
		/// <para>
		/// Performs a debug assertion that the given task is completed.
		/// </para>
		/// <para>
		/// Used to implement synchronous overloads through a task-based implementation.
		/// </para>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void AssumeSynchronous(this Task completedTask)
		{
			System.Diagnostics.Debug.Assert(completedTask.IsCompleted, "This task should have completed synchronously.");
		}
		/// <summary>
		/// <para>
		/// Performs a debug assertion that the given task is completed.
		/// </para>
		/// <para>
		/// Used to implement synchronous overloads through a task-based implementation.
		/// </para>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void AssumeSynchronous(this ValueTask completedTask)
		{
			System.Diagnostics.Debug.Assert(completedTask.IsCompleted, "This task should have completed synchronously.");
		}

		/// <summary>
		/// <para>
		/// Performs a debug assertion that the given task is completed, and returns its result.
		/// </para>
		/// <para>
		/// Used to implement synchronous overloads through a task-based implementation.
		/// </para>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T AssumeSynchronous<T>(this Task<T> completedTask)
		{
			System.Diagnostics.Debug.Assert(completedTask.IsCompleted, "This task should have completed synchronously.");

			return completedTask.Result;
		}
	}
}

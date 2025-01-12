using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Architect.EntityFramework.DbContextManagement
{
	/// <summary>
	/// <para>
	/// Provides extensions on <see cref="Task"/> and related types.
	/// </para>
	/// <para>
	/// Many of these are aimed at implementing synchronous overloads without a duplicated implementation.
	/// Such overloads make use of generalized, task-based implementations, using these extension methods to confirm receiving a task that was completed successfully (or faulted).
	/// </para>
	/// </summary>
	internal static class TaskExtensions
	{
		/// <summary>
		/// <para>
		/// If the given task is faulted, it is awaited, so that its exception is thrown.
		/// </para>
		/// <para>
		/// Otherwise, an exception is thrown to indicate that the task should have been completed.
		/// </para>
		/// </summary>
		private static T HandleIncompleteTask<T>(Task task)
		{
			switch (task.Status)
			{
				case TaskStatus.Faulted:
				case TaskStatus.Canceled:
					task.GetAwaiter().GetResult(); // Allow exceptions to be thrown
					return default!; // Never reached
				default:
					throw new Exception("The task should have completed synchronously.");
			}
		}

		/// <summary>
		/// <para>
		/// Throws if the given task is not completed.
		/// </para>
		/// <para>
		/// If the task contains an exception, it is thrown.
		/// </para>
		/// <para>
		/// Used to implement synchronous overloads through a task-based implementation.
		/// </para>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void RequireCompleted(this Task completedTask)
		{
			if (completedTask.IsCompletedSuccessfully) return;
			HandleIncompleteTask<bool>(completedTask);
		}

		/// <summary>
		/// <para>
		/// Throws if the given task is not completed.
		/// </para>
		/// <para>
		/// If the task contains an exception, it is thrown.
		/// </para>
		/// <para>
		/// Used to implement synchronous overloads through a task-based implementation.
		/// </para>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void RequireCompleted(this ValueTask t)
		{
			if (t.IsCompletedSuccessfully) return;
			HandleIncompleteTask<bool>(t.AsTask());
		}

		/// <summary>
		/// <para>
		/// Throws if the given task is not completed.
		/// </para>
		/// <para>
		/// Returns the task's result.
		/// If the task contains an exception, it is thrown.
		/// </para>
		/// <para>
		/// Used to implement synchronous overloads through a task-based implementation.
		/// </para>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T RequireCompleted<T>(this Task<T> completedTask)
		{
			return completedTask.IsCompletedSuccessfully
				? completedTask.Result
				: HandleIncompleteTask<T>(completedTask);
		}

		/// <summary>
		/// <para>
		/// Throws if the given task is not completed.
		/// </para>
		/// <para>
		/// Returns the task's result.
		/// If the task contains an exception, it is thrown.
		/// </para>
		/// <para>
		/// Used to implement synchronous overloads through a task-based implementation.
		/// </para>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T RequireCompleted<T>(this ValueTask<T> completedTask)
		{
			return completedTask.IsCompletedSuccessfully
				? completedTask.Result
				: HandleIncompleteTask<T>(completedTask.AsTask());
		}

		/// <summary>
		/// <para>
		/// Performs a debug assertion that the given task is completed.
		/// </para>
		/// <para>
		/// Used to implement synchronous overloads through a task-based implementation.
		/// </para>
		/// </summary>
		/// <param name="synchronous">If true, an assertion is made that the task is completed.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task AssumeSynchronous(this Task task, bool synchronous)
		{
			System.Diagnostics.Debug.Assert(!synchronous || task.IsCompleted, "This task should have completed synchronously.");
			return task;
		}
		/// <summary>
		/// <para>
		/// Performs a debug assertion that the given task is completed.
		/// </para>
		/// <para>
		/// Used to implement synchronous overloads through a task-based implementation.
		/// </para>
		/// </summary>
		/// <param name="synchronous">If true, an assertion is made that the task is completed.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ValueTask AssumeSynchronous(this ValueTask task, bool synchronous)
		{
			System.Diagnostics.Debug.Assert(!synchronous || task.IsCompleted, "This task should have completed synchronously.");
			return task;
		}

		/// <summary>
		/// <para>
		/// Performs a debug assertion that the given task is completed, and returns its result.
		/// </para>
		/// <para>
		/// Used to implement synchronous overloads through a task-based implementation.
		/// </para>
		/// <param name="synchronous">If true, an assertion is made that the task is completed.</param>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<T> AssumeSynchronous<T>(this Task<T> task, bool synchronous)
		{
			System.Diagnostics.Debug.Assert(!synchronous || task.IsCompleted, "This task should have completed synchronously.");
			return task;
		}

		/// <summary>
		/// <para>
		/// Performs a debug assertion that the given task is completed, and returns its result.
		/// </para>
		/// <para>
		/// Used to implement synchronous overloads through a task-based implementation.
		/// </para>
		/// <param name="synchronous">If true, an assertion is made that the task is completed.</param>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ValueTask<T> AssumeSynchronous<T>(this ValueTask<T> task, bool synchronous)
		{
			System.Diagnostics.Debug.Assert(!synchronous || task.IsCompleted, "This task should have completed synchronously.");
			return task;
		}
	}
}

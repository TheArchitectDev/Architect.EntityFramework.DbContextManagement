using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Architect.EntityFramework.DbContextManagement
{
	/// <summary>
	/// Provides extensions on the <see cref="DbContext"/> type.
	/// </summary>
	internal static class DbContextExtensions
	{
		private static Func<DbContext, int> DbContextLeaseCountGetter { get; } = CreateFieldGetter<DbContext, int>("_leaseCount");

		private static Func<TObject, TField> CreateFieldGetter<TObject, TField>(string fieldName)
		{
			var field = typeof(TObject).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
				ThrowHelper.ThrowIncompatibleWithEfVersion<FieldInfo>(new Exception($"Field {nameof(DbContext)}.{fieldName} does not exist."));
			var param = Expression.Parameter(typeof(TObject), "obj");
			var value = Expression.Field(param, field);
			var lambda = Expression.Lambda<Func<TObject, TField>>(value, param);
			var fieldGetter = lambda.Compile();
			return fieldGetter;
		}

		/// <summary>
		/// <para>
		/// Returns the lease count on the <see cref="DbContext"/>, which increases on every lease if pooling is used.
		/// </para>
		/// <para>
		/// This can be used to confirm that a <see cref="DbContext"/> was not prematurely disposed and redistributed.
		/// </para>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetLeaseCount(this DbContext dbContext)
		{
			return DbContextLeaseCountGetter(dbContext);
		}
	}
}

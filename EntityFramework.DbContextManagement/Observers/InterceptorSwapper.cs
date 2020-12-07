using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Architect.EntityFramework.DbContextManagement.Observers
{
	/// <summary>
	/// <para>
	/// Helps swap the set of interceptors attached to a <see cref="Microsoft.EntityFrameworkCore.DbContext"/> on the fly.
	/// </para>
	/// <para>
	/// Safe even with context pooling, as long as the <see cref="InterceptorSwapper{TInterceptor}"/> is disposed before the <see cref="Microsoft.EntityFrameworkCore.DbContext"/>.
	/// </para>
	/// </summary>
	internal sealed class InterceptorSwapper<TInterceptor> : IDisposable
		where TInterceptor : class, IInterceptor
	{
		private TInterceptor OriginalInterceptor { get; }
		private InterceptorAggregator<TInterceptor> Aggregator { get; }

		private DbContext DbContext { get; }

		public InterceptorSwapper(DbContext dbContext, TInterceptor additionalInterceptor)
		{
			this.DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

			// Swap the set of interceptors by a new set that has the additional interceptor appended
			{
				var interceptors = this.GetIInterceptors();

				// Get the original (composite) interceptor
				this.OriginalInterceptor = interceptors.Aggregate<TInterceptor>();

				this.Aggregator = this.GetAggregator(interceptors);

				var newInterceptors = this.OriginalInterceptor is null
					? new[] { additionalInterceptor }
					: new[] { this.OriginalInterceptor, additionalInterceptor };

				this.PopulateAggregator(this.Aggregator, newInterceptors);
			}
		}

		public void Dispose()
		{
			this.PopulateAggregator(this.Aggregator, this.OriginalInterceptor is null
				? Array.Empty<TInterceptor>()
				: new[] { this.OriginalInterceptor });
		}

		private IInterceptors GetIInterceptors()
		{
			return this.DbContext.GetService<IInterceptors>();

			// Alternative to get a service from the internal service provider even if the DbContext turns out to be already disposed, to avoid throwing
			{
				//var dbContextServicesType = typeof(DbContext).Assembly.GetTypes().SingleOrDefault(type => type.Name == "IDbContextServices" && type.IsInterface) ??
				//	throw new Exception("Type IDbContextServices does not exist.");

				//var dbContextServicesField = typeof(DbContext).GetField("_contextServices", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) ??
				//	throw new Exception("Field _contextServices does not exist.");
				//if (!dbContextServicesType.IsAssignableFrom(dbContextServicesField.FieldType))
				//	throw new Exception($"Field _contextServices is of unexpected type {dbContextServicesField.FieldType.Name}.");

				//var internalServiceProviderProperty = dbContextServicesType.GetProperty("InternalServiceProvider", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) ??
				//	throw new Exception("Property IDbContextServices.InternalServiceProvider does not exist.");
				//if (!typeof(IServiceProvider).IsAssignableFrom(internalServiceProviderProperty.PropertyType))
				//	throw new Exception($"Property IDbContextServices.InternalServiceProvider is of unexpected type {internalServiceProviderProperty.PropertyType.Name}.");
				//var internalServiceProviderGetter = internalServiceProviderProperty.GetMethod ??
				//	throw new Exception("Property IDbContextServices.InternalServiceProvider is not gettable.");

				//var dbContextServices = dbContextServicesField.GetValue(this.DbContext) ??
				//	throw new Exception("Field _contextServices contained null.");
				//var internalServiceProvider = (IServiceProvider?)internalServiceProviderGetter.Invoke(dbContextServices, parameters: null) ??
				//	throw new Exception("Property IDbContextServices.InternalServiceProvider returned null.");

				//return internalServiceProvider.GetRequiredService<IInterceptors>();
			}
		}

		/// <summary>
		/// From the <see cref="IInterceptors"/> object, returns the aggregator of interceptors of the given type.
		/// </summary>
		private InterceptorAggregator<TInterceptor> GetAggregator(IInterceptors interceptors)
		{
			var aggregator = ReflectionCache.GetAggregator(interceptors);
			return aggregator;
		}

		/// <summary>
		/// Force re-evaluates the given aggregator with the given interceptors.
		/// </summary>
		private void PopulateAggregator(InterceptorAggregator<TInterceptor> aggregator, TInterceptor[] interceptors)
		{
			ReflectionCache.SetInterceptors(aggregator, interceptors);
		}

		/// <summary>
		/// Provides and caches compiled code created with the help of reflection.
		/// </summary>
		private static class ReflectionCache
		{
			private static object Lock { get; } = new object();
			private static Func<IInterceptors, InterceptorAggregator<TInterceptor>>? AggregatorGetter { get; set; }
			private static Action<InterceptorAggregator<TInterceptor>, TInterceptor[]>? InterceptorsSetter { get; set; }

			public static InterceptorAggregator<TInterceptor> GetAggregator(IInterceptors interceptors)
			{
				System.Diagnostics.Debug.Assert(interceptors != null);

				if (AggregatorGetter is null)
					SetAggregatorGetter(interceptors);

				System.Diagnostics.Debug.Assert(AggregatorGetter != null);

				return AggregatorGetter(interceptors);

				// Local function that creates and assigns the compiled getter
				static void SetAggregatorGetter(IInterceptors theInterceptors)
				{
					lock (Lock)
					{
						if (AggregatorGetter != null) return;

						try
						{
							// Check everything with slow reflection once

							var aggregatorsField = theInterceptors.GetType().GetField("_aggregators", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) ??
								throw new Exception($"Field {theInterceptors.GetType().Name}._aggregators does not exist.");
							if (!typeof(IReadOnlyDictionary<Type, IInterceptorAggregator>).IsAssignableFrom(aggregatorsField.FieldType))
								throw new Exception($"Field {theInterceptors.GetType().Name}._aggregators is of unexpected type {aggregatorsField.FieldType.Name}.");

							var aggregators = (IReadOnlyDictionary<Type, IInterceptorAggregator>?)aggregatorsField.GetValue(theInterceptors) ??
								throw new Exception($"Field {theInterceptors.GetType().Name}._aggregators contains a null value.");

							if (!aggregators.TryGetValue(typeof(TInterceptor), out var aggregator))
								throw new Exception($"Field {theInterceptors.GetType().Name}._aggregators did not contain an aggregator for {typeof(TInterceptor).Name}.");

							if (aggregator is null)
								throw new Exception($"Field {theInterceptors.GetType().Name}._aggregators contained a null aggregator for {typeof(TInterceptor).Name}.");

							if (aggregator is not InterceptorAggregator<TInterceptor> typedAggregator)
								throw new Exception($"Field {theInterceptors.GetType().Name}._aggregators contained an aggregator for {typeof(TInterceptor).Name} of unexpected type {aggregator.GetType().Name}.");

							// Compile a fast expression tree
							// Omit the checks, trusting that things will be the same in the future
							{
								var indexerMethod = typeof(IReadOnlyDictionary<Type, IInterceptorAggregator>).GetMethod("get_Item") ??
									throw new Exception($"Type {typeof(IReadOnlyDictionary<Type, IInterceptorAggregator>).Name} does not have a single public indexer.");

								// IInterceptors interceptors;
								var param = Expression.Parameter(typeof(IInterceptors), "interceptors");
								// (Interceptors)interceptors;
								var convertedParam = Expression.Convert(param, theInterceptors.GetType());
								// ((Interceptors)interceptors)._aggregators;
								var aggregatorDictionary = Expression.Field(convertedParam, aggregatorsField);
								var interceptorType = Expression.Constant(typeof(TInterceptor));
								// _aggregators[typeof(TInterceptor)]
								var interceptionAggregator = Expression.Call(aggregatorDictionary, indexerMethod, interceptorType);
								// (InterceptorAggregator<TInterceptor>)_aggregators[typeof(TInterceptor)]
								var convertedInterceptionAggregator = Expression.Convert(interceptionAggregator, typeof(InterceptorAggregator<TInterceptor>));

								var lambda = Expression.Lambda<Func<IInterceptors, InterceptorAggregator<TInterceptor>>>(convertedInterceptionAggregator, param);
								AggregatorGetter = lambda.Compile();
							}
						}
						catch (Exception e)
						{
							throw ThrowHelper.ThrowIncompatibleWithEfVersion(e);
						}
					}
				}
			}

			public static void SetInterceptors(InterceptorAggregator<TInterceptor> aggregator, TInterceptor[] interceptors)
			{
				System.Diagnostics.Debug.Assert(aggregator != null);

				if (InterceptorsSetter is null)
					SetInterceptorsSetter(aggregator);

				System.Diagnostics.Debug.Assert(InterceptorsSetter != null);

				InterceptorsSetter(aggregator, interceptors);

				// Local function that creates and assigns the compiled setter
				static void SetInterceptorsSetter(InterceptorAggregator<TInterceptor> theAggregator)
				{
					lock (Lock)
					{
						if (InterceptorsSetter != null) return;

						try
						{
							var aggregatorBaseType = theAggregator.GetType().BaseType!;

							// Null out the field, which AggregateInterceptors() does not handle for us (because it does not expect reinitialization)
							var interceptorField = aggregatorBaseType.GetField("_interceptor", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) ??
								throw new Exception($"Field {aggregatorBaseType.Name}._interceptor does not exist.");

							var resolvedField = aggregatorBaseType.GetField("_resolved", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) ??
								throw new Exception($"Field {aggregatorBaseType.Name}._resolved does not exist.");

							// Compile a fast expression tree
							// Omit the checks, trusting that things will be the same in the future
							{
								var aggregateInterceptorsMethod = typeof(IInterceptorAggregator).GetMethod(nameof(IInterceptorAggregator.AggregateInterceptors)) ??
									throw new Exception($"Type {nameof(IInterceptorAggregator)} does not have a single public method named {nameof(IInterceptorAggregator.AggregateInterceptors)}.");

								// InterceptorAggregator<TInterceptor> aggregator;
								var aggregatorParam = Expression.Parameter(typeof(InterceptorAggregator<TInterceptor>), "aggregator");
								// TInterceptor[] interceptors;
								var interceptorsParam = Expression.Parameter(typeof(TInterceptor[]), "interceptors");
								// SomeCustomAggregator theAggregator;
								var convertedAggregatorVariable = Expression.Variable(aggregatorBaseType, "theAggregator");
								// theAggregator = (SomeCustomAggregator)aggregator;
								var aggregatorVariableAssignment = Expression.Assign(convertedAggregatorVariable, Expression.Convert(aggregatorParam, aggregatorBaseType));
								// theAggregator._interceptor = null;
								var interceptorFieldExpression = Expression.Field(convertedAggregatorVariable, interceptorField);
								var setInterceptors = Expression.Assign(interceptorFieldExpression, Expression.Constant(null, interceptorField.FieldType));
								// theAggregator._resolved = false;
								var resolvedFieldExpression = Expression.Field(convertedAggregatorVariable, resolvedField);
								var setResolved = Expression.Assign(resolvedFieldExpression, Expression.Constant(false));
								// theAggregator.AggregateInterceptors(interceptors);
								var callAggregateInterceptors = Expression.Call(convertedAggregatorVariable, aggregateInterceptorsMethod, interceptorsParam);
								// Block(theAggregator):
								// theAggregator = (SomeCustomAggregator)aggregator;
								// theAggregator._interceptor = null;
								// theAggregator._resolved = false;
								// theAggregator.AggregateInterceptors(interceptors);
								var blockExpression = Expression.Block(variables: new[] { convertedAggregatorVariable }, aggregatorVariableAssignment,
									setInterceptors, setResolved, callAggregateInterceptors);

								var lambda = Expression.Lambda<Action<InterceptorAggregator<TInterceptor>, TInterceptor[]>>(blockExpression, aggregatorParam, interceptorsParam);
								InterceptorsSetter = lambda.Compile();
							}
						}
						catch (Exception e)
						{
							throw ThrowHelper.ThrowIncompatibleWithEfVersion(e);
						}
					}
				}
			}
		}
	}
}

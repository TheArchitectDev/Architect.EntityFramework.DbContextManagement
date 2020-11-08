using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Architect.AmbientContexts;
using Architect.EntityFramework.DbContextManagement;
using EntityFramework.DbContextManagement.Example.Example;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EntityFramework.DbContextManagement.Example
{
	class Program
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void ThrowAsync1(CancellationToken _ = default)
		{
			Console.WriteLine("HI");
			throw new Exception("REPLACED!!");
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Task<int> ThrowAsync2(bool _, CancellationToken cancellationToken = default)
		{
			ThrowAsync1(cancellationToken);
			throw new Exception();
		}

		static async Task Main()
		{
			//Replace(typeof(DbContext).GetMethods().Single(m => m.Name == "SaveChangesAsync" && m.GetParameters().Count() == 1), typeof(Program).GetMethod("ThrowAsync1"));
			//Replace(typeof(DbContext).GetMethods().Single(m => m.Name == "SaveChangesAsync" && m.GetParameters().Count() == 2), typeof(Program).GetMethod("ThrowAsync2"));
			//Replace(typeof(ExampleDbContext).GetMethods().Single(m => m.Name == "SaveChangesAsync" && m.GetParameters().Count() == 1), typeof(Program).GetMethod("ThrowAsync1"));
			//Replace(typeof(ExampleDbContext).GetMethods().Single(m => m.Name == "SaveChangesAsync" && m.GetParameters().Count() == 2), typeof(Program).GetMethod("ThrowAsync2"));
			//DynamicMojo.SwapMethodBodies(typeof(ExampleDbContext).GetMethods().Single(m => m.Name == "SaveChangesAsync" && m.GetParameters().Count() == 2), typeof(Program).GetMethod("ThrowAsync2"));
			//var harmony = new Harmony("Architect.EntityFramework.DbContextManagement");

			//var originalMethod2 = typeof(DbContext).GetMethods().Single(m => m.Name == "SaveChangesAsync" && m.GetParameters().Count() == 1);
			//var prefixMethod2 = typeof(Program).GetMethod("ThrowAsync1");
			//harmony.Patch(originalMethod2, prefix: new HarmonyMethod(prefixMethod2));

			//var originalMethod = typeof(ExampleDbContext).GetMethods().Single(m => m.Name == "SaveChangesAsync" && m.GetParameters().Count() == 2);
			//var prefixMethod = typeof(Program).GetMethod("ThrowAsync2");
			//harmony.Patch(originalMethod, prefix: new HarmonyMethod(prefixMethod), postfix: new HarmonyMethod(prefixMethod));

			await OrderDemo.Run();
			if (DateTime.Today.Year < 3000) return;

			File.Delete("Temp.database");

			var hostBuilder = new HostBuilder();
			hostBuilder.ConfigureServices(services => services.AddDbContext<ExampleDbContext>(opt => opt.UseSqlite("Filename=D:/Temp/Temp.database;").UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll), ServiceLifetime.Transient));
			hostBuilder.ConfigureServices(services => services.AddDbContextScope<ExampleDbContext>());//scope => scope.DbContextFactory(() => new ExampleDbContext())));
			//hostBuilder.ConfigureServices(services => services.AddDbContextScope<ExampleDbContext>(scope => scope.DbContextFactory(() => new ExampleDbContext())));
			using var host = hostBuilder.Build();
			var provider = host.Services.GetRequiredService<IDbContextProvider<ExampleDbContext>>();
			var accessor = host.Services.GetRequiredService<IDbContextAccessor<ExampleDbContext>>();

			await using (var scope = provider.CreateDbContextScope())
			{
				scope.DbContext.Database.OpenConnection();
				scope.DbContext.Database.EnsureCreated();
				{
					await using var innerScope = provider.CreateDbContextScope(AmbientScopeOption.JoinExisting);
					
					accessor.CurrentDbContext.Database.IsSqlite();

					var order = new Order() { Id = 1, Name = "Ordeur" };
					accessor.CurrentDbContext.Orders.Add(order);

					await DbContextScope<ExampleDbContext>.Current.DbContext.SaveChangesAsync();

					//if (DbContextScope<ExampleDbContext>.Current.DbContext.Orders.Single().Id != 1) throw new Exception();
				}
			}
			//if (DbContextScope<ExampleDbContext>.Current.DbContext.Orders.Single().Id != 1) throw new Exception();

			Console.WriteLine();
		}

		// Note: This method replaces methodToReplace with methodToInject
		// Note: methodToInject will still remain pointing to the same location
		private static unsafe MethodReplacementState Replace(MethodInfo methodToReplace, MethodInfo methodToInject)
		{
			Console.WriteLine($"Replacing {methodToReplace.DeclaringType.Name}.{methodToReplace.Name} by {methodToInject.Name}.");

			//#if DEBUG
			RuntimeHelpers.PrepareMethod(methodToReplace.MethodHandle);
			RuntimeHelpers.PrepareMethod(methodToInject.MethodHandle);
			//#endif
			MethodReplacementState state;

			IntPtr tar = methodToReplace.MethodHandle.Value;
			if (!methodToReplace.IsVirtual)
				tar += 8;
			else
			{
				var index = (int)(((*(long*)tar) >> 32) & 0xFF);
				var classStart = *(IntPtr*)(methodToReplace.DeclaringType.TypeHandle.Value + (IntPtr.Size == 4 ? 40 : 64));
				tar = classStart + IntPtr.Size * index;
			}
			var inj = methodToInject.MethodHandle.Value + 8;
#if DEBUG
			tar = *(IntPtr*)tar + 1;
			inj = *(IntPtr*)inj + 1;
			state.Location = tar;
			state.OriginalValue = new IntPtr(*(int*)tar);

			*(int*)tar = *(int*)inj + (int)(long)inj - (int)(long)tar;
			return state;

#else
            state.Location = tar;
            state.OriginalValue = *(IntPtr*)tar;
            * (IntPtr*)tar = *(IntPtr*)inj;
            return state;
#endif
		}
		private struct MethodReplacementState : IDisposable
		{
			internal IntPtr Location;
			internal IntPtr OriginalValue;
			public void Dispose()
			{
				this.Restore();
			}

			public unsafe void Restore()
			{
#if DEBUG
				*(int*)Location = (int)OriginalValue;
#else
            *(IntPtr*)Location = OriginalValue;
#endif
			}
		}

		public static class DynamicMojo
		{
			/// <summary>
			/// Swaps the function pointers for a and b, effectively swapping the method bodies.
			/// </summary>
			/// <exception cref="ArgumentException">
			/// a and b must have same signature
			/// </exception>
			/// <param name="a">Method to swap</param>
			/// <param name="b">Method to swap</param>
			public static void SwapMethodBodies(MethodInfo a, MethodInfo b)
			{
				//if (!HasSameSignature(a, b)) throw new ArgumentException("a and b must have have same signature");

				RuntimeHelpers.PrepareMethod(a.MethodHandle);
				RuntimeHelpers.PrepareMethod(b.MethodHandle);

				unsafe
				{
					if (IntPtr.Size == 4)
					{
						int* inj = (int*)b.MethodHandle.Value.ToPointer() + 2;
						int* tar = (int*)a.MethodHandle.Value.ToPointer() + 2;

						byte* injInst = (byte*)*inj;
						byte* tarInst = (byte*)*tar;

						int* injSrc = (int*)(injInst + 1);
						int* tarSrc = (int*)(tarInst + 1);

						int tmp = *tarSrc;
						*tarSrc = (((int)injInst + 5) + *injSrc) - ((int)tarInst + 5);
						*injSrc = (((int)tarInst + 5) + tmp) - ((int)injInst + 5);
					}
					else
					{
						long* inj = (long*)b.MethodHandle.Value.ToPointer() + 1;
						long* tar = (long*)a.MethodHandle.Value.ToPointer() + 1;
#if DEBUG
						Console.WriteLine("\nVersion x64 Debug\n");
						byte* injInst = (byte*)*inj;
						byte* tarInst = (byte*)*tar;


						int* injSrc = (int*)(injInst + 1);
						int* tarSrc = (int*)(tarInst + 1);

						int tmp = *tarSrc;
						*tarSrc = (((int)injInst + 5) + *injSrc) - ((int)tarInst + 5);
						*injSrc = (((int)tarInst + 5) + tmp) - ((int)injInst + 5);
#else
						Console.WriteLine("\nVersion x64 Release\n");
						*tar = *inj;
#endif
					}
				}
			}

			private static bool HasSameSignature(MethodInfo a, MethodInfo b)
			{
				bool sameParams = !a.GetParameters().Any(x => !b.GetParameters().Any(y => x == y));
				bool sameReturnType = a.ReturnType == b.ReturnType;
				return sameParams && sameReturnType;
			}
		}
	}
}

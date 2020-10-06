using System;
using Architect.EntityFramework.DbContextManagement.Exceptions;

namespace Architect.EntityFramework.DbContextManagement
{
	internal static class ThrowHelper
	{
		internal static Exception ThrowIncompatibleWithEfVersion(Exception? innerException = null)
		{
			return ThrowIncompatibleWithEfVersion<Exception>(innerException);
		}

		internal static TResult ThrowIncompatibleWithEfVersion<TResult>(Exception? innerException = null)
		{
			throw new IncompatibleVersionException("This package is incompatible with the current version of EntityFrameworkCore. Downgrade that package or upgrade this one.", innerException);
		}
	}
}

using System;
using System.Runtime.Serialization;

namespace Architect.EntityFramework.DbContextManagement.Exceptions
{
	/// <summary>
	/// An exception that indicates that the current <see cref="DbContextManagement"/> version is incompatible with the current <see cref="Microsoft.EntityFrameworkCore"/> package.
	/// </summary>
	internal sealed class IncompatibleVersionException : Exception
	{
		public IncompatibleVersionException()
		{
		}

		public IncompatibleVersionException(string? message) : base(message)
		{
		}

		public IncompatibleVersionException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}

		public IncompatibleVersionException(string? message, Exception? innerException) : base(message, innerException)
		{
		}
	}
}

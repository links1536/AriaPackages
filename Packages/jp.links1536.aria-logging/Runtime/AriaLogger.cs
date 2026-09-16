
using Aria.Logging;
using Microsoft.Extensions.Logging;

namespace Aria.Logging
{
	public static class AriaLogger<T>
	{
		static readonly ILogger m_Logger = new LazyLoggerAccessor<T>();

		public static ILogger Get()
			=> m_Logger;
	}

	sealed class LazyLoggerAccessor<T> : ILogger
	{
		ILogger? m_Logger;

		ILogger GetOrCreateInstance()
		{
			if (m_Logger == null)
				m_Logger = LoggerHandler.CreateLogger<T>();
			return m_Logger;
		}

		public System.IDisposable? BeginScope<TState>(TState state) where TState : notnull
		{
			var logger = GetOrCreateInstance();
			return logger.BeginScope(state);
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			var logger = GetOrCreateInstance();
			return logger.IsEnabled(logLevel);
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, System.Exception? exception, System.Func<TState, System.Exception?, string> formatter)
		{
			var logger = GetOrCreateInstance();
			logger.Log(logLevel, eventId, state, exception, formatter);
		}
	}
}

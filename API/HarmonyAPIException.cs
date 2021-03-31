using System;

namespace HarmonyManager
{
	/// <summary>An exception caused by an invalid call to the Harmony API.</summary>
	///
	[Serializable]
	public class HarmonyAPIException : Exception
	{
		internal HarmonyAPIException(string message)
			: base(message)
		{
		}
		internal HarmonyAPIException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}

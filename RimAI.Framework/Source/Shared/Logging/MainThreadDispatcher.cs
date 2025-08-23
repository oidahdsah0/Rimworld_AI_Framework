using System;
using System.Collections.Concurrent;
using UnityEngine;
using Verse;

namespace RimAI.Framework.Shared.Logging
{
	[StaticConstructorOnStartup]
	public static class RimAILoggerBootstrap
	{
		static RimAILoggerBootstrap()
		{
			RimAIFrameworkDispatcher.Ensure();
		}
	}

	internal class RimAIFrameworkDispatcher : MonoBehaviour
	{
		private static RimAIFrameworkDispatcher _instance;
		private static readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();

		public static bool IsAvailable => _instance != null;

		public static void Ensure()
		{
			if (_instance != null) return;
			var go = new GameObject("RimAIFrameworkDispatcher");
			UnityEngine.Object.DontDestroyOnLoad(go);
			_instance = go.AddComponent<RimAIFrameworkDispatcher>();
		}

		public static void Post(Action action)
		{
			if (action == null) return;
			_queue.Enqueue(action);
		}

		private void Update()
		{
			while (_queue.TryDequeue(out var a))
			{
				try { a(); }
				catch (Exception ex)
				{
					Verse.Log.Error("[RimAI.Framework] Log dispatch error: " + ex);
				}
			}
		}
	}
}


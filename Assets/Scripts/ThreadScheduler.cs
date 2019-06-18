using UnityEngine;

namespace Kai.SDK
{
	public class ThreadScheduler : MonoBehaviour
	{
		public void Update()
		{
			lock(KaiSDK.actions)
			{
				if (KaiSDK.actions.Count == 0)
					return;
				foreach (var action in KaiSDK.actions)
				{
					action.Item1.Invoke(action.Item2, action.Item3);
				}
				KaiSDK.actions.Clear();
			}
		}
	}
}
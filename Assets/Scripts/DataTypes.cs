using System;

namespace Kai.SDK
{
	[Flags]
	public enum KaiCapabilities
	{
		GestureData				= 1,
		LinearFlickData			= 2,
		FingerShortcutData		= 4,
		FingerPositionalData	= 8,

		PYRData					= 16,
		QuaternionData			= 32,
		AccelerometerData		= 64,
		GyroscopeData			= 128,
		
		MagnetometerData		= 256
	}

	public enum Gesture
	{
		SwipeUp,
		SwipeDown,
		SwipeLeft,
		SwipeRight,
		SideSwipeUp,
		SideSwipeDown,
		SideSwipeLeft,
		SideSwipeRight,
		Pinch2Begin,
		Pinch2End,
		GrabBegin,
		GrabEnd,
		Pinch3Begin,
		Pinch3End,
		DialBegin,
		DialEnd			
	}

	public enum Hand
	{
		Left,
		Right
	}
}

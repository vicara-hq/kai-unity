using System;
using UnityEngine;
using WebSocketSharp;
using Newtonsoft.Json;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Kai.SDK
{
	public static class KaiSDK
	{
		internal static GameObject threadSchedulerObject = null;
		internal static readonly LinkedList<Tuple<Action<object, EventArgs>, object, EventArgs>> actions = new LinkedList<Tuple<Action<object, EventArgs>, object, EventArgs>>();
		private static Kai[] connectedKais { get; } = new Kai[8];
		private static bool initialised;
		private static WebSocket webSocket;
		private static Thread connectionThread;


		/// <summary>
		/// Represents a boolean value whether the module is authenticated or not
		/// </summary>
		public static bool Authenticated { get; private set; }

		/// <summary>
		/// Represents the <code>ProcessName</code> of the process that's in focus
		/// </summary>
		/// <see cref="System.Diagnostics.Process.ProcessName"/>
		public static string ForegroundProcess { get; private set; }

		/// <summary>
		/// Represents the module ID
		/// </summary>
		public static string ModuleID { get; private set; }

		/// <summary>
		/// Represents the secret data of the Module
		/// </summary>
		public static string ModuleSecret { get; private set; }

		/// <summary>
		/// Occurs when an unrecognised data is received by the module
		/// </summary>
		public static UnknownDataHandler UnknownData;

		/// <summary>
		/// Occurs when an error is reported by the SDK
		/// </summary>
		public static EventHandler<ErrorEventArgs> Error;

		/// <summary>
		/// Contains the value of the default Kai that is connected to the SDK
		/// </summary>
		public static Kai DefaultKai { get; } = new Kai();

		/// <summary>
		/// Contains the value of the default left Kai that is connected to the SDK
		/// </summary>
		public static Kai DefaultLeftKai { get; } = new Kai();

		/// <summary>
		/// Contains the value of the default right Kai that is connected to the SDK
		/// </summary>
		public static Kai DefaultRightKai { get; } = new Kai();

		/// <summary>
		/// Throws data when any Kai receives data
		/// </summary>
		public static Kai AnyKai { get; } = new Kai();

		/// <summary>
		/// Initialises the SDK. This function *has* to be called before receiving data from the Kai
		/// </summary>
		public static void Initialise(string moduleId, string moduleSecret)
		{
			if (initialised)
				return;

			ModuleID = moduleId;
			ModuleSecret = moduleSecret;

			threadSchedulerObject = GameObject.Find("KaiSDK-ThreadSchedulerObject");
			if (threadSchedulerObject != null)
			{
				GameObject.Destroy(threadSchedulerObject);
			}
			threadSchedulerObject = new GameObject("KaiSDK-ThreadSchedulerObject", typeof(ThreadScheduler));

			connectionThread = new Thread(ConnectInNewThread);

			Application.quitting += Disconnect;

			initialised = true;
		}

		/// <summary>
		/// Gets the Kai object by the Kai ID
		/// </summary>
		public static Kai GetKaiByID(byte kaiId)
		{
			if (kaiId >= 8)
				throw new ArgumentOutOfRangeException(nameof(kaiId));
			return connectedKais[kaiId];
		}

		/// <summary>
		/// Connects to the SDK and starts receiving data
		/// </summary>
		/// <exception cref="ApplicationException">Thrown if this function is called before Initialise()</exception>
		public static void Connect()
		{
			connectionThread.Start();
		}

		/// <summary>
		/// Disconnects from the SDK
		/// </summary>
		public static void Disconnect()
		{
			webSocket.Close();
			connectionThread.Join();
			Application.quitting -= Disconnect;

			initialised = false;
		}

		/// <summary>
		/// Gets the list of all connected Kais
		/// </summary>
		public static void GetConnectedKais()
		{
			Send(
				new JObject()
				{
					[Constants.Type] = Constants.ListConnectedKais
				}.ToString(Formatting.None)
			);
		}

		/// <summary>
		/// Set the Kai's capabilities and subscribes to that data
		/// </summary>
		/// <param name="capabilities">The capabilities to set the Kai to</param>
		/// <param name="kai">The kai to set the capabilities to</param>
		public static void SetCapabilities(Kai kai, KaiCapabilities capabilities)
		{
			var newCapabilities = kai.Capabilities | capabilities;

			kai.Capabilities = newCapabilities;
			if(kai.Capabilities == 0)
				return;
			if (!Authenticated)
				return;

			var json = new JObject
			{
				[Constants.Type] = Constants.SetCapabilities
			};

			if (ReferenceEquals(kai, DefaultKai))
			{
				json.Add(Constants.KaiID, Constants.Default);
			}
			else if (ReferenceEquals(kai, DefaultLeftKai))
			{
				json.Add(Constants.KaiID, Constants.DefaultLeft);
			}
			else if (ReferenceEquals(kai, DefaultRightKai))
			{
				json.Add(Constants.KaiID, Constants.DefaultRight);
			}
			else
			{
				json.Add(Constants.KaiID, kai.KaiID);
			}

			if (capabilities.HasFlag(KaiCapabilities.GestureData))
				json.Add(Constants.GestureData, true);

			if (capabilities.HasFlag(KaiCapabilities.LinearFlickData))
				json.Add(Constants.LinearFlickData, true);

			if (capabilities.HasFlag(KaiCapabilities.FingerShortcutData))
				json.Add(Constants.FingerShortcutData, true);

			if (capabilities.HasFlag(KaiCapabilities.FingerPositionalData))
				json.Add(Constants.FingerPositionalData, true);

			if (capabilities.HasFlag(KaiCapabilities.PYRData))
				json.Add(Constants.PYRData, true);

			if (capabilities.HasFlag(KaiCapabilities.QuaternionData))
				json.Add(Constants.QuaternionData, true);

			if (capabilities.HasFlag(KaiCapabilities.AccelerometerData))
				json.Add(Constants.AccelerometerData, true);

			if (capabilities.HasFlag(KaiCapabilities.GyroscopeData))
				json.Add(Constants.GyroscopeData, true);

			if (capabilities.HasFlag(KaiCapabilities.MagnetometerData))
				json.Add(Constants.MagnetometerData, true);

			Send(json.ToString(Formatting.None));
		}

		/// <summary>
		/// Unset the Kai's capabilities and subscribes to that data
		/// </summary>
		/// <param name="capabilities">The capabilities to set the Kai to</param>
		/// <param name="kai">The kai to set the capabilities to</param>
		public static void UnsetCapabilities(Kai kai, KaiCapabilities capabilities)
		{
			kai.Capabilities &= ~capabilities; // value = value AND NOT parameter. This will unset the parameter from the value
			if (!Authenticated)
				return;

			var json = new JObject
			{
				[Constants.Type] = Constants.SetCapabilities
			};

			if (ReferenceEquals(kai, DefaultKai))
			{
				json.Add(Constants.KaiID, Constants.Default);
			}
			else if (ReferenceEquals(kai, DefaultLeftKai))
			{
				json.Add(Constants.KaiID, Constants.DefaultLeft);
			}
			else if (ReferenceEquals(kai, DefaultRightKai))
			{
				json.Add(Constants.KaiID, Constants.DefaultRight);
			}
			else
			{
				json.Add(Constants.KaiID, kai.KaiID);
			}

			if (capabilities.HasFlag(KaiCapabilities.GestureData))
				json.Add(Constants.GestureData, false);

			if (capabilities.HasFlag(KaiCapabilities.LinearFlickData))
				json.Add(Constants.LinearFlickData, false);

			if (capabilities.HasFlag(KaiCapabilities.FingerShortcutData))
				json.Add(Constants.FingerShortcutData, false);

			if (capabilities.HasFlag(KaiCapabilities.FingerPositionalData))
				json.Add(Constants.FingerPositionalData, false);

			if (capabilities.HasFlag(KaiCapabilities.PYRData))
				json.Add(Constants.PYRData, false);

			if (capabilities.HasFlag(KaiCapabilities.QuaternionData))
				json.Add(Constants.QuaternionData, false);

			if (capabilities.HasFlag(KaiCapabilities.AccelerometerData))
				json.Add(Constants.AccelerometerData, false);

			if (capabilities.HasFlag(KaiCapabilities.GyroscopeData))
				json.Add(Constants.GyroscopeData, false);

			if (capabilities.HasFlag(KaiCapabilities.MagnetometerData))
				json.Add(Constants.MagnetometerData, false);

			Send(json.ToString(Formatting.None));
		}

		private static void ConnectInNewThread()
		{
			if (!initialised)
				throw new ApplicationException("You must call Initialise() before trying to get data");

			SetupConnections();

			SendAuth(ModuleID, ModuleSecret);

			// TODO check compatibility with SDK
			// Test compatibility
		}

		private static void SetupConnections()
		{
			webSocket = new WebSocket("ws://localhost:2203");
			webSocket.OnMessage += OnWebSocketMessage;
			while (true)
			{
				try
				{
					webSocket.Connect();
					break;
				}
				catch (WebSocketException)
				{
					Thread.Sleep(2000); // Potentially do exponential back-off here
				}
			}
		}

		private static void Send(string data)
		{
			if (webSocket != null)
			{
				Debug.Log("Sending " + data);
				webSocket.Send(data);
			}
		}

		private static void OnWebSocketMessage(object sender, MessageEventArgs e)
		{
			Handle(e.Data);
		}

		private static void SendAuth(string moduleId, string moduleSecret)
		{
			var json = new JObject
			{
				[Constants.Type] = Constants.Authentication,
				[Constants.ModuleId] = moduleId,
				[Constants.ModuleSecret] = moduleSecret
			};

			Send(json.ToString(Formatting.None));
		}

		private static void Handle(string data)
		{
			if (!initialised)
			{
				Debug.Log($"Received {data} before the listener was initialised. Ignoring...");
				return;
			}

			try
			{
				var input = JObject.Parse(data);

				var success = input[Constants.Success].ToObject<bool>();
				if (success != true)
				{
					DecodeSDKError(input);
					return;
				}

				var type = input[Constants.Type].ToObject<string>();

				switch (type)
				{
					case Constants.Authentication:
						DecodeAuthentication();
						break;
					case Constants.IncomingData:
						DecodeIncomingData(input);
						break;
					case Constants.ListConnectedKais:
						DecodeConnectedKais(input);
						break;
					case Constants.KaiConnected:
						DecodeKaiConnected(input);
						break;
					default:
						UnknownData?.Invoke(input);
						break;
				}
			}
			catch (Exception e)
			{
				// Ignore if the data is not formatted properly
				Debug.Log($"Error parsing JSON. Received: {data}. Error: {e.GetType().Name} - {e.Message}: {e.StackTrace}");
			}
		}

		private static void DecodeSDKError(JObject input)
		{
			var errorCode = input[Constants.ErrorCode].ToObject<int>();
			var error = input[Constants.Error].ToObject<string>();
			var message = input[Constants.Message].ToObject<string>();

			lock (actions)
			{
				actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
				{
					Error?.Invoke(sender, e as ErrorEventArgs);
				}, null, new ErrorEventArgs(errorCode, error, message)));
			}
		}

		private static void DecodeIncomingData(JObject input)
		{
			ForegroundProcess = input[Constants.ForegroundProcess].ToObject<string>();
			var kaiId = input[Constants.KaiID].ToObject<int>();
			var kai = connectedKais[kaiId];
			var defaultKai = input[Constants.DefaultKai]?.ToObject<bool>();
			var defaultLeftKai = input[Constants.DefaultLeftKai]?.ToObject<bool>();
			var defaultRightKai = input[Constants.DefaultRightKai]?.ToObject<bool>();

			var dataList = input[Constants.Data].ToObject<JArray>();

			if (dataList == null)
			{
				Debug.Log($"Data list is null. Received: {input}");
				return;
			}

			foreach (var data in dataList)
			{
				if (data.Type != JTokenType.Object)
				{
					Debug.Log($"Data is not an object. Received: {data}");
					continue;
				}

				var dataObject = data.ToObject<JObject>();
				var type = dataObject[Constants.Type].ToObject<string>();

				switch (type)
				{
					case Constants.GestureData:
						ParseGestureData(dataObject);
						break;
					case Constants.FingerShortcutData:
						ParseFingerShortcutData(dataObject);
						break;
					case Constants.PYRData:
						ParsePYRData(dataObject);
						break;
					case Constants.QuaternionData:
						ParseQuaternionData(dataObject);
						break;
					case Constants.LinearFlickData:
						ParseLinearFlickData(dataObject);
						break;
					case Constants.FingerPositionalData:
						ParseFingerPositionalData(dataObject);
						break;
					case Constants.AccelerometerData:
						ParseAccelerometerData(dataObject);
						break;
					case Constants.GyroscopeData:
						ParseGyroscopeData(dataObject);
						break;
					case Constants.MagnetometerData:
						ParseMagnetometerData(dataObject);
						break;
					default:
						UnknownData.Invoke(input);
						break;
				}
			}

			void ParseGestureData(JObject data)
			{
				var gesture = data[Constants.Gesture].ToObject<string>();

				if (Enum.TryParse(gesture, true, out Gesture knownGesture))
				{
					FireGestureEvent(new GestureEventArgs(knownGesture));
				}
				else
				{
					FireUnknownGestureEvent(new UnknownGestureEventArgs(gesture));
				}
			}

			void ParseFingerShortcutData(JObject data)
			{
				var dataArray = data[Constants.Fingers].ToObject<JArray>();
				var array = new bool[4];

				for (var i = 0; i < dataArray.Count; i++)
				{
					array[i] = dataArray[i].ToObject<bool>();
				}

				FireFingerShortcutEvent(new FingerShortcutEventArgs(array));

			}

			void ParseQuaternionData(JObject data)
			{
				var json = data[Constants.Quaternion].ToObject<JObject>();

				var quaternion = new Quaternion
				{
					w = json[Constants.W].ToObject<float>(),
					x = json[Constants.X].ToObject<float>(),
					y = json[Constants.Y].ToObject<float>(),
					z = json[Constants.Z].ToObject<float>()
				};

				FireQuaternionEvent(new QuaternionEventArgs(quaternion));
			}

			void ParsePYRData(JObject json)
			{
				var pitch = json[Constants.Pitch].ToObject<float>();
				var yaw = json[Constants.Yaw].ToObject<float>();
				var roll = json[Constants.Roll].ToObject<float>();

				FirePYREvent(new PYREventArgs(pitch, yaw, roll));
			}

			void ParseLinearFlickData(JObject data)
			{
				var flick = data[Constants.Flick].ToObject<string>();

				FireLinearFlickEvent(new LinearFlickEventArgs(flick));
			}

			void ParseFingerPositionalData(JObject data)
			{
				var json = data[Constants.Fingers].ToObject<JObject>();
				var array = new int[4];

				for (var i = 0; i < json.Count; i++)
				{
					array[i] = json[i].ToObject<int>();
				}

				FireFingerPositionalEvent(new FingerPositionalEventArgs(array));
			}

			void ParseAccelerometerData(JObject data)
			{
				var json = data[Constants.Accelerometer].ToObject<JObject>();

				var accelerometer = new Vector3
				{
					x = json[Constants.X].ToObject<float>(),
					y = json[Constants.Y].ToObject<float>(),
					z = json[Constants.Z].ToObject<float>()
				};

				FireAccelerometerEvent(new AccelerometerEventArgs(accelerometer));
			}

			void ParseGyroscopeData(JObject data)
			{
				var json = data[Constants.Gyroscope].ToObject<JObject>();

				var gyroscope = new Vector3
				{
					x = json[Constants.X].ToObject<float>(),
					y = json[Constants.Y].ToObject<float>(),
					z = json[Constants.Z].ToObject<float>()
				};

				FireGyroscopeEvent(new GyroscopeEventArgs(gyroscope));
			}

			void ParseMagnetometerData(JObject data)
			{
				var json = data[Constants.Magnetometer].ToObject<JObject>();

				var magnetometer = new Vector3
				{
					x = json[Constants.X].ToObject<float>(),
					y = json[Constants.Y].ToObject<float>(),
					z = json[Constants.Z].ToObject<float>()
				};

				FireMagnetometerEvent(new MagnetometerEventArgs(magnetometer));
			}

			void FireGestureEvent(GestureEventArgs args)
			{
				lock (actions)
				{
					if (defaultKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).Gesture?.Invoke(sender, e as GestureEventArgs);
						}, DefaultKai, args));
					}
					if (defaultLeftKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).Gesture?.Invoke(sender, e as GestureEventArgs);
						}, DefaultLeftKai, args));
					}
					if (defaultRightKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).Gesture?.Invoke(sender, e as GestureEventArgs);
						}, DefaultRightKai, args));
					}

					actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
					{
						(sender as Kai).Gesture?.Invoke(sender, e as GestureEventArgs);
					}, kai, args));
					actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
					{
						(sender as Kai).Gesture?.Invoke(sender, e as GestureEventArgs);
					}, AnyKai, args));
				}
			}

			void FireUnknownGestureEvent(UnknownGestureEventArgs args)
			{
				lock (actions)
				{
					if (defaultKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).UnknownGesture?.Invoke(sender, e as UnknownGestureEventArgs);
						}, DefaultKai, args));
					}
					if (defaultLeftKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).UnknownGesture?.Invoke(sender, e as UnknownGestureEventArgs);
						}, DefaultLeftKai, args));
					}
					if (defaultRightKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).UnknownGesture?.Invoke(sender, e as UnknownGestureEventArgs);
						}, DefaultRightKai, args));
					}


					actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
					{
						(sender as Kai).UnknownGesture?.Invoke(sender, e as UnknownGestureEventArgs);
					}, kai, args));
					actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
					{
						(sender as Kai).UnknownGesture?.Invoke(sender, e as UnknownGestureEventArgs);
					}, AnyKai, args));
				}
			}

			void FireLinearFlickEvent(LinearFlickEventArgs args)
			{
				lock (actions)
				{
					if (defaultKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).LinearFlick?.Invoke(sender, e as LinearFlickEventArgs);
						}, DefaultKai, args));
					}
					if (defaultLeftKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).LinearFlick?.Invoke(sender, e as LinearFlickEventArgs);
						}, DefaultLeftKai, args));
					}
					if (defaultRightKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).LinearFlick?.Invoke(sender, e as LinearFlickEventArgs);
						}, DefaultRightKai, args));
					}


					actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
					{
						(sender as Kai).LinearFlick?.Invoke(sender, e as LinearFlickEventArgs);
					}, kai, args));
					actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
					{
						(sender as Kai).LinearFlick?.Invoke(sender, e as LinearFlickEventArgs);
					}, AnyKai, args));
				}
			}

			void FireFingerShortcutEvent(FingerShortcutEventArgs args)
			{
				lock (actions)
				{
					if (defaultKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).FingerShortcut?.Invoke(sender, e as FingerShortcutEventArgs);
						}, DefaultKai, args));
					}
					if (defaultLeftKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).FingerShortcut?.Invoke(sender, e as FingerShortcutEventArgs);
						}, DefaultLeftKai, args));
					}
					if (defaultRightKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).FingerShortcut?.Invoke(sender, e as FingerShortcutEventArgs);
						}, DefaultRightKai, args));
					}

					actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
					{
						(sender as Kai).FingerShortcut?.Invoke(sender, e as FingerShortcutEventArgs);
					}, kai, args));
					actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
					{
						(sender as Kai).FingerShortcut?.Invoke(sender, e as FingerShortcutEventArgs);
					}, AnyKai, args));
				}
			}

			void FireFingerPositionalEvent(FingerPositionalEventArgs args)
			{
				lock (actions)
				{
					if (defaultKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).FingerPositionalData?.Invoke(sender, e as FingerPositionalEventArgs);
						}, DefaultKai, args));
					}
					if (defaultLeftKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).FingerPositionalData?.Invoke(sender, e as FingerPositionalEventArgs);
						}, DefaultLeftKai, args));
					}
					if (defaultRightKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).FingerPositionalData?.Invoke(sender, e as FingerPositionalEventArgs);
						}, DefaultRightKai, args));
					}

					actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
					{
						(sender as Kai).FingerPositionalData?.Invoke(sender, e as FingerPositionalEventArgs);
					}, kai, args));
					actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
					{
						(sender as Kai).FingerPositionalData?.Invoke(sender, e as FingerPositionalEventArgs);
					}, AnyKai, args));
				}
			}

			void FirePYREvent(PYREventArgs args)
			{
				lock (actions)
				{
					if (defaultKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).PYRData?.Invoke(sender, e as PYREventArgs);
						}, DefaultKai, args));
					}
					if (defaultLeftKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).PYRData?.Invoke(sender, e as PYREventArgs);
						}, DefaultLeftKai, args));
					}
					if (defaultRightKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).PYRData?.Invoke(sender, e as PYREventArgs);
						}, DefaultRightKai, args));
					}

					actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
					{
						(sender as Kai).PYRData?.Invoke(sender, e as PYREventArgs);
					}, kai, args));
					actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
					{
						(sender as Kai).PYRData?.Invoke(sender, e as PYREventArgs);
					}, AnyKai, args));
				}
			}

			void FireQuaternionEvent(QuaternionEventArgs args)
			{
				lock (actions)
				{
					if (defaultKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).QuaternionData?.Invoke(sender, e as QuaternionEventArgs);
						}, DefaultKai, args));
					}
					if (defaultLeftKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).QuaternionData?.Invoke(sender, e as QuaternionEventArgs);
						}, DefaultLeftKai, args));
					}
					if (defaultRightKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).QuaternionData?.Invoke(sender, e as QuaternionEventArgs);
						}, DefaultRightKai, args));
					}

					actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
					{
						(sender as Kai).QuaternionData?.Invoke(sender, e as QuaternionEventArgs);
					}, kai, args));
					actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
					{
						(sender as Kai).QuaternionData?.Invoke(sender, e as QuaternionEventArgs);
					}, AnyKai, args));
				}
			}

			void FireAccelerometerEvent(AccelerometerEventArgs args)
			{
				lock (actions)
				{
					if (defaultKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).AccelerometerData?.Invoke(sender, e as AccelerometerEventArgs);
						}, DefaultKai, args));
					}
					if (defaultLeftKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).AccelerometerData?.Invoke(sender, e as AccelerometerEventArgs);
						}, DefaultLeftKai, args));
					}
					if (defaultRightKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).AccelerometerData?.Invoke(sender, e as AccelerometerEventArgs);
						}, DefaultRightKai, args));
					}

					actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
					{
						(sender as Kai).AccelerometerData?.Invoke(sender, e as AccelerometerEventArgs);
					}, kai, args));
					actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
					{
						(sender as Kai).AccelerometerData?.Invoke(sender, e as AccelerometerEventArgs);
					}, AnyKai, args));
				}
			}

			void FireGyroscopeEvent(GyroscopeEventArgs args)
			{
				lock (actions)
				{
					if (defaultKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).GyroscopeData?.Invoke(sender, e as GyroscopeEventArgs);
						}, DefaultKai, args));
					}
					if (defaultLeftKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).GyroscopeData?.Invoke(sender, e as GyroscopeEventArgs);
						}, DefaultLeftKai, args));
					}
					if (defaultRightKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).GyroscopeData?.Invoke(sender, e as GyroscopeEventArgs);
						}, DefaultRightKai, args));
					}

					actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
					{
						(sender as Kai).GyroscopeData?.Invoke(sender, e as GyroscopeEventArgs);
					}, kai, args));
					actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
					{
						(sender as Kai).GyroscopeData?.Invoke(sender, e as GyroscopeEventArgs);
					}, AnyKai, args));
				}
			}

			void FireMagnetometerEvent(MagnetometerEventArgs args)
			{
				lock (actions)
				{
					if (defaultKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).GyroscopeData?.Invoke(sender, e as GyroscopeEventArgs);
						}, DefaultKai, args));
					}
					if (defaultLeftKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).GyroscopeData?.Invoke(sender, e as GyroscopeEventArgs);
						}, DefaultLeftKai, args));
					}
					if (defaultRightKai == true)
					{
						actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
						{
							(sender as Kai).GyroscopeData?.Invoke(sender, e as GyroscopeEventArgs);
						}, DefaultRightKai, args));
					}

					actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
					{
						(sender as Kai).GyroscopeData?.Invoke(sender, e as GyroscopeEventArgs);
					}, kai, args));
					actions.AddLast(new Tuple<Action<object, EventArgs>, object, EventArgs>((sender, e) =>
					{
						(sender as Kai).GyroscopeData?.Invoke(sender, e as GyroscopeEventArgs);
					}, AnyKai, args));
				}
			}
		}

		private static void DecodeAuthentication()
		{
			Authenticated = true;
			GetConnectedKais();
		}

		private static void DecodeConnectedKais(JObject input)
		{
			var kaiList = input[Constants.Kais].ToObject<JArray>();
			foreach (var token in kaiList)
				DecodeKaiConnected((JObject)token);
		}

		private static void DecodeKaiConnected(JObject input)
		{
			var kaiID = input[Constants.KaiID].ToObject<int>();
			var hand = input[Constants.Hand]?.ToObject<string>(); // will not be optional in future
			var defaultKai = input[Constants.DefaultKai]?.ToObject<bool>();
			var defaultLeftKai = input[Constants.DefaultLeftKai]?.ToObject<bool>();
			var defaultRightKai = input[Constants.DefaultRightKai]?.ToObject<bool>();
			var kaiSerialNumber = input[Constants.KaiSerialNumber]?.ToObject<bool>(); // will not be optional in future

			//var kaiParsed = KaiObjectParsed.Parse(input);
			if (!Enum.TryParse(hand, true, out Hand handEnum))
				handEnum = Hand.Left;

			if (defaultKai == true)
			{
				DefaultKai.KaiID = kaiID;
				DefaultKai.Hand = handEnum;
			}

			if (defaultLeftKai == true)
			{
				DefaultLeftKai.KaiID = kaiID;
				DefaultLeftKai.Hand = Hand.Left;
			}

			if (defaultRightKai == true)
			{
				DefaultRightKai.KaiID = kaiID;
				DefaultRightKai.Hand = Hand.Right;
			}

			connectedKais[kaiID] = new Kai
			{
				KaiID = kaiID,
				Hand = handEnum
			};

			if (defaultKai == true || defaultLeftKai == true || defaultRightKai == true)
			{
				ResetDefaultCapabilities();
			}
		}

		private static void ResetDefaultCapabilities()
		{
			if (DefaultKai.Capabilities != 0)
				DefaultKai.SetCapabilities(DefaultKai.Capabilities);
			
			if (DefaultLeftKai.Capabilities != 0)
				DefaultLeftKai.SetCapabilities(DefaultLeftKai.Capabilities);

			if (DefaultRightKai.Capabilities != 0)
				DefaultRightKai.SetCapabilities(DefaultRightKai.Capabilities);
		}
	}
}

using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;


// Here is an expanded version of the EventManager from: 
// https://unity3d.com/learn/tutorials/topics/scripting/events-creating-simple-messaging-system
// C# generics can be frustrating if you haven't seen them or C++ templates before. 
// This code implements the generics support you need for varying param counts.
// It supports all the different parameter counts of the UnityEvents. 
// Also, it uses class type as dictionary key instead of strings.
// 
// Jeff Wilson (jeff@imtc.gatech.edu)




// Also, here are some demonstrations of use:

////Example custom event. the param types can be pretty much anything. not just floats
//public class TwoParamEvent : UnityEvent<float, float> { }
//
////two-param unity action
//private UnityAction<float, float> twoParamEventListener;
//
//    ....
//
//    //twoParamHandler shows defined below
//    twoParamEventListener = new UnityAction<float, float> (twoParamHandler);
//
//    ....
//
//    //note that we no longer pass a string event name. the first generic param handles ID of event type
//    EventManager.TriggerEvent<TwoParamEvent, float, float> ( 3f, 5f);
//
//    ....
//
//    EventManager.StartListening<TwoParamEvent, float, float> ( twoParamEventListener);
//
//    .....
//
//    EventManager.StopListening<TwoParamEvent, float, float> (twoParamEventListener);
//
//    .....
//
//    void twoParamHandler (float a, float b)
//{
//
//    float total = a + b;
//
//    //no string builder b/c I'm lazy
//    print ("a (" + a.ToString () + ") + b (" + b.ToString () + ") = " + total.ToString ());
//
//}


public class EventManager : MonoBehaviour
{

		private Dictionary <System.Type, UnityEventBase> eventDictionary;

		private static EventManager eventManager;

		public static EventManager instance {
				get {
						if (!eventManager) {
								eventManager = FindObjectOfType (typeof(EventManager)) as EventManager;

								if (!eventManager) {
										Debug.LogError ("There needs to be one active EventManger script on a GameObject in your scene.");
								} else {
										eventManager.Init (); 
								}
						}

						return eventManager;
				}
		}

		void Init ()
		{
				if (eventDictionary == null) {
						eventDictionary = new Dictionary<System.Type, UnityEventBase> ();
				}


		}



		public static void StartListening<Tbase> (UnityAction listener) where Tbase : UnityEvent, new()
		{

				UnityEventBase thisEvent = null;

				if (instance.eventDictionary.TryGetValue (typeof(Tbase), out thisEvent)) {
						Tbase e = thisEvent as Tbase;

						if (e != null)
								e.AddListener (listener);
						else
								Debug.LogError ("EventManager.StartListening() FAILED! Event type " + typeof(Tbase).ToString() + " could not be accessed for some strange reason.");
				} else {
						Tbase e = new Tbase ();
						e.AddListener (listener);
						instance.eventDictionary.Add (typeof(Tbase), e);
				}
		}



		public static void StartListening<Tbase, T0> (UnityAction<T0> listener) where Tbase : UnityEvent<T0>, new()
		{

				UnityEventBase thisEvent = null;

				if (instance.eventDictionary.TryGetValue (typeof(Tbase), out thisEvent)) {
						Tbase e = thisEvent as Tbase;

						if (e != null)
								e.AddListener (listener);
						else
								Debug.LogError ("EventManager.StartListening() FAILED! Event type " + typeof(Tbase).ToString() + " could not be accessed for some strange reason.");
				} else {
						Tbase e = new Tbase ();
						e.AddListener (listener);
						instance.eventDictionary.Add (typeof(Tbase), e);
				}
		}



		public static void StartListening<Tbase, T0, T1> (UnityAction<T0, T1> listener) where Tbase : UnityEvent<T0, T1>, new()
		{

				UnityEventBase thisEvent = null;
				if (instance.eventDictionary.TryGetValue (typeof(Tbase), out thisEvent)) {
						Tbase e = thisEvent as Tbase;

						if (e != null)
								e.AddListener (listener);
						else
								Debug.LogError ("EventManager.StartListening() FAILED! Event type " + typeof(Tbase).ToString() + " could not be accessed for some strange reason.");
				} else {
						Tbase e = new Tbase ();
						e.AddListener (listener);
						instance.eventDictionary.Add (typeof(Tbase), e);
				}
		}


		public static void StartListening<Tbase, T0, T1, T2> (UnityAction<T0, T1, T2> listener) where Tbase : UnityEvent<T0, T1, T2>, new()
		{

				UnityEventBase thisEvent = null;
				if (instance.eventDictionary.TryGetValue (typeof(Tbase), out thisEvent)) {
						Tbase e = thisEvent as Tbase;

						if (e != null)
								e.AddListener (listener);
						else
								Debug.LogError ("EventManager.StartListening() FAILED! Event type " + typeof(Tbase).ToString() + " could not be accessed for some strange reason.");
				} else {
						Tbase e = new Tbase ();
						e.AddListener (listener);
						instance.eventDictionary.Add (typeof(Tbase), e);
				}
		}



		public static void StartListening<Tbase, T0, T1, T2, T3> (UnityAction<T0, T1, T2, T3> listener) where Tbase : UnityEvent<T0, T1, T2, T3>, new()
		{

				UnityEventBase thisEvent = null;
				if (instance.eventDictionary.TryGetValue (typeof(Tbase), out thisEvent)) {
						Tbase e = thisEvent as Tbase;

						if (e != null)
								e.AddListener (listener);
						else
								Debug.LogError ("EventManager.StartListening() FAILED! Event type " + typeof(Tbase).ToString() + " could not be accessed for some strange reason.");
				} else {
						Tbase e = new Tbase ();
						e.AddListener (listener);
						instance.eventDictionary.Add (typeof(Tbase), e);
				}
		}
				

		public static void StopListening<Tbase> (UnityAction listener) where Tbase : UnityEvent
		{
				if (eventManager == null)
						return;
				UnityEventBase thisEvent = null;
				if (instance.eventDictionary.TryGetValue (typeof(Tbase), out thisEvent)) {
						Tbase e = thisEvent as Tbase;

						if (e != null)
								e.RemoveListener (listener);
						else
								Debug.LogError ("EventManager.StopListening() FAILED! Event type " + typeof(Tbase).ToString() + " could not be accessed for some strange reason.");
				}
		}

		public static void StopListening<Tbase, T0> (UnityAction<T0> listener) where Tbase : UnityEvent<T0>
		{
				if (eventManager == null)
						return;
				UnityEventBase thisEvent = null;
				if (instance.eventDictionary.TryGetValue (typeof(Tbase), out thisEvent)) {
						Tbase e = thisEvent as Tbase;

						if (e != null)
								e.RemoveListener (listener);
						else
								Debug.LogError ("EventManager.StopListening() FAILED! Event type " + typeof(Tbase).ToString() + " could not be accessed for some strange reason.");
				}
		}


		public static void StopListening<Tbase, T0, T1> (UnityAction<T0, T1> listener) where Tbase : UnityEvent<T0, T1>
		{
				if (eventManager == null)
						return;
				UnityEventBase thisEvent = null;
				if (instance.eventDictionary.TryGetValue (typeof(Tbase), out thisEvent)) {
						Tbase e = thisEvent as Tbase;

						if (e != null)
								e.RemoveListener (listener);
						else
								Debug.LogError ("EventManager.StopListening() FAILED! Event type " + typeof(Tbase).ToString() + " could not be accessed for some strange reason.");
				}
		}



		public static void StopListening<Tbase, T0, T1, T2> (UnityAction<T0, T1, T2> listener) where Tbase : UnityEvent<T0, T1, T2>
		{
				if (eventManager == null)
						return;
				UnityEventBase thisEvent = null;
				if (instance.eventDictionary.TryGetValue (typeof(Tbase), out thisEvent)) {
						Tbase e = thisEvent as Tbase;

						if (e != null)
								e.RemoveListener (listener);
						else
								Debug.LogError ("EventManager.StopListening() FAILED! Event type " + typeof(Tbase).ToString() + " could not be accessed for some strange reason.");
				}
		}


		public static void StopListening<Tbase, T0, T1, T2, T3> (UnityAction<T0, T1, T2, T3> listener) where Tbase : UnityEvent<T0, T1, T2, T3>
		{
				if (eventManager == null)
						return;
				UnityEventBase thisEvent = null;
				if (instance.eventDictionary.TryGetValue (typeof(Tbase), out thisEvent)) {
						Tbase e = thisEvent as Tbase;

						if (e != null)
								e.RemoveListener (listener);
						else
								Debug.LogError ("EventManager.StopListening() FAILED! Event type " + typeof(Tbase).ToString() + " could not be accessed for some strange reason.");
				}
		}



		public static void TriggerEvent<Tbase> () where Tbase : UnityEvent
		{
				UnityEventBase thisEvent = null;
				if (instance.eventDictionary.TryGetValue (typeof(Tbase), out thisEvent)) {

						Tbase e = thisEvent as Tbase;

						if (e != null)
								e.Invoke ();
						else
								Debug.LogError ("EventManager.TriggerEvent() FAILED! Event type " + typeof(Tbase).ToString() + " could not be accessed for some strange reason.");
				}
		}

		public static void TriggerEvent<Tbase, T0> (T0 t0_obj) where Tbase : UnityEvent<T0>
		{
				UnityEventBase thisEvent = null;
				if (instance.eventDictionary.TryGetValue (typeof(Tbase), out thisEvent)) {

						Tbase e = thisEvent as Tbase;

						if (e != null)
								e.Invoke (t0_obj);
						else
								Debug.LogError ("EventManager.TriggerEvent() FAILED! Event type " + typeof(Tbase).ToString() + " could not be accessed for some strange reason.");
				}
		}


		public static void TriggerEvent<Tbase, T0, T1> (T0 t0_obj, T1 t1_obj) where Tbase : UnityEvent<T0, T1>
		{
				UnityEventBase thisEvent = null;
				if (instance.eventDictionary.TryGetValue (typeof(Tbase), out thisEvent)) {

						Tbase e = thisEvent as Tbase;

						if (e != null)
								e.Invoke (t0_obj, t1_obj);
						else
								Debug.LogError ("EventManager.TriggerEvent() FAILED! Event type " + typeof(Tbase).ToString() + " could not be accessed for some strange reason.");
				}
		}


		public static void TriggerEvent<Tbase, T0, T1, T2> (T0 t0_obj, T1 t1_obj, T2 t2_obj) where Tbase : UnityEvent<T0, T1, T2>
		{
				UnityEventBase thisEvent = null;
				if (instance.eventDictionary.TryGetValue (typeof(Tbase), out thisEvent)) {

						Tbase e = thisEvent as Tbase;

						if (e != null)
								e.Invoke (t0_obj, t1_obj, t2_obj);
						else
								Debug.LogError ("EventManager.TriggerEvent() FAILED! Event type " + typeof(Tbase).ToString() + " could not be accessed for some strange reason.");
				}
		}


		public static void TriggerEvent<Tbase, T0, T1, T2, T3> (T0 t0_obj, T1 t1_obj, T2 t2_obj, T3 t3_obj) where Tbase : UnityEvent<T0, T1, T2, T3>
		{
				UnityEventBase thisEvent = null;
				if (instance.eventDictionary.TryGetValue (typeof(Tbase), out thisEvent)) {

						Tbase e = thisEvent as Tbase;

						if (e != null)
								e.Invoke (t0_obj, t1_obj, t2_obj, t3_obj);
						else
								Debug.LogError ("EventManager.TriggerEvent() FAILED! Event type " + typeof(Tbase).ToString() + " could not be accessed for some strange reason.");
				}
		}
}

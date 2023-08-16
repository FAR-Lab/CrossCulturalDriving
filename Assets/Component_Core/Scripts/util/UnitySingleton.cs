/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;

/// <summary>
///     Abstract Unity Component Singleton.
/// </summary>
public abstract class UnitySingleton<T> : MonoBehaviour where T : Component {
    protected static T _instance;

    public static bool IsInstantiated => _instance != default(T);

    public static T Instance {
        get {
            if (_instance == null) {
                var t = (T)FindObjectOfType(typeof(T));
                if (t) {
                    _instance = t;
                }
                else {
                    var go = new GameObject();
                    _instance = go.AddComponent<T>();
                    go.name = typeof(T).ToString();
                }
            }

            return _instance;
        }
    }

    public static T GetInstance<U>() where U : T {
        if (_instance == null || !(_instance is U)) {
            var u = FindObjectOfType(typeof(U)) as U;
            if (u != null) {
                _instance = u;
            }
            else {
                var go = new GameObject();
                _instance = go.AddComponent<U>();
                go.name = typeof(U).ToString();
            }
        }

        return _instance;
    }

    #region Unity events

    protected virtual void Awake() {
        _instance = Instance;
    }


    protected virtual void OnDestroy() {
        if (_instance == this)
            _instance = default;
    }


    protected virtual void OnApplicationQuit() {
        if (_instance == this)
            _instance = default;
    }

    #endregion
}


public abstract class PersistentUnitySingleton<T> : UnitySingleton<T> where T : Component {
    protected override void Awake() {
        base.Awake();
        if (_instance == this)
            DontDestroyOnLoad(gameObject);
    }
}
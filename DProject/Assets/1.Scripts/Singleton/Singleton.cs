using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    private static object _lock = new object();

    public static T Instance
    {
        get
        {
            if (applicationIsQuitting)
            {
                Debug.LogWarning("[Singleton]'" + typeof(T) +
                    "'의 객체는 이미 이 프로그램에서는 파괴되었다." +
                    " 절대 다시 생성하면 안됩니다. - Null값만 반환합니다.");
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = (T)FindObjectOfType(typeof(T));

                    if (FindObjectsOfType(typeof(T)).Length > 1)
                    {
                        Debug.LogError("[Singleton] 무엇인가 잘못되었다." +
                            " - 싱글톤은 절대 1개 이상 있으면 안된다!" +
                            " 재시작을 해보면 해결할 수 있습니다..");
                        return _instance;
                    }

                    if (_instance == null)
                    {
                        GameObject singleton = new GameObject();
                        _instance = singleton.AddComponent<T>();
                        singleton.name = "(singleton) " + typeof(T).ToString();

                        DontDestroyOnLoad(singleton);

                        Debug.Log("[Singleton]  " + typeof(T) +
                            " 의 객체가 scene에 필요합니다, '" + singleton +
                            "' 을 DontDestroyOnLoad 로 생성합니다.");
                    }
                    else
                    {
                        Debug.Log("[Singleton] 이미 생성되어 사용중인 객체: " +
                            _instance.gameObject.name);
                    }
                }

                return _instance;
            }
        }

    }

    private static bool applicationIsQuitting = false;
    /// <summary>
    /// 유니티가 종료되면 임의의 순서로 객체를 파괴합니다.
    /// 원칙적으로 싱글톤은 응용 프로그램이 종료될때만 파괴됩니다.
    /// 어떤 스크립트가 객체가 파괴된 후에 인스턴스를 호출하면
    ///   그것은 에디터에 머무를 버그있는 유령 객체를 만든다.
    ///  응용프로그램을 종료하더라도!
    /// 그래서 이것은 버그 없는 유령 객체를 생성하지 않는다는것을 확실히 하기 위해 만들어졌다.
    /// </summary>
    public void OnDestroy()
    {
        applicationIsQuitting = true;
    }
}

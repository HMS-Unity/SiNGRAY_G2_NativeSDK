using UnityEngine;


namespace Singray.Foundation
{
   internal class XvAndroidHelper
    {

        //****************** android java call unity 

        public static AndroidJavaClass GetClass(string className)
        {
            try
            {
                return new AndroidJavaClass(className);
            }
            catch (AndroidJavaException e)
            {
                Debug.LogError("Exception getting class " + className + ": " + e);
                return null;
            }
        }

        public static AndroidJavaObject Create(string className, params object[] args)
        {
            try
            {
                return new AndroidJavaObject(className, args);
            }
            catch (AndroidJavaException e)
            {
                Debug.LogError("Exception creating object " + className + ": " + e);
                return null;
            }
        }

        public static bool CallStaticMethod(AndroidJavaObject jo, string name, params object[] args)
        {
            if (jo == null)
            {
                Debug.LogError("Object is null when calling static method " + name);
                return false;
            }
            try
            {
                jo.CallStatic(name, args);
                return true;
            }
            catch (AndroidJavaException e)
            {
                Debug.LogError("Exception calling static method " + name + ": " + e);
                return false;
            }
        }

        public static bool CallObjectMethod(AndroidJavaObject jo, string name, params object[] args)
        {
            if (jo == null)
            {
                Debug.LogError("Object is null when calling method " + name);
                return false;
            }
            try
            {
                jo.Call(name, args);
                return true;
            }
            catch (AndroidJavaException e)
            {
                Debug.LogError("Exception calling method " + name + ": " + e);
                return false;
            }
        }

        public static bool CallStaticMethod<T>(ref T result, AndroidJavaObject jo, string name,
                                                  params object[] args)
        {
            if (jo == null)
            {
                Debug.LogError("Object is null when calling static method " + name);
                return false;
            }
            try
            {
                result = jo.CallStatic<T>(name, args);
                return true;
            }
            catch (AndroidJavaException e)
            {
                Debug.LogError("Exception calling static method " + name + ": " + e);
                return false;
            }
        }

        public static bool CallObjectMethod<T>(ref T result, AndroidJavaObject jo, string name,
                                                  params object[] args)
        {
            if (jo == null)
            {
                Debug.LogError("darren Object is null when calling method " + name);
                return false;
            }
            try
            {
                result = jo.Call<T>(name, args);
                return true;
            }
            catch (AndroidJavaException e)
            {
                Debug.LogError("darren Exception calling method " + name + ": " + e);
                return false;
            }
        }
    }
}

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Singray.utils
{
   internal class AndroidHelper
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
                XvXRLog.InternalXvXRLog("Exception getting class " + className + ": " + e);
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
                XvXRLog.InternalXvXRLog("Exception creating object " + className + ": " + e);
                return null;
            }
        }

        public static bool CallStaticMethod(AndroidJavaObject jo, string name, params object[] args)
        {
            if (jo == null)
            {
                XvXRLog.InternalXvXRLog("Object is null when calling static method " + name);
                return false;
            }
            try
            {
                jo.CallStatic(name, args);
                return true;
            }
            catch (AndroidJavaException e)
            {
                XvXRLog.InternalXvXRLog("Exception calling static method " + name + ": " + e);
                return false;
            }
        }

        public static bool CallObjectMethod(AndroidJavaObject jo, string name, params object[] args)
        {
            if (jo == null)
            {
                XvXRLog.InternalXvXRLog("Object is null when calling method " + name);
                return false;
            }
            try
            {
                jo.Call(name, args);
                return true;
            }
            catch (AndroidJavaException e)
            {
                XvXRLog.InternalXvXRLog("Exception calling method " + name + ": " + e);
                return false;
            }
        }

        public static bool CallStaticMethod<T>(ref T result, AndroidJavaObject jo, string name,
                                                  params object[] args)
        {
            if (jo == null)
            {
                XvXRLog.InternalXvXRLog("Object is null when calling static method " + name);
                return false;
            }
            try
            {
                result = jo.CallStatic<T>(name, args);
                return true;
            }
            catch (AndroidJavaException e)
            {
                XvXRLog.InternalXvXRLog("Exception calling static method " + name + ": " + e);
                return false;
            }
        }

        public static bool CallObjectMethod<T>(ref T result, AndroidJavaObject jo, string name,
                                                  params object[] args)
        {
            if (jo == null)
            {
                XvXRLog.InternalXvXRLog("darren Object is null when calling method " + name);
                return false;
            }
            try
            {
                result = jo.Call<T>(name, args);
                return true;
            }
            catch (AndroidJavaException e)
            {
                XvXRLog.InternalXvXRLog("darren Exception calling method " + name + ": " + e);
                return false;
            }
        }
    }
}

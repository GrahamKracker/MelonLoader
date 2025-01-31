﻿using System;
using System.Reflection;
using System.Runtime.InteropServices;
using MelonLoader;
using MelonLoader.Utils;

namespace MelonLoader.NativeUtils
{
    public static class MelonNativeLibrary
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        internal delegate string StringDelegate();
        
        public static bool TryLoad(string name, out IntPtr result)
        {
            bool wasSuccessful = false;
            try
            {
                result = Load(name);
                wasSuccessful = result != IntPtr.Zero;
            }
            catch { result = IntPtr.Zero; }
            return wasSuccessful;
        }

        public static IntPtr Load(string name)
        {
            // Check if passed valid Native Library name
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            // Get Native Library Pointer
            IntPtr ptr = BootstrapInterop.NativeLoadLib(name);
            if (ptr == IntPtr.Zero)
                throw new Exception($"Unable to Load Native Library {name}!");

            // Return Native Library Pointer
            return ptr;
        }

        public static bool TryGetExport(IntPtr handle, string name, out IntPtr result)
        {
            bool wasSuccessful = false;
            try
            {
                result = GetExport(handle, name);
                wasSuccessful = result != IntPtr.Zero;
            }
            catch { result = IntPtr.Zero; }
            return wasSuccessful;
        }

        public static IntPtr GetExport(IntPtr handle, string name)
        {
            // Check if being passed valid Pointer
            if (handle == IntPtr.Zero)
                throw new ArgumentNullException(nameof(handle));

            // Check if being passed valid Export Name
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            // Get the Export Pointer
            IntPtr returnval = BootstrapInterop.NativeGetExport(handle, name);
            if (returnval == IntPtr.Zero)
                throw new Exception($"Unable to Find Native Library Export {name}!");

            // Return the Export Pointer
            return returnval;
        }

        public static T ReflectiveLoad<T>(string name)
        {
            // Attempt to load Native Library
            if (!TryLoad(name, out IntPtr ptr)
                || ptr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(ptr));

            // Return Object Instance
            return ReflectiveLoad<T>(ptr);
        }

        public static T ReflectiveLoad<T>(IntPtr ptr)
        {
            // Attempt to load Native Library
            if (ptr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(ptr));

            // Get Reflected Type
            Type specifiedType = typeof(T);
            if (specifiedType.IsAbstract && specifiedType.IsSealed)
                throw new Exception($"Specified Type {specifiedType.FullName} must be Non-Static!");

            // Create new Object Instance
            T instance = (T)Activator.CreateInstance(specifiedType);

            // Scan fields of Reflected Type
            FieldInfo[] fields = specifiedType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fields.Length > 0)
                foreach (FieldInfo fieldInfo in fields)
                {
                    // Check Field for Delegate subtype and UnmanagedFunctionPointerAttribute
                    Type fieldType = fieldInfo.FieldType;
                    if (!typeof(Delegate).IsAssignableFrom(fieldType)
                        || fieldType.GetCustomAttributes(typeof(UnmanagedFunctionPointerAttribute), false).Length <= 0)
                        continue;

                    // Get Export from Native Library
                    if (!TryGetExport(ptr, fieldInfo.Name, out IntPtr expPtr)
                        || expPtr == IntPtr.Zero)
                        continue;

                    // Apply Export as Delegate to Field
                    fieldInfo.SetValue(instance, expPtr.GetDelegate(fieldType));
                }

            // Return Object Instance
            return instance;
        }
    }
}
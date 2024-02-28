using AOT;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.OpenXR.Features;
using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif

namespace Gdi.OpenXR
{
#if UNITY_EDITOR
    [OpenXRFeature(UiName = "Gdi: OpenXR Stereo",
        BuildTargetGroups = new[] { BuildTargetGroup.Standalone },
        Company = "Gdi",
        Desc = "",
        DocumentationLink = "",
        OpenxrExtensionStrings = "",
        Version = "0.0.1",
        FeatureId = FeatureId)]
#endif
    public class GdiOpenXRStereo : OpenXRFeature
    {
        public const string FeatureId = "com.gdi.linkxr-sdk.feature.stereo";

        private const string ExtLib = "OpenXrStereo";

        private static bool hasInitialize = false;

        private static int eyeIndex = 0;

        private bool useStereo = false;

        protected override IntPtr HookGetInstanceProcAddr(IntPtr func)
        {
            if(!XrManager.UseOpenXr())
                return IntPtr.Zero;

            useStereo = XrManager.UseStereo();
            
            if (!useStereo)
                return func;

            Internal_SetCallback(OnMessage);
            Internal_Xr_CallBack(OnXrCallBack);
            return intercept_xrCreateSession_xrGetInstanceProcAddr(func);
        }

        protected override bool OnInstanceCreate(ulong xrInstance)
        {
            if (!useStereo)
                return false;

            hasInitialize = false;
            eyeIndex = 0;

            Camera.onPreCull -= OnPreCullCamera;
            Camera.onPreCull += OnPreCullCamera;

            XrManager.ExecuteSafelyOnInitializeCompleted(() => {
                NativeRenderScreen renderScreen = XrManager.CalculateNativeRenderScreen();
                bool isEditor = Application.isEditor;
                string productName = Application.productName;
                hasInitialize = Internal_Initialize(renderScreen.posX, renderScreen.posY, renderScreen.width, renderScreen.height, isEditor, productName);
            });
            return true;
        }

        protected override void OnInstanceDestroy(ulong xrInstance)
        {
            if (!useStereo)
                return;

            Camera.onPreCull -= OnPreCullCamera;
            Internal_Destroy(false);
        }

        private void OnPreCullCamera(Camera camera)
        {
            eyeIndex %= XrManager.EyeNum;
            XrManager.UpdateCamera(eyeIndex, camera);
            eyeIndex += 1;
        }

        private delegate void OnMessageDelegate(string message);

        [MonoPInvokeCallback(typeof(OnMessageDelegate))]
        private static void OnMessage(string message)
        {
            if (message == null)
                return;

            Debug.Log(message);
        }

        private delegate void ReceiveMessageDelegate(string message);

        private delegate void XrBeginFrame(string name);

        [MonoPInvokeCallback(typeof(XrBeginFrame))]
        private static void OnXrCallBack(string name)
        {
            if (name == "xrBeginFrame")
            {
                eyeIndex = 0;
            }
        }

        [DllImport(ExtLib, EntryPoint = "script_initialize")]
        private static extern bool Internal_Initialize(int posX, int posY, int width, int height, bool isEditor, string windowName);

        [DllImport(ExtLib, EntryPoint = "script_set_callback")]
        private static extern void Internal_SetCallback(ReceiveMessageDelegate callback);

        [DllImport(ExtLib, EntryPoint = "script_set_xr_callback")]
        private static extern void Internal_Xr_CallBack(XrBeginFrame callback);

        [DllImport(ExtLib, EntryPoint = "script_intercept_xrCreateSession_xrGetInstanceProcAddr")]
        private static extern IntPtr intercept_xrCreateSession_xrGetInstanceProcAddr(IntPtr func);

        [DllImport(ExtLib, EntryPoint = "script_destroy")]
        private static extern void Internal_Destroy(bool force);

        [DllImport(ExtLib, EntryPoint = "script_log")]
        private static extern void Internal_Log(string value);
    }
}
using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Collections.Generic;

namespace VOS.Runtime.Web
{
    [CustomEditor(typeof(VOSRicohAPI))]
    public class VOSRicohInspector: Editor
    {
        public int sleepDelay = 65535;
        public int offDelay = 65535;

        public List<string> cameraOptions = new List<string>();

        public bool hdr = false;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            VOSRicohAPI api = (VOSRicohAPI)target;
            if (GUILayout.Button("Get Info"))
            {
                api.GetInfo((d) => Debug.Log(d));
            }

            if (GUILayout.Button("Get State"))
            {
                api.GetState((d) => Debug.Log(d));
            }

            if (GUILayout.Button("Get Camera Options"))
            {
                api.StartSession(() =>
                {
                    api.GetCameraOptions((d) =>
                    {
                        Debug.Log(d);
                        api.CloseSession();
                    },
                    new string[] { "iso", "shutterSpeed" });
                });
            }

            if (GUILayout.Button("Set Camera Options 01"))
            {
                api.StartSession(() =>
                {
                    api.SetCameraOptions(new CameraOptionExposureProgram { exposureProgram = 1 }, () =>
                    {
                        //api.CloseSession();
                    });
                });
            }

            if (GUILayout.Button("Set Camera Options 02"))
            {
                api.StartSession(() =>
                {
                    object mergedOptions = api.MakeCameraOptions(typeof(CameraOptionIso), typeof(CameraOptionShutterSpeed));
                    api.SetIsoAndShutterSpeed(ref mergedOptions, 100, 0.01f);
                    api.SetCameraOptions(mergedOptions, () =>
                    {
                        //api.CloseSession();
                    });
                });
            }

            hdr = EditorGUILayout.Toggle("HDR", hdr);
            if (GUILayout.Button("Capture Image"))
            {
                api.CaptureImage(hdr);
            }
        }
    }
}
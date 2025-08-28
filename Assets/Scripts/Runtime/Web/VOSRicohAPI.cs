using Newtonsoft.Json;
using System;
using System.Collections;
using UnityEngine;
using VOS.Core.Common;
using VOS.Core.Generics;

namespace VOS.Runtime.Web
{
    [Serializable]
    public struct RicohStateGroup
    {
        public string fingerprint;
        public RicohState state;
    }

    [Serializable]
    public struct RicohState
    {
        public string sessionId;
        public float batteryLevel;
        public bool storageChanged;
        public string _captureStatus;
        public int _recordedTime;
        public int _recordableTime;
        public string _latestFileUri;
        public string _batteryState;
        public string[] _cameraError;
    }

    [Serializable]
    public struct CommandExecute<T>
    {
        public string name;
        public T parameters;
    }

    [Serializable]
    public struct CameraGetOptionsParameters
    {
        public string sessionId;
        public string[] optionNames;
    }

    [Serializable]
    public struct CameraSetOptionsParameters<T>
    {
        public string sessionId;
        public T options;
    }

    [Serializable]
    public struct CameraOptionSleepDelay
    {
        public int sleepDelay;
    }

    [Serializable]
    public struct CameraOptionOffDelay
    {
        public int offDelay;
    }

    [Serializable]
    public struct CameraOptionExposureProgram
    {
        public int exposureProgram;
    }

    [Serializable]
    public struct CameraOptionIso
    {
        public int iso;
    }

    [Serializable]
    public struct CameraOptionShutterSpeed
    {
        public float shutterSpeed;
    }

    [Serializable]
    public struct SessionIDParam
    {
        public string sessionId;
    }

    [Serializable]
    public struct GetImg
    {
        public string fileUri;
        public string _type;
    }

    [Serializable]
    public struct StateFingerprint
    {
        public string stateFingerprint;
    }

    [Serializable]
    public struct CaptureImgResponse
    {
        public string name;
        public string state;
        public string id;
        public Progress progress;
    }

    [Serializable]
    public struct Progress
    {
        public int completion;
    }

    public class VOSRicohAPI : VOSMonoBehaviour
    {
        [SerializeField] private Texture2D capturedImage = null;

        private enum ExposureProgram { Manual = 1, Normal = 2, Shutter = 4, ISO = 9 }
        private int[] ISOValues = new int[] 
        { 
            100, 125, 160, 200, 250, 320, 400, 500, 640, 800, 1000, 1250, 1600 
        };
        private float[] ShutterSpeedValues = new float[] 
        {
            0.00015625f, 0.0002f, 0.00025f, 0.0003125f, 0.0004f, 0.0005f, 0.000625f, 0.0008f, 0.001f, 0.00125f, 0.0015625f, 0.002f, 0.0025f, 
            0.003125f, 0.004f, 0.005f, 0.00625f, 0.008f, 0.01f, 0.0125f, 0.01666666f, 0.02f, 0.025f, 0.03333333f, 0.04f, 0.05f, 0.06666666f, 0.07692307f, 0.1f, 0.125f, 0.16666666f, 0.2f, 0.25f, 
            0.33333333f, 0.4f, 0.5f, 0.625f, 0.76923076f, 1f, 1.3f, 1.6f, 2f, 2.5f, 3.2f, 4f, 5f, 6f, 8f, 10f, 13f, 15f, 20f, 25f, 30f, 60f 
        };
        private float[] hdrShutterSpeedValues = new float[]
        {
            0.001f, 0.002f, 0.004f, 0.008f, 0.01f, 0.02f, 0.04f, 0.1f, 0.125f, 0.2f, 0.25f, 0.4f, 1f, 1.6f, 2f
        };
        private const string baseURL = "http://192.168.1.1/";

        private string fingerprint = "";
        private string id = "";
        private string latestFileUri = "";

        #region Public Methods

        public void GetInfo(Action<string> result)
        {
            StartCoroutine(GetInfo_CR(result));
        }

        public void StartSession(Action onComplete = null)
        {
            StartCoroutine(StartSession_CR(onComplete));
        }

        public void CloseSession(Action onComplete = null)
        {
            StartCoroutine(CloseSession_CR(onComplete));
        }

        public void GetState(Action<string> result = null)
        {
            StartCoroutine(GetState_CR(result));
        }

        public void SetStateData(Action onComplete = null)
        {
            StartCoroutine(SetStateData_CR(onComplete));
        }

        public void GetCameraOptions(Action<string> result, params string[] options)
        {
            StartCoroutine(GetCameraOptions_CR(result, options));
        }

        public object MakeCameraOptions(params Type[] types)
        {
            Type cameraOptionsType = VOSCommon.MergeTypes("CameraOptions", types);
            return Activator.CreateInstance(cameraOptionsType);
        }

        public void SetIsoAndShutterSpeed(ref object mergedOptions, int isoValue, float shutterSpeedValue)
        {
            VOSCommon.SetMergedStruct(ref mergedOptions, typeof(CameraOptionIso), isoValue);
            VOSCommon.SetMergedStruct(ref mergedOptions, typeof(CameraOptionShutterSpeed), shutterSpeedValue);
        }

        public void SetCameraOptions(object options, Action onComplete = null)
        {
            StartCoroutine(SetCameraOptions_CR(options, onComplete));
        }

        public void CaptureImage(bool hdr, Action onComplete = null)
        {
            StartCoroutine(CaptureImage_CR(hdr, onComplete));
        }

        #endregion

        #region IEnumerator Methods

        private IEnumerator GetInfo_CR(Action<string> result)
        {
            yield return VOSWebRequest.Get(baseURL + "osc/info", result,
                (e) =>
                {
                    Debug.Log(e);
                });
        }

        private IEnumerator StartSession_CR(Action onComplete = null)
        {
            CommandExecute<string> startSession = new CommandExecute<string> { name = "camera.startSession", parameters = "{}" };
            yield return VOSWebRequest.Post(baseURL + "osc/commands/execute", startSession, VOSWebRequest.DataType.JSON,
                (d) =>
                {
                    Debug.Log("Session Started...");
                    onComplete?.Invoke();
                }, (c, e) =>
                {
                    Debug.Log(e);
                });
            yield return SetStateData_CR(onComplete);
        }

        private IEnumerator CloseSession_CR(Action onComplete = null)
        {
            CommandExecute<SessionIDParam> closeSession = new CommandExecute<SessionIDParam> { name = "camera.closeSession", parameters = new SessionIDParam { sessionId = id } };
            yield return VOSWebRequest.Post(baseURL + "osc/commands/execute", closeSession, VOSWebRequest.DataType.JSON,
                (d) =>
                {
                    Debug.Log("Session Closed...");
                    onComplete?.Invoke();
                }, (c, e) =>
                {
                    Debug.Log(e);
                });
        }

        private IEnumerator GetState_CR(Action<string> result = null)
        {
            yield return VOSWebRequest.Post(baseURL + "osc/state", "", VOSWebRequest.DataType.None, result,
                (c, e) =>
                {
                    Debug.Log(e);
                });
        }

        private IEnumerator SetStateData_CR(Action onComplete = null)
        {
            yield return GetState_CR((d) =>
            {
                RicohStateGroup rsg = JsonUtility.FromJson<RicohStateGroup>(d);
                fingerprint = rsg.fingerprint;
                latestFileUri = rsg.state._latestFileUri;
                id = rsg.state.sessionId;
            });
        }

        private IEnumerator GetCameraOptions_CR(Action<string> result, string[] options)
        {
            yield return SetStateData_CR();
            CommandExecute<CameraGetOptionsParameters> cameraGetOptionsParameters = new CommandExecute<CameraGetOptionsParameters>
            {
                name = "camera.getOptions",
                parameters = new CameraGetOptionsParameters
                {
                    sessionId = id,
                    optionNames = options
                }
            };
            yield return VOSWebRequest.Post(baseURL + "osc/commands/execute", cameraGetOptionsParameters, VOSWebRequest.DataType.JSON, 
                result,
                (c, e) =>
                {
                    Debug.Log(e);
                });
        }

        private IEnumerator SetCameraOptions_CR(object mergedOptions, Action onComplete = null)
        {
            yield return SetStateData_CR();
            CommandExecute<CameraSetOptionsParameters<object>> options = new CommandExecute<CameraSetOptionsParameters<object>>
            {
                name = "camera.setOptions",
                parameters = new CameraSetOptionsParameters<object>
                {
                    sessionId = id,
                    options = mergedOptions
                }
            };
            Debug.Log(JsonConvert.SerializeObject(options));
            yield return VOSWebRequest.Post(baseURL + "osc/commands/execute", options, VOSWebRequest.DataType.JSON,
                (d) =>
                {
                    onComplete?.Invoke();
                }, (c, e) =>
                {
                    Debug.Log(e);
                });
        }

        private IEnumerator CaptureImage_CR(bool hdr, Action onComplete = null)
        {
            yield return StartSession_CR();
            if (hdr)
            {
                yield return SetCameraOptions_CR(new CameraOptionExposureProgram { exposureProgram = (int)ExposureProgram.Manual });

                object mergedOptions = MakeCameraOptions(typeof(CameraOptionIso), typeof(CameraOptionShutterSpeed));
                for (int i = 0; i < hdrShutterSpeedValues.Length; i++)
                {
                    SetIsoAndShutterSpeed(ref mergedOptions, ISOValues[0], hdrShutterSpeedValues[i]);
                    yield return SetCameraOptions_CR(mergedOptions);
                    yield return CaptureImg_CR();
                    yield return CheckImageSaved();
                }
            }else
            {
                yield return SetCameraOptions_CR(new CameraOptionExposureProgram { exposureProgram = (int)ExposureProgram.Normal });
                yield return CaptureImg_CR((d) => 
                {
                    Debug.Log("Captured Image");
                });
                yield return GetImage();
            }
        }

        private IEnumerator CaptureImg_CR(Action<string> result = null)
        {
            CommandExecute<SessionIDParam> parameters = new CommandExecute<SessionIDParam>
            {
                name = "camera.takePicture",
                parameters = new SessionIDParam
                {
                    sessionId = id
                }
            };
            yield return VOSWebRequest.Post(baseURL + "osc/commands/execute", parameters, VOSWebRequest.DataType.JSON,
                (d) =>
                {
                    result?.Invoke(d);
                }, (c, e) =>
                {
                    Debug.Log(e);
                });
        }

        private IEnumerator WaitForUpdates(Action onComplete = null)
        {
            bool timeout = false;
            yield return VOSWebRequest.Post(baseURL + "osc/checkForUpdates", new StateFingerprint { stateFingerprint = fingerprint }, VOSWebRequest.DataType.JSON,
                (d) =>
                {
                    fingerprint = JsonUtility.FromJson<StateFingerprint>(d).stateFingerprint;
                    onComplete?.Invoke();
                }, (c, e) =>
                {
                    if (c == 408) timeout = true;
                    Debug.Log(e);
                });
            if (timeout)
            {
                Debug.Log("Update Timed Out");
                yield return new WaitForEndOfFrame();
                yield return WaitForUpdates(onComplete);
            }
        }

        private IEnumerator CheckImageSaved(bool isFileSaved = false)
        {
            yield return WaitForUpdates();
            yield return GetState_CR((d) =>
            {
                RicohStateGroup rsg = JsonUtility.FromJson<RicohStateGroup>(d);
                if (latestFileUri != rsg.state._latestFileUri)
                {
                    Debug.Log("File is saved.");
                    isFileSaved = true;
                    latestFileUri = rsg.state._latestFileUri;
                }
            });

            if (!isFileSaved)
            {
                Debug.Log("File not saved. Will Check Again.");
                yield return new WaitForEndOfFrame();
                yield return CheckImageSaved(isFileSaved);
            }
        }

        private IEnumerator GetImage()
        {
            yield return CheckImageSaved();
            CommandExecute<GetImg> getImgParams = new CommandExecute<GetImg>
            {
                name = "camera.getImage",
                parameters = new GetImg
                {
                    fileUri = latestFileUri,
                    _type = "full"
                }
            };
            yield return VOSWebRequest.Post_GetData(baseURL + "osc/commands/execute", getImgParams, VOSWebRequest.DataType.JSON,
                (d) =>
                {
                    Debug.Log("Image downloaded");
                    capturedImage = new Texture2D(5376, 2688);
                    capturedImage.LoadImage(d);
                    capturedImage.Apply();
                    Debug.Log("Image Loaded Into Texture2D");
                }, (c, e) =>
                {
                    Debug.Log(e);
                });
        }

        #endregion

        
    }
}
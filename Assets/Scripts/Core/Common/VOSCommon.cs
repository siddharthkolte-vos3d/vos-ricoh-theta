using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using VOS.Core.Enums;

namespace VOS.Core.Common
{
    public class VOSCommon
    {
        public static Type MergeTypes(string typeName, params Type[] types)
        {
            AssemblyName assemblyName = new AssemblyName("DynamicStructAssembly");
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicStructModule");

            TypeBuilder typeBuilder = moduleBuilder.DefineType(
                typeName,
                TypeAttributes.Public | TypeAttributes.Serializable,
                null
            );

            for (int i = 0; i < types.Length; i++)
            {
                for (int j = 0; j < types[i].GetFields().Length; j++)
                {
                    FieldInfo[] fields = types[i].GetFields();
                    typeBuilder.DefineField(fields[j].Name, fields[j].FieldType, FieldAttributes.Public);
                }
            }

            return typeBuilder.CreateType();
        }

        public static string[] GetFieldNames(Type type)
        {
            FieldInfo[] fields = type.GetFields();
            string[] result = new string[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                result[i] = fields[i].Name;
            }
            return result;
        }

        public static void SetMergedStruct<T>(ref object mergedStruct, Type type, T value)
        {
            string[] fieldNames = GetFieldNames(type);
            // For now considering there is only one field per combined struct
            mergedStruct.GetType().GetField(fieldNames[0]).SetValue(mergedStruct, value);
        }
    }

    public class VOSMonoBehaviour : MonoBehaviour
    {
        public void WaitForEndOfFrame(Action completedMethod)
        {
            StartCoroutine(WaitForEndOfFrameCR(completedMethod));
        }

        public void Transition(float duration, Action<float> onUpdate, Action onComplete)
        {
            StartCoroutine(TransitionCR(duration, onUpdate, onComplete));
        }

        public void Transition(float duration, Func<float, bool> onUpdate, Action onComplete)
        {
            StartCoroutine(TransitionCR(duration, onUpdate, onComplete));
        }

        private IEnumerator WaitForEndOfFrameCR(Action completedMethod)
        {
            yield return new WaitForEndOfFrame();
            completedMethod?.Invoke();
        }

        private IEnumerator TransitionCR(float duration, Action<float> onUpdate, Action onComplete)
        {
            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                onUpdate?.Invoke(t);
                yield return new WaitForEndOfFrame();
            }
            onComplete?.Invoke();
        }

        private IEnumerator TransitionCR(float duration, Func<float, bool> onUpdate, Action onComplete)
        {
            float elapsedTime = 0f;
            bool cancelled = false;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                bool? cancel = onUpdate?.Invoke(t);
                if (cancel.HasValue && !cancel.Value)
                {
                    cancelled = true;
                    break;
                }
                yield return new WaitForEndOfFrame();
            }

            if (!cancelled) onComplete?.Invoke();
        }
    }

    public static class VOSArrayExtensions
    {
        public static T[] Trim<T>(this T[] array)
        {
            if (array == null || array.Length == 0) return array;

            uint newSize = 0;
            ReadOnlySpan<T> spanArray = array.AsSpan();
            for (int i = 0; i < spanArray.Length; i++)
            {
                if (spanArray[i] != null) newSize++;
            }
            T[] trimmedArray = new T[newSize];
            int index = 0;
            for (int i = 0; i < spanArray.Length; i++)
            {
                if (spanArray[i] != null)
                {
                    trimmedArray[index] = spanArray[i];
                    index++;
                }
            }
            return trimmedArray;
        }

        public static T[] Trim<T, M>(this T[] array, M valueToTrim)
        {
            if (array == null || array.Length == 0) return array;

            uint newSize = 0;
            ReadOnlySpan<T> spanArray = array.AsSpan();
            for (int i = 0; i < spanArray.Length; i++)
            {
                if (spanArray[i].Equals(valueToTrim)) newSize++;
            }
            T[] trimmedArray = new T[newSize];
            int index = 0;
            for (int i = 0; i < spanArray.Length; i++)
            {
                if (!spanArray[i].Equals(valueToTrim))
                {
                    trimmedArray[index] = array[i];
                    index++;
                }
            }
            return trimmedArray;
        }

        public static T[] Merge<T>(this T[] array1, T[] array2)
        {
            if (array1 == null || array1.Length == 0) return array2;
            if (array2 == null || array2.Length == 0) return array1;

            uint newSize = (uint)array1.Length;
            ReadOnlySpan<T> spanArray1 = array1.AsSpan();
            ReadOnlySpan<T> spanArray2 = array2.AsSpan();

            for (int i = 0; i < spanArray2.Length; i++)
            {
                if (Array.FindIndex(array1, (x) => array2[i].Equals(x)) == -1) newSize++;
            }

            if (newSize == array1.Length) return array1; // No new elements to add

            T[] mergedArray = new T[newSize];
            array1.CopyTo(mergedArray, 0);
            int index = array1.Length;

            for (int i = 0; i < spanArray2.Length; i++)
            {
                if (Array.FindIndex(array1, (x) => array2[i].Equals(x)) == -1) mergedArray[index++] = array2[i];
            }

            return mergedArray;
        }

        public static T[] CollectSubArray<T, M>(this M[] array, Func<uint, uint> getArraySize, Func<uint, T[]> getElemArray)
        {
            if (array == null || array.Length == 0) return null;
            uint newSize = 0;
            for (uint i = 0; i < array.Length; i++)
            {
                newSize += getArraySize(i);
            }
            T[] result = new T[newSize];
            uint index = 0;
            for (uint i = 0; i < array.Length; i++)
            {
                T[] collectedArray = getElemArray(i);
                collectedArray.CopyTo(result, index);
                index += (uint)collectedArray.Length;
            }
            return result;
        }

        public static T[] ToSet<T>(this T[] array)
        {
            return ToSet(array, (a, b) => {
                return a.Equals(b);
            });
        }

        public static T[] ToSet<T>(this T[] array, Func<T, T, bool> compareMethod)
        {
            if (array == null || array.Length == 0) return null;
            ReadOnlySpan<T> spanArray = array.AsSpan();
            T[] result = new T[10];
            result[0] = spanArray[0];
            int index = 1;
            for (int i = 1; i < spanArray.Length; i++)
            {
                if (index >= result.Length) Array.Resize(ref result, result.Length + 10);
                if (!result.Contains(spanArray[i], compareMethod)) result[index++] = spanArray[i];
            }
            return result.Trim();
        }

        public static string ToValueString<T>(this T[] array, string seperator = ", ")
        {
            if (array == null || array.Length == 0) return string.Empty;
            ReadOnlySpan<T> spanArray = array.AsSpan();
            string result = string.Empty;
            for (int i = 0; i < spanArray.Length; i++)
            {
                if (i > 0) result += seperator;
                if (spanArray[i] == null) result += "null";
                else result += spanArray[i].ToString();
            }
            return result;
        }

        public static bool Contains<T>(this T[] array, T value, Action containsAction = null)
        {
            return Contains(array, value, (a, b) =>
            {
                return a.Equals(b);
            }, containsAction);
        }

        public static bool Contains<T>(this T[] array, T value, Func<T, T, bool> compareMethod = null, Action containsAction = null)
        {
            if (array == null || array.Length == 0) return false;
            ReadOnlySpan<T> spanArray = array.AsSpan();
            for (int i = 0; i < spanArray.Length; i++)
            {
                if (spanArray[i] == null) continue;

                if (compareMethod(spanArray[i], value))
                {
                    if (containsAction != null) containsAction();
                    return true;
                }
            }

            return false;
        }

        public static bool Exists<T>(this T[] array, T value, Action<T> action = null)
        {
            if (array == null || array.Length == 0) return false;
            ReadOnlySpan<T> spanArray = array.AsSpan();
            bool result = false;
            for (int i = 0; i < spanArray.Length; i++)
            {
                if (spanArray[i].Equals(value))
                {
                    if (action != null) action(spanArray[i]);
                    result = true;
                }
            }

            return result;
        }
    }

    public static class VOSMeshExtensions
    {
        /// <summary>
        /// Adds a quad to the mesh with the specified vertices and normal.
        /// </summary>
        /// <param name="m">Reference to the Mesh</param>
        /// <param name="v0">Position of Vertex 1</param>
        /// <param name="v1">Position of Vertex 2</param>
        /// <param name="v2">Position of Vertex 3</param>
        /// <param name="v3">Position of Vertex 4</param>
        /// <param name="normal">Normal of the Quad</param>
        /// <param name="newSubMesh">Should this be a new Sub Mesh</param>
        public static void AddQuad(this Mesh m, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 normal, bool newSubMesh = false)
        {
            if (newSubMesh) m.subMeshCount++;

            Vector3[] newVerts = new Vector3[m.vertices.Length + 4];
            m.vertices.CopyTo(newVerts, 0);
            newVerts[newVerts.Length - 4] = v0;
            newVerts[newVerts.Length - 3] = v1;
            newVerts[newVerts.Length - 2] = v2;
            newVerts[newVerts.Length - 1] = v3;
            m.vertices = newVerts;

            Vector3[] newNormals = new Vector3[m.vertexCount];
            m.normals.CopyTo(newNormals, 0);
            newNormals[newNormals.Length - 4] = normal.normalized;
            newNormals[newNormals.Length - 3] = normal.normalized;
            newNormals[newNormals.Length - 2] = normal.normalized;
            newNormals[newNormals.Length - 1] = normal.normalized;
            m.normals = newNormals;

            int[] newTriangles = new int[m.triangles.Length + 6];
            m.triangles.CopyTo(newTriangles, 0);
            newTriangles[newTriangles.Length - 6] = m.vertexCount - 4;
            newTriangles[newTriangles.Length - 5] = m.vertexCount - 3;
            newTriangles[newTriangles.Length - 4] = m.vertexCount - 2;
            newTriangles[newTriangles.Length - 3] = m.vertexCount - 4;
            newTriangles[newTriangles.Length - 2] = m.vertexCount - 2;
            newTriangles[newTriangles.Length - 1] = m.vertexCount - 1;
            m.SetTriangles(newTriangles, m.subMeshCount - 1);

            Vector2[] newUVs = new Vector2[m.vertexCount];
            m.uv.CopyTo(newUVs, 0);
            newUVs[newUVs.Length - 4] = new Vector2(0, 0);
            newUVs[newUVs.Length - 3] = new Vector2(1, 0);
            newUVs[newUVs.Length - 2] = new Vector2(1, 1);
            newUVs[newUVs.Length - 1] = new Vector2(0, 1);
            m.uv = newUVs;

            m.RecalculateNormals();
        }

        /// <summary>
        /// Extends the mesh by adding a quad between two existing vertices and two new vertices.
        /// </summary>
        /// <param name="m">Reference to the Mesh</param>
        /// <param name="ov0">Existing Vertex 1</param>
        /// <param name="ov1">Existing Vertex 2</param>
        /// <param name="v0">New Vertex 1</param>
        /// <param name="v1">New Vertex 2</param>
        /// <param name="normal">Normal of the new Quad</param>
        public static void ExtendQuad(this Mesh m, Vector3 ov0, Vector3 ov1, Vector3 v0, Vector3 v1, Vector3 normal)
        {
            Vector3[] newVerts = new Vector3[m.vertices.Length + 2];
            m.vertices.CopyTo(newVerts, 0);
            newVerts[newVerts.Length - 2] = v0;
            newVerts[newVerts.Length - 1] = v1;
            m.vertices = newVerts;

            Vector3[] newNormals = new Vector3[m.vertexCount];
            m.normals.CopyTo(newNormals, 0);
            Vector3 eN0 = newNormals[newNormals.Length - 4];
            Vector3 eN1 = newNormals[newNormals.Length - 3];
            newNormals[newNormals.Length - 4] = ((eN0 - normal.normalized) / 2).normalized;
            newNormals[newNormals.Length - 3] = ((eN1 - normal.normalized) / 2).normalized;
            newNormals[newNormals.Length - 2] = normal.normalized;
            newNormals[newNormals.Length - 1] = normal.normalized;
            m.normals = newNormals;

            int[] newTriangles = new int[m.triangles.Length + 6];
            m.triangles.CopyTo(newTriangles, 0);

            int i0 = Array.FindIndex(m.vertices, (x) => x == ov0);
            if (i0 != -1)
            {
                newTriangles[newTriangles.Length - 6] = i0;
            }
            else
            {
                newTriangles[newTriangles.Length - 6] = m.vertexCount - 4;
            }

            int i1 = Array.FindIndex(m.vertices, (x) => x == ov1);
            if (i1 != -1)
            {
                newTriangles[newTriangles.Length - 5] = i1;
                newTriangles[newTriangles.Length - 3] = i1;
            }
            else
            {
                newTriangles[newTriangles.Length - 5] = m.vertexCount - 3;
                newTriangles[newTriangles.Length - 3] = m.vertexCount - 3;
            }

            newTriangles[newTriangles.Length - 4] = m.vertexCount - 1;
            newTriangles[newTriangles.Length - 2] = m.vertexCount - 2;
            newTriangles[newTriangles.Length - 1] = m.vertexCount - 1;
            m.SetTriangles(newTriangles, m.subMeshCount - 1);

            Vector2[] newUVs = new Vector2[m.vertexCount];
            m.uv.CopyTo(newUVs, 0);
            newUVs[newUVs.Length - 2] = new Vector2(0, -1);
            newUVs[newUVs.Length - 1] = new Vector2(1, -1);
            m.uv = newUVs;

            m.RecalculateNormals();
        }
    }

    public static class VOSTransformExtensions
    {
        public static void SetPosition(this Transform t, VOSAxis axis, float value, bool local = false)
        {
            switch (axis)
            {
                case VOSAxis.X:
                    if (local) t.localPosition = new Vector3(value, t.localPosition.y, t.localPosition.z);
                    else t.position = new Vector3(value, t.position.y, t.position.z);
                    break;
                case VOSAxis.Y:
                    if (local) t.localPosition = new Vector3(t.localPosition.x, value, t.localPosition.z);
                    else t.position = new Vector3(t.position.x, value, t.position.z);
                    break;
                case VOSAxis.Z:
                    if (local) t.localPosition = new Vector3(t.localPosition.x, t.localPosition.y, value);
                    else t.position = new Vector3(t.position.x, t.position.y, value);
                    break;
                default:
                    Debug.Log("Impossible Axis Error");
                    break;
            }
        }
    }
}
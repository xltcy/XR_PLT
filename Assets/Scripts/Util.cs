using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public static class Util
{
    public static IEnumerator SetTimeOut(float time, Action callback)
    {
        yield return new WaitForSeconds(time);
        callback();
    }

    public static IEnumerator SetTimeOutAsync(float time, Func<Task> callback)
    {
        yield return new WaitForSeconds(time);
        yield return callback().AsCoroutine();
    }
 
    public static float CosineValue(Vector3 from, Vector3 to) =>
        Mathf.Clamp(Vector3.Dot(from.normalized, to.normalized), -1f, 1f);

    public static bool IsEmpty(this ICollection collection) => collection.Count == 0;

    public static byte[] ToByteArray(this AudioClip clip)
    {
        float[] data = new float[clip.samples];
        clip.GetData(data, 0);
        return data.ToByteArray();
    }

    public static byte[] ToByteArray(this float[] floatArray)
    {
        int len = floatArray.Length * 4;
        byte[] byteArray = new byte[len];
        int pos = 0;
        foreach (float f in floatArray)
        {
            byte[] data = System.BitConverter.GetBytes(f);
            System.Array.Copy(data, 0, byteArray, pos, 4);
            pos += 4;
        }
        return byteArray;
    }

    // Used to convert the byte array to float array for the audio clip
    public static float[] ToFloatArray(this byte[] byteArray)
    {
        int len = byteArray.Length / 4;
        float[] floatArray = new float[len];
        for (int i = 0; i < byteArray.Length; i += 4)
        {
            if (BitConverter.IsLittleEndian) Array.Reverse(floatArray, i * 4, 4);
            floatArray[i / 4] = System.BitConverter.ToSingle(byteArray, i) / 0x80000000;
        }
        return floatArray;
    }

    public static IEnumerator AsCoroutine(this Task task)
    {
        yield return new WaitUntil(() => task.IsCompleted);
        if (task.IsFaulted)
        {
            Debug.LogError($"[AsCoroutine]: Task ended with error: {task.Exception}");
        }
    }

    public static void RunTask(this MonoBehaviour self, Task task)
    {
        self.StartCoroutine(task.AsCoroutine());
    }
}


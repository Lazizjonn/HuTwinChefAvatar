using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;
using System.IO;
using System;

public class BodyBoneLogger : MonoBehaviour
{
    private OVRSkeleton skeleton;
    private string logFilePath;
    private ConcurrentQueue<string> logQueue = new ConcurrentQueue<string>();
    private Thread logThread;
    private bool isRunning = true;

    void Start()
    {
        skeleton = GetComponent<OVRSkeleton>();

        if (skeleton == null)
        {
            Debug.LogError("TTT, OVRSkeleton component not found on this GameObject, Start()");
            enabled = false;
            return;
        }

        // Create a unique file name with a timestamp
        string timeStamp = DateTime.Now.ToString("yyyy_MM_dd_HH-mm-ss");
        string fileName = $"bone_log_{timeStamp}.txt";

        // Set the log file path to persistent data path with the unique file name
        logFilePath = Path.Combine(Application.persistentDataPath, fileName);

        // Ensure the directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));

        File.WriteAllText(logFilePath, "Bone Log\n");
        Debug.Log("TTT, Bone log file created at " + logFilePath + ", Start()");

        // Start the logging thread, my new thread
        logThread = new Thread(LogToFile);
        logThread.Start();
    }

    void Update()
    {
        if (skeleton.IsDataValid && skeleton.Bones != null)
        {
            foreach (var bone in skeleton.Bones)
            {
                if (bone != null && bone.Transform != null)
                {
                    Vector3 position = bone.Transform.position;
                    Quaternion rotation = bone.Transform.rotation;
                    string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: Bone {bone.Id} - Position: (X: {position.x:F3}, Y: {position.y:F3}, Z: {position.z:F3}), Rotation: (X: {rotation.x:F3}, Y: {rotation.y:F3}, Z: {rotation.z:F3}, W: {rotation.w:F3})";

                    logQueue.Enqueue(logEntry);
                    Debug.Log("TTT, " + logEntry + ", Update()");
                }
            }
        }
        else
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: Skeleton data is not valid or bones are missing.";
            logQueue.Enqueue(logEntry);
            Debug.LogWarning("TTT, " + logEntry + ", Update()");
        }
    }

    private void LogToFile()
    {
        while (isRunning || !logQueue.IsEmpty)
        {
            if (logQueue.TryDequeue(out string logEntry))
            {
                File.AppendAllText(logFilePath, logEntry + "\n");
            }
            else
            {
                Thread.Sleep(10); // Prevent busy-waiting
            }
        }
    }

    void OnDestroy()
    {
        isRunning = false;
        logThread?.Join(); // Wait for thread to finish

        string shutdownLog = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: Logging stopped and application shutting down.";
        File.AppendAllText(logFilePath, shutdownLog + "\n");
        Debug.Log("TTT, " + shutdownLog + ", OnDestroy()");
    }
}

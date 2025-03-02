using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;
using System.IO;
using System;

public class BoneLogger : MonoBehaviour
{
    private OVRSkeleton skeleton;
    private string logFilePath;
    private ConcurrentQueue<string> logQueue = new ConcurrentQueue<string>();
    private Thread logThread;
    private bool isRunning = true;
    private string message = null;

    void Start()
    {
        skeleton = GetComponent<OVRSkeleton>();

        if (skeleton == null)
        {
            // Debug.LogError("TTT, OVRSkeleton component not found on this GameObject, Start()");
            enabled = false;
            return;
        }

        // Create a unique file name with a timestamp
        string timeStamp = DateTime.Now.ToString("yyyy_MM_dd_HH-mm-ss");
        string fileName = $"bone_log_{timeStamp}.csv";

        // Set the log file path to persistent data path with the unique file name
        logFilePath = Path.Combine(Application.persistentDataPath, fileName);

        // Ensure the directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));

        File.WriteAllText(logFilePath, "Time,Bone,PositionX,PositionY,PositionZ,RotationX,RotationY,RotationZ,RotationW\n");
        // Debug.Log("TTT, Bone log file created at " + logFilePath + ", Start()");

        // Start the logging thread
        logThread = new Thread(LogToFile);
        logThread.Start();
    }

    void Update()
    {
        if (skeleton.IsDataValid && skeleton.Bones != null && skeleton.Bones.Count > 0)
        {
            foreach (var bone in skeleton.Bones)
            {
                if (bone == null) continue;

                Vector3 position = bone.Transform.position;
                Quaternion rotation = bone.Transform.rotation;

                // Format the log entry
                string logEntry = $"{DateTime.Now:hh:mm:ss.fff tt}, Bone {bone.Id}, " +
                                  $"{position.x:F3}, {position.y:F3}, {position.z:F3}, " +
                                  $"{rotation.x:F3}, {rotation.y:F3}, {rotation.z:F3}, {rotation.w:F3}";

                logQueue.Enqueue(logEntry);
                // Debug.Log("TTT, " + logEntry + ", Update()");
            }
        }
        else
        {
            // Log invalid skeleton data
            string logEntry = $"{DateTime.Now:hh:mm:ss.fff tt}, Skeleton data is not valid or bones are missing.";
            logQueue.Enqueue(logEntry);
            // Debug.LogWarning("TTT, " + logEntry + ", Update()");
        }
    }

    private void LogToFile()
    {
        while (isRunning || !logQueue.IsEmpty)
        {
            if (message != null)
            {
               File.AppendAllText(logFilePath, message);
               message = null;
            }
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

    public void AddTaskLevelMessage(string taskLevel)
    {
       string taskLevelLog = $"########################   New level - {taskLevel}   ########################";
       message = "\n" + taskLevelLog + "\n";
    }

    void OnDestroy()
    {
        isRunning = false;
        logThread?.Join(); // Wait for the thread to finish

        // Log shutdown for debugging purposes
        string shutdownLog = $"{DateTime.Now:hh:mm:ss.fff tt}, Logging stopped and application shutting down.";
        File.AppendAllText(logFilePath, shutdownLog + "\n");
        // Debug.Log("TTT, " + shutdownLog + ", OnDestroy()");
    }
}

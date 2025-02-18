using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using UnityEngine;
using System;

public class ExternalExpressionLogger : MonoBehaviour
{
    private OVRFaceExpressions faceExpressions;
    private string logFilePath;
    private ConcurrentQueue<string> logQueue = new ConcurrentQueue<string>();
    private Thread logThread;
    private bool isRunning = false;

    public void StartLogging()
    {
        faceExpressions = GetComponent<OVRFaceExpressions>();

        if (faceExpressions == null)
        {
            Debug.LogError("TTT, OVRFaceExpressions component not found on this GameObject, StartLogging()");
            return;
        }

        if (isRunning)
        {
            Debug.LogWarning("TTT, ExpressionLogger is already running, StartLogging()");
            return;
        }

        string timeStamp = DateTime.Now.ToString("yyyy_MM_dd_HH-mm-ss");
        string fileName = $"expression_log_{timeStamp}.txt";

#if UNITY_ANDROID && !UNITY_EDITOR
        string externalPath = Path.Combine(Application.persistentDataPath, "../../Documents");
        Directory.CreateDirectory(externalPath);
        logFilePath = Path.Combine(externalPath, fileName);
#else
        logFilePath = Path.Combine(Application.persistentDataPath, fileName);
#endif

        File.WriteAllText(logFilePath, "Expression Log\n");
        Debug.Log("TTT, Expression log file created at " + logFilePath + ", StartLogging()");

        // Start the logging thread
        isRunning = true;
        logThread = new Thread(LogToFile);
        logThread.Start();
    }

    public void StopLogging()
    {
        if (!isRunning)
        {
            Debug.LogWarning("TTT, ExpressionLogger is not running, StopLogging()");
            return;
        }

        isRunning = false;

        
        logThread?.Join();
        logThread = null;

        
        string shutdownLog = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: Logging stopped.";
        File.AppendAllText(logFilePath, shutdownLog + "\n");
        Debug.Log("TTT, " + shutdownLog + ", StopLogging()");
    }

    void Update()
    {
        if (!isRunning) return;

        if (faceExpressions.ValidExpressions)
        {
            for (int i = 0; i < faceExpressions.Count; i++)
            {
                OVRFaceExpressions.FaceExpression expression = (OVRFaceExpressions.FaceExpression)i;
                float weight = faceExpressions[expression];
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: Expression {expression}: {weight}";

                logQueue.Enqueue(logEntry);
                Debug.Log("TTT, " + logEntry + ", Update()");
            }
        }
        else
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: Face expressions are not valid.";
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
        StopLogging();
    }
}

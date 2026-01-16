using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class AsyncProcessor : MonoBehaviour
{
    private static AsyncProcessor instance;
    private Queue<AsyncTask> taskQueue;
    private List<AsyncTask> runningTasks;
    private int maxConcurrentTasks = 4;

    [System.Serializable]
    public class AsyncTask
    {
        public string taskId;
        public Task task;
        public System.Action onComplete;
        public System.Action<AsyncTask> onProgress;
        public float progress;
        public bool isCompleted;
        public System.Exception exception;

        public AsyncTask(string id, Task t, System.Action complete = null, System.Action<AsyncTask> progress = null)
        {
            taskId = id;
            task = t;
            onComplete = complete;
            onProgress = progress;
            progress = 0f;
            isCompleted = false;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        taskQueue = new Queue<AsyncTask>();
        runningTasks = new List<AsyncTask>();
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // Process completed tasks
        for (int i = runningTasks.Count - 1; i >= 0; i--)
        {
            var asyncTask = runningTasks[i];

            if (asyncTask.task.IsCompleted)
            {
                runningTasks.RemoveAt(i);

                if (asyncTask.task.IsFaulted)
                {
                    asyncTask.exception = asyncTask.task.Exception;
                    Debug.LogError($"Async task '{asyncTask.taskId}' failed: {asyncTask.exception.Message}");
                }
                else
                {
                    asyncTask.isCompleted = true;
                    asyncTask.onComplete?.Invoke();
                }
            }
            else
            {
                // Update progress if available
                asyncTask.onProgress?.Invoke(asyncTask);
            }
        }

        // Start new tasks if we have capacity
        while (taskQueue.Count > 0 && runningTasks.Count < maxConcurrentTasks)
        {
            var task = taskQueue.Dequeue();
            runningTasks.Add(task);
        }
    }

    /// <summary>
    /// Adds a task to the processing queue
    /// </summary>
    public static void EnqueueTask(string taskId, Task task, System.Action onComplete = null, System.Action<AsyncTask> onProgress = null)
    {
        if (instance == null)
        {
            Debug.LogError("AsyncProcessor not initialized!");
            return;
        }

        var asyncTask = new AsyncTask(taskId, task, onComplete, onProgress);
        instance.taskQueue.Enqueue(asyncTask);
    }

    /// <summary>
    /// Creates and enqueues a generation task
    /// </summary>
    public static void EnqueueGenerationTask(string taskId, System.Func<Task> generationFunction, System.Action onComplete = null)
    {
        Task task = Task.Run(async () =>
        {
            try
            {
                await generationFunction();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Generation task '{taskId}' failed: {ex.Message}");
                throw;
            }
        });

        EnqueueTask(taskId, task, onComplete);
    }

    /// <summary>
    /// Creates and enqueues a processing task with progress tracking
    /// </summary>
    public static AsyncTask EnqueueProcessingTask(string taskId, System.Func<AsyncTask, Task> processingFunction, System.Action onComplete = null)
    {
        var asyncTask = new AsyncTask(taskId, null, onComplete, null);

        asyncTask.task = Task.Run(async () =>
        {
            try
            {
                await processingFunction(asyncTask);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Processing task '{taskId}' failed: {ex.Message}");
                throw;
            }
        });

        if (instance != null)
        {
            instance.taskQueue.Enqueue(asyncTask);
        }

        return asyncTask;
    }

    /// <summary>
    /// Cancels a specific task
    /// </summary>
    public static bool CancelTask(string taskId)
    {
        if (instance == null)
            return false;

        // Check running tasks
        for (int i = instance.runningTasks.Count - 1; i >= 0; i--)
        {
            if (instance.runningTasks[i].taskId == taskId)
            {
                // Note: Unity tasks don't support cancellation easily
                // In a real implementation, you'd use CancellationToken
                Debug.LogWarning($"Cannot cancel running task '{taskId}'");
                return false;
            }
        }

        // Check queued tasks
        var tempQueue = new Queue<AsyncTask>();
        bool found = false;

        while (instance.taskQueue.Count > 0)
        {
            var task = instance.taskQueue.Dequeue();
            if (task.taskId != taskId)
            {
                tempQueue.Enqueue(task);
            }
            else
            {
                found = true;
            }
        }

        instance.taskQueue = tempQueue;
        return found;
    }

    /// <summary>
    /// Gets the number of queued tasks
    /// </summary>
    public static int GetQueuedTaskCount()
    {
        return instance?.taskQueue.Count ?? 0;
    }

    /// <summary>
    /// Gets the number of running tasks
    /// </summary>
    public static int GetRunningTaskCount()
    {
        return instance?.runningTasks.Count ?? 0;
    }

    /// <summary>
    /// Gets all running tasks
    /// </summary>
    public static List<AsyncTask> GetRunningTasks()
    {
        return new List<AsyncTask>(instance?.runningTasks ?? new List<AsyncTask>());
    }

    /// <summary>
    /// Clears all queued tasks
    /// </summary>
    public static void ClearQueue()
    {
        if (instance != null)
        {
            instance.taskQueue.Clear();
        }
    }

    /// <summary>
    /// Sets the maximum number of concurrent tasks
    /// </summary>
    public static void SetMaxConcurrentTasks(int maxTasks)
    {
        if (instance != null)
        {
            instance.maxConcurrentTasks = Mathf.Max(1, maxTasks);
        }
    }

    /// <summary>
    /// Processes tasks on the main thread (for Unity operations)
    /// </summary>
    public static async Task ProcessOnMainThread(System.Action action)
    {
        // This is a simplified version. In a real implementation,
        // you'd use Unity's main thread dispatcher
        await Task.Yield();

        if (action != null)
        {
            try
            {
                action();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Main thread action failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Creates a task that reports progress
    /// </summary>
    public static AsyncTask CreateProgressTask(string taskId, System.Func<AsyncTask, Task> taskFunction, System.Action onComplete = null)
    {
        var asyncTask = new AsyncTask(taskId, null, onComplete, null);

        asyncTask.task = Task.Run(async () =>
        {
            try
            {
                await taskFunction(asyncTask);
                asyncTask.progress = 1f;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Progress task '{taskId}' failed: {ex.Message}");
                throw;
            }
        });

        return asyncTask;
    }

    /// <summary>
    /// Waits for all tasks to complete
    /// </summary>
    public static async Task WaitForAllTasks()
    {
        if (instance == null)
            return;

        while (instance.taskQueue.Count > 0 || instance.runningTasks.Count > 0)
        {
            await Task.Delay(100); // Wait 100ms before checking again
        }
    }

    /// <summary>
    /// Creates a batch processing task
    /// </summary>
    public static AsyncTask CreateBatchTask(string taskId, List<System.Func<Task>> taskFunctions, System.Action onComplete = null)
    {
        var asyncTask = new AsyncTask(taskId, null, onComplete, null);

        asyncTask.task = Task.Run(async () =>
        {
            try
            {
                int completed = 0;
                foreach (var taskFunction in taskFunctions)
                {
                    await taskFunction();
                    completed++;
                    asyncTask.progress = (float)completed / taskFunctions.Count;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Batch task '{taskId}' failed: {ex.Message}");
                throw;
            }
        });

        return asyncTask;
    }

    /// <summary>
    /// Gets task statistics
    /// </summary>
    public static TaskStatistics GetStatistics()
    {
        if (instance == null)
        {
            return new TaskStatistics();
        }

        return new TaskStatistics
        {
            queuedTasks = instance.taskQueue.Count,
            runningTasks = instance.runningTasks.Count,
            maxConcurrentTasks = instance.maxConcurrentTasks
        };
    }

    [System.Serializable]
    public class TaskStatistics
    {
        public int queuedTasks;
        public int runningTasks;
        public int maxConcurrentTasks;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    /// <summary>
    /// Ensures the AsyncProcessor exists in the scene
    /// </summary>
    public static void EnsureExists()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("AsyncProcessor");
            instance = go.AddComponent<AsyncProcessor>();
        }
    }
}

/// <summary>
/// Extension methods for async processing
/// </summary>
public static class AsyncExtensions
{
    /// <summary>
    /// Enqueues a task with automatic AsyncProcessor creation
    /// </summary>
    public static void Enqueue(this Task task, string taskId, System.Action onComplete = null)
    {
        AsyncProcessor.EnsureExists();
        AsyncProcessor.EnqueueTask(taskId, task, onComplete);
    }

    /// <summary>
    /// Creates a delayed task
    /// </summary>
    public static Task Delay(float seconds)
    {
        return Task.Delay((int)(seconds * 1000));
    }

    /// <summary>
    /// Creates a task that completes when a condition is met
    /// </summary>
    public static async Task WaitUntil(System.Func<bool> condition, float checkInterval = 0.1f)
    {
        while (!condition())
        {
            await Task.Delay((int)(checkInterval * 1000));
        }
    }
}

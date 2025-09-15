namespace System.Threading.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public static class TaskHelper
    {
        /// <remarks>Проглатывает последующие исключения.</remarks>
        [DebuggerStepThrough]
        public static Task WhenAllOrAnyException(params Task[] tasksArray)
        {
            return WhenAllOrAnyException(tasks: tasksArray);
        }

        /// <remarks>Проглатывает последующие исключения.</remarks>
        [DebuggerStepThrough]
        public static async Task WhenAllOrAnyException(IEnumerable<Task> tasks)
        {
            if (tasks == null)
                throw new ArgumentNullException(nameof(tasks));

            var list = tasks.ToList();
            while (list.Count > 0)
            {
                var completedTask = await Task.WhenAny(list).ConfigureAwait(false);
                list.Remove(completedTask);

                if (completedTask.Exception?.InnerException is Exception ex)
                {
                    foreach (var task in list)
                    {
                        // "Просмотрим" любые исключения и проигнорируем их, что-бы предотвратить событие UnobservedTaskException.
                        _ = task.ContinueWith(static t => { _ = t.Exception; },
                            CancellationToken.None,
                            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                            TaskScheduler.Default);
                    }

                    // Остальные таски будут брошены, а исключения проглочены.
                    throw ex;
                }
            }
        }
    }
}

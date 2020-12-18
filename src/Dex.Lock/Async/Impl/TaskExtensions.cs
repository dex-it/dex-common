using System.Threading.Tasks;

namespace Dex.Lock.Async.Impl
{
    internal static class TaskExtensions
    {
        public static bool IsCompletedSuccessfully(this Task task)
        {
#if NETSTANDARD2_0
            return task.Status == TaskStatus.RanToCompletion;
#else
            return task.IsCompletedSuccessfully;
#endif
        }
    }
}

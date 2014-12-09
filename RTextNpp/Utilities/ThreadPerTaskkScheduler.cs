using System.Collections.Generic;
using System.Linq; 
using System.Threading.Tasks;
using System.Threading;

namespace ESRLabs.RTextEditor.Utilities
{
    /**
     * @class   ThreadPerTaskScheduler
     *
     * @brief   Provides a task scheduler that dedicates a thread per task.
     *
     * @author  Stefanos Anastasiou
     * @date    19.12.2012
     */
    public class ThreadPerTaskScheduler : TaskScheduler
    {
        /**
         * @fn  protected override IEnumerable<Task> GetScheduledTasks()
         *
         * @brief   Gets the tasks currently scheduled to this scheduler.
         *
         * @author  Stefanos Anastasiou
         * @date    19.12.2012
         *
         * @return  An enumerator that allows foreach to be used to process get scheduled tasks in this
         *          collection.
         *
         * ### remarks  This will always return an empty enumerable, as tasks are launched as soon as
         *              they're queued.
         */       
        protected override IEnumerable<Task> GetScheduledTasks() { return Enumerable.Empty<Task>(); }

        /**
         * @fn  protected override void QueueTask(Task task)
         *
         * @brief   Starts a new thread to process the provided task.
         *
         * @author  Stefanos Anastasiou
         * @date    19.12.2012
         *
         * @param   task    The task to be executed.
         */     
        protected override void QueueTask(Task task)
        {
            new Thread(() => TryExecuteTask(task)) { Priority = ThreadPriority.Normal, IsBackground = true }.Start();
        }

        /**
         * @fn  protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
         *
         * @brief   Runs the provided task on the current thread.
         *
         * @author  Stefanos Anastasiou
         * @date    19.12.2012
         *
         * @param   task                    The task to be executed.
         * @param   taskWasPreviouslyQueued Ignored.
         *
         * @return  Whether the task could be executed on the current thread.
         */        
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return TryExecuteTask(task);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RTextNppPlugin.Utilities
{
    /**
     * \class   ThreadPerTaskScheduler
     *
     * \brief   Provides a task scheduler that dedicates a thread per task.
     *
     */
    public class ThreadPerTaskScheduler : TaskScheduler
    {
        /**
         *
         * \brief   Gets the tasks currently scheduled to this scheduler.
         *
         *
         * \return  An enumerator that allows foreach to be used to process get scheduled tasks in this
         *          collection.
         *
         * ### remarks  This will always return an empty enumerable, as tasks are launched as soon as
         *              they're queued.
         */       
        protected override IEnumerable<Task> GetScheduledTasks() { return Enumerable.Empty<Task>(); }

        /**
         *
         * \brief   Starts a new thread to process the provided task.
         *
         *
         * \param   task    The task to be executed.
         */     
        protected override void QueueTask(Task task)
        {
            new Thread(() => TryExecuteTask(task)) { Priority = ThreadPriority.Normal, IsBackground = true }.Start();
        }

        /**
         *
         * \brief   Runs the provided task on the current thread.
         *
         *
         * \param   task                    The task to be executed.
         * \param   taskWasPreviouslyQueued Ignored.
         *
         * \return  Whether the task could be executed on the current thread.
         */        
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return TryExecuteTask(task);
        }
    }
}

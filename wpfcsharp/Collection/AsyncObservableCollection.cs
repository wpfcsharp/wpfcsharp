using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace wpfcsharp.Collection
{
    /// <summary>
    /// AsyncObservableCollection class.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class AsyncObservableCollection<T> : ObservableCollection<T>, ICommand
    {
        #region Fields

        private readonly Func<Object, Task<IEnumerable<T>>> factory;
        private EventHandler canExecuteChangedDelegate;
        private Int32 loading;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the AsyncObservableCollection class.
        /// </summary>
        /// <param name="factory"></param>
        public AsyncObservableCollection(Func<Object, Task<IEnumerable<T>>> factory)
        {
            this.factory = factory;
        }

        #endregion

        #region Properties

        public Boolean IsLoading
        {
            get { return loading != 0; }
        }

        public Boolean IsEnabled
        {
            get { return !IsLoading; }
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Update collection items.
        /// </summary>
        /// <param name="parameter"></param>
        public void Update(Object parameter = null)
        {
            var command = (ICommand)this;
            if (command.CanExecute(parameter))
                command.Execute(parameter);
        }

        #endregion

        #region Private Members

        private void InternalExecute(Object parameter)
        {
            if (Application.Current.CheckAccess())
            {
                OnCanExecuteChanged();
                Clear();
                ToAsync(factory(parameter), Callback, parameter);
            }
            else
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.DataBind, new Action<Object>(InternalExecute), parameter);
            }
        }

        private void OnCanExecuteChanged()
        {
            OnPropertyChanged(new PropertyChangedEventArgs("IsLoading"));
            OnPropertyChanged(new PropertyChangedEventArgs("IsEnabled"));

            var handler = canExecuteChangedDelegate;
            if (handler == null) return;
            handler(this, EventArgs.Empty);
        }

        /// <summary>
        /// References a method to be called when a corresponding asynchronous operation completes.
        /// </summary>
        /// <param name="asyncResult">The result of the asynchronous operation.</param>
        private void Callback(IAsyncResult asyncResult)
        {
            var task = asyncResult as Task<IEnumerable<T>>;
            if (task == null) return;
            switch (task.Status)
            {
                case TaskStatus.RanToCompletion:
                    // The task completed execution successfully.
                    Action<T> action = Add;
                    foreach (var entity in task.Result)
                    {
                        var obj = entity;
                        Application.Current.Dispatcher.Invoke(DispatcherPriority.DataBind, action, obj);
                    }
                    break;

                case TaskStatus.Faulted:
                    // The task completed due to an unhandled exception.
                    if (task.Exception != null) task.Exception.Handle(ExceptionHandle);
                    break;
            }
            Thread.VolatileWrite(ref loading, 0);
            Application.Current.Dispatcher.Invoke(DispatcherPriority.DataBind, new Action(OnCanExecuteChanged));
        }

        #endregion

        #region ICommand Members

        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute.
        /// </summary>
        event EventHandler ICommand.CanExecuteChanged
        {
            add { canExecuteChangedDelegate += value; }
            remove { canExecuteChangedDelegate -= value; }
        }

        /// <summary>
        /// Defines the method that determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command. If the command does not require data to be passed, this object can be set to null. </param>
        /// <returns>true if this command can be executed; otherwise, false.</returns>
        bool ICommand.CanExecute(object parameter)
        {
            return IsEnabled;
        }

        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">Data used by the command. If the command does not require data to be passed, this object can be set to null. </param>
        void ICommand.Execute(object parameter)
        {
            if (factory == null) return;
            if (Interlocked.CompareExchange(ref loading, 1, 0) == 0)
            {
                InternalExecute(parameter);
            }
        }

        #endregion

        #region Static Members

        /// <summary> 
        /// Creates a Task that represents the completion of another Task, and  
        /// that schedules an AsyncCallback to run upon completion. 
        /// </summary> 
        /// <param name="task">The antecedent Task.</param> 
        /// <param name="callback">The AsyncCallback to run.</param> 
        /// <param name="state">The object state to use with the AsyncCallback.</param> 
        /// <returns>The new task.</returns> 
        private static Task<TResult> ToAsync<TResult>(Task<TResult> task, AsyncCallback callback, object state)
        {
            if (task == null) throw new ArgumentNullException("task");

            var tcs = new TaskCompletionSource<TResult>(state);
            task.ContinueWith(_ =>
            {
                SetFromTask(tcs, task);
                if (callback != null) callback(tcs.Task);
            });
            return tcs.Task;
        }

        /// <summary>Transfers the result of a Task to the TaskCompletionSource.</summary> 
        /// <typeparam name="TResult">Specifies the type of the result.</typeparam> 
        /// <param name="resultSetter">The TaskCompletionSource.</param> 
        /// <param name="task">The task whose completion results should be transfered.</param> 
        private static void SetFromTask<TResult>(TaskCompletionSource<TResult> resultSetter, Task task)
        {
            switch (task.Status)
            {
                case TaskStatus.RanToCompletion: resultSetter.SetResult(task is Task<TResult> ? ((Task<TResult>)task).Result : default(TResult)); break;
                case TaskStatus.Faulted: resultSetter.SetException(task.Exception.InnerExceptions); break;
                case TaskStatus.Canceled: resultSetter.SetCanceled(); break;
                default: throw new InvalidOperationException("The task was not completed.");
            }
        }

        /// <summary>
        /// The predicate to execute for each exception.
        /// </summary>
        /// <param name="exception">An exception contained by this System.AggregateException was not handled.</param>
        /// <returns>The return value of the method that this delegate encapsulates.</returns>
        private static Boolean ExceptionHandle(Exception exception)
        {
            Debug.WriteLine("ERROR: {0}", exception.Message);
            return true;
        }

        #endregion
    }
}

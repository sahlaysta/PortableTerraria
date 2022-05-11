using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sahlaysta.PortableTerrariaCommon
{
    //abstract class for async install/progress dialogs
    abstract class GuiProgressibleOperation
    {
        //event end eventargs
        public class EndEventArgs : EventArgs
        {
            public EndEventArgs(Exception error, bool canceled)
            {
                _error = error;
                _canceled = canceled;
            }
            public Exception Error { get { return _error; } }
            public bool IsCanceled { get { return _canceled; } }
            readonly Exception _error;
            readonly bool _canceled;
        }

        //event progress eventargs
        public class ProgressEventArgs : EventArgs
        {
            public ProgressEventArgs(int numerator, int denominator)
            {
                _numerator = numerator;
                _denominator = denominator;
            }
            public int Numerator { get { return _numerator; } }
            public int Denominator { get { return _denominator; } }
            readonly int _numerator;
            readonly int _denominator;
        }

        //public operations
        public bool Ended { get => ended; }
        public bool Canceled { get => ended && canceled; }
        public event EventHandler<EndEventArgs> End;
        public event EventHandler<ProgressEventArgs> Progress;
        public void Cancel()
        {
            canceled = true;
        }
        public void RunAsync()
        {
            if (ended)
                return;
            Exception error = null;
            Task.Run(() =>
            {
                try
                {
                    run();
                }
                catch (Exception e)
                {
                    error = e;
                }
                ended = true;
                End?.Invoke(this, new EndEventArgs(error, canceled));
            });
        }

        //protected operations
        protected abstract void run();
        protected bool cancelRequested()
        {
            return canceled;
        }
        protected void progress(int numerator, int denominator)
        {
            Progress?.Invoke(this, new ProgressEventArgs(numerator, denominator));
        }

        volatile bool ended, canceled;
    }
}

using DSG.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG.Base
{
    public abstract class DisposableBase : IDisposable
    {
        static readonly string className = nameof(DisposableBase);

        private bool disposedValue;

        public event EventHandler? OnDisposing;
        public event EventHandler? OnDisposed;

        protected virtual void Dispose(bool disposing)
        {
            string method = nameof(Dispose);
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        OnDisposing?.Invoke(this, EventArgs.Empty);
                        OnDisposed?.Invoke(this, EventArgs.Empty);  
                    }
                    catch (Exception ex)
                    {
                    }

                }
                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~DisposableBase()
        {
             // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
             Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

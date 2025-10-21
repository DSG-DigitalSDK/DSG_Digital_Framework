using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG.Base
{
    public interface ICreatable : IDisposable
    {
        /// <summary>
        /// Alloc resources <br/>
        /// Method defines a workflow. use <see cref="OnDestroyImplementation"> to implement specific object instantiation</see>
        /// </summary
        /// <returns>operation result</returns>        
        Task<Result> CreateAsync();

        /// <summary>
        /// Free resources <br/>
        /// Method defines a workflow. use <see cref="OnDestroyImplementation"> to implement specific object instantiation</see>
        /// </summary
        /// <returns>operation result</returns>
        Task<Result> DestroyAsync();

        bool Enabled { get; set; }
        bool Initialized { get; }
        bool ThrowExceptions { get; set; }

        string Name { get; }

        event EventHandler? Creating;
        event EventHandler<ResultEventArgs>? Created;
        event EventHandler<ResultEventArgs>? CreateError;
        event EventHandler? Destroying;
        event EventHandler<ResultEventArgs>? Destroyed;
        event EventHandler<ResultEventArgs>? DestroyError;
    }
}

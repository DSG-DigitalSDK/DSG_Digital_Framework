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
        /// Alloc resources <br/>
        /// Method defines a workflow. use <see cref="OnDestroyImplementation"> to implement specific object instantiation</see>
        /// </summary
        /// <returns>operation result</returns>        
        Result Create();

        /// <summary>
        /// Free resources <br/>
        /// Method defines a workflow. use <see cref="OnDestroyImplementation"> to implement specific object instantiation</see>
        /// </summary
        /// <returns>operation result</returns>        
        Result Destroy();

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

        event EventHandler? OnCreating;
        event EventHandler<ResultEventArgs>? OnCreate;
        event EventHandler<ResultEventArgs>? OnCreateError;
        event EventHandler? OnDestroying;
        event EventHandler<ResultEventArgs>? OnDestroy;
        event EventHandler<ResultEventArgs>? OnDestroyError;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG.Base
{
    public interface ICreatable
    {
        Dictionary<string, object> ParameterCollection { get; }
        Task<Result> CreateAsync();
        Result Create();
        Task<Result> DestroyAsync();
        Result Destroy();

        bool Initialized { get; }
        string Name { get; }

        event EventHandler? OnCreating;
        event EventHandler? OnCreate;
        event EventHandler<ResultEventArgs>? OnCreateError;
        event EventHandler? OnDestroying;
        event EventHandler? OnDestroy;
        event EventHandler<ResultEventArgs>? OnDestroyError;
    }
}

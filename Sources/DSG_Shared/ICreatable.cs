using DSG.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG
{
    public interface ICreatable
    {
        Dictionary<string, object> CreationParameters { get; }
        Task<Result> CreateAsync();
        Result Create();
        Task<Result> DestroyAsync();
        Result Destroy();

        bool Initialized { get; }
        string Name { get; }

        event EventHandler OnCreating;
        event EventHandler OnCreate;
        event EventHandler OnCreateError;
        event EventHandler OnDestroying;
        event EventHandler OnDestroy;
        event EventHandler OnDestroyError;
    }
}

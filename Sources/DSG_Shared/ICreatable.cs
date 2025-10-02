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
        Dictionary<string, object> CreationParameters { get; set; }
        Task<OperationResult> CreateAsync();
        OperationResult Create();
        Task<OperationResult> DestroyAsync();
        OperationResult Destroy();

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

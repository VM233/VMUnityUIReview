using System.Collections.Generic;

namespace VMUnityAutomation.Runtime
{
    public interface IVmUiWorkContextSource
    {
        IEnumerable<VmUiWorkContextBinding> GetWorkContextBindings();
    }
}

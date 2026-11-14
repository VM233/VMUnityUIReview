using System.Collections.Generic;

namespace VMUnityAutomation.Runtime
{
    public interface IVmUiWorkContextSource
    {
        IEnumerable<VmUiWorkContextBinding> GetWorkContextBindings();
    }

    public readonly struct VmUiWorkContextBinding
    {
        public string ElementName { get; }
        public string Context { get; }

        public VmUiWorkContextBinding(string elementName, string context)
        {
            ElementName = elementName;
            Context = context;
        }
    }
}

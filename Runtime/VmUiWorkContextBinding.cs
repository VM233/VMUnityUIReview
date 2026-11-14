namespace VMUnityAutomation.Runtime
{
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

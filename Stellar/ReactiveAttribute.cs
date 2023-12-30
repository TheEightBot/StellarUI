using System.ComponentModel;

namespace Stellar.DoNotUse;

[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ReactiveAttribute : Attribute
{
}

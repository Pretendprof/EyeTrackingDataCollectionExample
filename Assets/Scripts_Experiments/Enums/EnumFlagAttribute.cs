using UnityEngine;

/// <summary>
/// https://gist.github.com/FFouetil/dd081256da0e3475d524d88b414076e3
/// </summary>
public class EnumFlagAttribute : PropertyAttribute
{
    public string enumName;

    public EnumFlagAttribute() { }

    public EnumFlagAttribute(string name)
    {
        enumName = name;
    }
}
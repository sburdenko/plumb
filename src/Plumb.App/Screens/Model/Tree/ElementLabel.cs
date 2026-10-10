
namespace Plumb.App.Screens.Model.Tree;

public static class ElementLabel
{
    /// <summary>
    /// Drops the Revit family prefix: "Basic Wall:Exterior - Brick" becomes "Exterior - Brick".
    /// Names without a prefix are returned unchanged.
    /// </summary>
    public static string WithoutFamily(string name)
    {
        var separator = name.IndexOf(':');
        if (separator <= 0 || separator == name.Length - 1)
        {
            return name;
        }

        return name[(separator + 1)..].TrimStart();
    }
}

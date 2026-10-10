namespace Plumb.Core.Model
{
    /// <summary>Where a property value comes from.</summary>
    public enum PropertySource
    {
        /// <summary>Set on the element itself.</summary>
        Instance,

        /// <summary>Inherited from the element's type, such as a wall type shared by many walls.</summary>
        Type,
    }
}

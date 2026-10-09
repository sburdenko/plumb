namespace Plumb.Viewer
{
    /// <summary>One entry of the package's <c>elements.json</c>.</summary>
    public sealed class ElementInfo
    {
        public ElementInfo(string id, string type, string name, string storey)
        {
            Id = id;
            Type = type;
            Name = name;
            Storey = storey;
        }

        public string Id { get; }

        public string Type { get; }

        public string Name { get; }

        public string Storey { get; }
    }
}

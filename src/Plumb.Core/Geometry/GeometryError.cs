namespace Plumb.Core.Geometry
{
    public enum GeometryError
    {
        /// <summary>The IfcConvert program is not where Plumb expects it.</summary>
        ConverterMissing,

        /// <summary>IfcConvert ran but reported an error or produced no file.</summary>
        ConverterFailed,

        /// <summary>IfcConvert did not finish within the time limit and was stopped.</summary>
        Timeout,

        /// <summary>The package has no geometry file, for example one saved before geometry existed.</summary>
        FileMissing,
    }
}

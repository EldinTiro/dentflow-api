namespace DentFlow.Domain.Clinical;

/// <summary>
/// FDI (ISO 3950) tooth numbers, the system dentists in Bosnia and the rest of Europe use:
/// quadrant 1–4 (upper right, upper left, lower left, lower right), then tooth 1–8 from the midline.
/// </summary>
public static class Fdi
{
    /// <summary>A permanent tooth: 11–18, 21–28, 31–38, 41–48.</summary>
    public static bool IsPermanentTooth(int tooth) =>
        tooth / 10 is >= 1 and <= 4 && tooth % 10 is >= 1 and <= 8;

    /// <summary>
    /// The FDI number of a US "Universal" number (1–32: upper right third molar round to the lower
    /// right third molar). Treatment items were stored that way before the tooth chart.
    /// </summary>
    public static int FromUniversal(int universal) => universal switch
    {
        >= 1 and <= 8 => 19 - universal,
        >= 9 and <= 16 => 12 + universal,
        >= 17 and <= 24 => 55 - universal,
        >= 25 and <= 32 => 16 + universal,
        _ => throw new ArgumentOutOfRangeException(nameof(universal), universal, "Universal tooth numbers run from 1 to 32."),
    };
}

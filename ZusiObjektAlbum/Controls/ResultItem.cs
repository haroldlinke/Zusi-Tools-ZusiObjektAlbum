namespace ZusiObjektAlbum.Controls;

/// <summary>
/// Ein einzelnes Suchergebnis für die Anzeige in der WPF-Ergebnisliste.
/// </summary>
public sealed class ResultItem
{
    public required string ObjectId { get; init; }
    public required float Score { get; init; }
    public required string BestView { get; init; }

    /// <summary>
    /// Echter Pfad zur ursprünglichen .ls3-Datei im Zusi-Datenverzeichnis -
    /// zum Öffnen des Objekts im 3D-Editor/Album.
    /// </summary>
    public required string SourcePath { get; init; }

    /// <summary>
    /// Absoluter Pfad zum Vorschaubild (beste passende Ansicht), oder null,
    /// falls kein Bild gefunden wurde.
    /// </summary>
    public string? ThumbnailPath { get; init; }

    public string ScoreDisplay => $"Score: {Score:F3}";
    public string ViewDisplay => $"Beste Ansicht: {BestView}";
}

namespace ZusiObjektAlbum.Core;

/// <summary>
/// Repräsentiert das Embedding einer einzelnen Seitenansicht (front/back/left/right)
/// eines Zusi-3D-Objekts.
///
/// SourcePath ist der echte Pfad zur ursprünglichen .ls3-Datei im Zusi-
/// Datenverzeichnis - wird bei der Suche mit ausgegeben, damit man das
/// gefundene Objekt direkt im 3D-Editor/Album öffnen kann.
/// </summary>
public sealed record ViewEmbedding(string ObjectId, string ViewName, float[] Vector, string SourcePath);

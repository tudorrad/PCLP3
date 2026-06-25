using System.IO.Compression;
using System.Text.Json;
using StoryEngine.Model;

namespace StoryEngine.Persistence;

/// <summary>
/// Incarca si salveaza fisiere .story (ZIP cu definition.json + imagini).
/// </summary>
public static class StoryFile
{
    private const string DefinitionEntry = "definition.json";
    private const string ImagesFolder    = "images/";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented        = true,
        PropertyNamingPolicy = null
    };

    // ──────────────────────────────────────────────────────
    // LOAD
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// Incarca o poveste dintr-un fisier .story.
    /// Returneaza (definition, imageDictionary[fileName -> bytes]).
    /// </summary>
    public static (StoryDefinition Story, Dictionary<string, byte[]> Images) Load(string path)
    {
        using var archive = ZipFile.OpenRead(path);

        // Citim definition.json
        var defEntry = archive.GetEntry(DefinitionEntry)
            ?? throw new FileNotFoundException($"'{DefinitionEntry}' not found in story file.");

        StoryDefinition story;
        using (var stream = defEntry.Open())
        {
            story = JsonSerializer.Deserialize<StoryDefinition>(stream, JsonOptions)
                    ?? throw new InvalidDataException("Failed to deserialize story definition.");
        }

        // Citim imaginile
        var images = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in archive.Entries)
        {
            if (entry.FullName.StartsWith(ImagesFolder, StringComparison.OrdinalIgnoreCase)
                && !entry.FullName.EndsWith("/"))
            {
                using var ms = new MemoryStream();
                using var s  = entry.Open();
                s.CopyTo(ms);
                string name = entry.FullName[ImagesFolder.Length..]; // strip prefix
                images[name] = ms.ToArray();
            }
        }

        return (story, images);
    }

    // ──────────────────────────────────────────────────────
    // SAVE
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// Salveaza o poveste intr-un fisier .story.
    /// imagePaths: dictionar fileName (ex: "forest.jpg") -> cale absoluta pe disc.
    /// </summary>
    public static void Save(string path, StoryDefinition story,
        Dictionary<string, string>? imagePaths = null)
    {
        // Scriem intr-un temp file ca sa nu corupem originalul daca ceva pica
        string tmp = path + ".tmp";

        using (var archive = ZipFile.Open(tmp, ZipArchiveMode.Create))
        {
            // definition.json
            var defEntry = archive.CreateEntry(DefinitionEntry, CompressionLevel.Optimal);
            using (var stream = defEntry.Open())
            {
                JsonSerializer.Serialize(stream, story, JsonOptions);
            }

            // imagini
            if (imagePaths != null)
            {
                foreach (var (name, srcPath) in imagePaths)
                {
                    if (!File.Exists(srcPath)) continue;
                    archive.CreateEntryFromFile(srcPath,
                        ImagesFolder + name,
                        CompressionLevel.Optimal);
                }
            }
        }

        // Inlocuieste fisierul vechi
        if (File.Exists(path)) File.Delete(path);
        File.Move(tmp, path);
    }

    /// <summary>
    /// Salveaza o poveste impreuna cu imaginile deja in memorie.
    /// </summary>
    public static void SaveWithBytes(string path, StoryDefinition story,
        Dictionary<string, byte[]>? imageBytes = null)
    {
        string tmp = path + ".tmp";

        using (var archive = ZipFile.Open(tmp, ZipArchiveMode.Create))
        {
            var defEntry = archive.CreateEntry(DefinitionEntry, CompressionLevel.Optimal);
            using (var stream = defEntry.Open())
                JsonSerializer.Serialize(stream, story, JsonOptions);

            if (imageBytes != null)
            {
                foreach (var (name, bytes) in imageBytes)
                {
                    var imgEntry = archive.CreateEntry(ImagesFolder + name,
                        CompressionLevel.Optimal);
                    using var s = imgEntry.Open();
                    s.Write(bytes, 0, bytes.Length);
                }
            }
        }

        if (File.Exists(path)) File.Delete(path);
        File.Move(tmp, path);
    }
}

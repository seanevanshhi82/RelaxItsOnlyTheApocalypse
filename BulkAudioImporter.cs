using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class AudioClipImporter : EditorWindow
{
    private string sourceFolder = "C:/Sounds/Zombies";
    private string targetFolder = "Assets/Audio/Zombies";
    private string prefix = "Zombie_";
    private bool make3D = true;
    private bool loopByDefault = false;
    private string statusMessage = "";
    private Vector2 scrollPos;

    [MenuItem("Tools/Bulk Audio Importer")]
    public static void ShowWindow()
    {
        GetWindow<AudioClipImporter>("Bulk Audio Importer");
    }

    void OnGUI()
    {
        GUILayout.Label("Bulk Audio Importer", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        sourceFolder = EditorGUILayout.TextField("Source Folder (on disk)", sourceFolder);
        targetFolder = EditorGUILayout.TextField("Target Folder (in project)", targetFolder);

        prefix = EditorGUILayout.TextField("Filename Prefix (optional)", prefix);

        make3D = EditorGUILayout.Toggle("Make 3D Spatial Sound", make3D);
        loopByDefault = EditorGUILayout.Toggle("Loop by Default", loopByDefault);

        EditorGUILayout.Space(20);

        if (GUILayout.Button("IMPORT ALL AUDIO FILES", GUILayout.Height(50)))
        {
            ImportAudioFiles();
        }

        EditorGUILayout.Space(10);

        if (!string.IsNullOrEmpty(statusMessage))
        {
            EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
        }

        EditorGUILayout.HelpBox("1. Point to a folder on your computer with .wav/.mp3/.ogg files\n2. Choose project folder\n3. Set prefix (e.g. 'Zombie_')\n4. Click IMPORT\n\nFiles will be renamed, imported, and configured automatically.", MessageType.Info);
    }

    private void ImportAudioFiles()
    {
        if (!Directory.Exists(sourceFolder))
        {
            statusMessage = $"Source folder not found: {sourceFolder}";
            return;
        }

        if (!AssetDatabase.IsValidFolder(targetFolder))
        {
            AssetDatabase.CreateFolder(Path.GetDirectoryName(targetFolder), Path.GetFileName(targetFolder));
        }

        string[] audioFiles = Directory.GetFiles(sourceFolder, "*.*", SearchOption.TopDirectoryOnly)
            .Where(f => f.EndsWith(".wav", System.StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".mp3", System.StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".ogg", System.StringComparison.OrdinalIgnoreCase))
            .ToArray();

        int importedCount = 0;

        foreach (string filePath in audioFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);

            // Clean and prefix name
            string cleanName = fileName.Replace(" ", "_").Replace("-", "_");
            string newName = string.IsNullOrEmpty(prefix) ? cleanName : prefix + cleanName;

            string targetPath = Path.Combine(targetFolder, newName + extension);

            // Copy file (overwrite if exists)
            File.Copy(filePath, targetPath, true);

            // Import as asset
            AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceUpdate);

            // Configure modern import settings
            AudioImporter importer = AssetImporter.GetAtPath(targetPath) as AudioImporter;
            if (importer != null)
            {
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;

                // Load type (replaces old loadType)
                settings.loadType = make3D ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;

                // Compression (replaces old compressionFormat)
                settings.compressionFormat = AudioCompressionFormat.Vorbis;

                // 3D / Spatial
                importer.threeD = make3D;

                // Loop
                importer.loopable = loopByDefault;

                // Quality & other common settings
                settings.quality = 0.5f; // Balanced quality (0-1)
                settings.sampleRateOverride = 0; // Auto

                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();

                importedCount++;
                Debug.Log($"Imported & configured: {targetPath}");
            }
            else
            {
                Debug.LogWarning($"Failed to configure importer for: {targetPath}");
            }
        }

        AssetDatabase.Refresh();
        statusMessage = $"Imported and configured {importedCount} audio files to {targetFolder}!";
        Repaint();
    }
}
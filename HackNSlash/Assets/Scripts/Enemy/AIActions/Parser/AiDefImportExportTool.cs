#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Extensions.UtilityAI;
using Extensions.UtilityAI.Parser;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

/** <summary>
 * MonoBehaviour import/export tool for the AI definition language.
 *
 * Attach to any GameObject in the scene (or create a dedicated "AITools" GameObject),
 * assign the fields in the Inspector, then use the Odin buttons to import or export.
 *
 * Workflow — Import (text → ScriptableObjects):
 * <list type="number">
 *   <item>Create a .aidef text file (see README for grammar).</item>
 *   <item>Drag it into the <c>Source File</c> field.</item>
 *   <item>Set <c>Output Directory</c> to the folder where assets should be created.</item>
 *   <item>Press <c>Import Actions</c>.</item>
 *   <item>Drag the generated assets into your EnemyStateMachine action list.</item>
 * </list>
 *
 * Workflow — Export (ScriptableObjects → text):
 * <list type="number">
 *   <item>Populate the <c>Actions To Export</c> list.</item>
 *   <item>Set <c>Export Path</c> to the desired output file path.</item>
 *   <item>Press <c>Export Actions</c>.</item>
 * </list>
 * </summary>
 */
[AddComponentMenu("UtilityAI/AI Def Import Export Tool")]
public sealed class AiDefImportExportTool : MonoBehaviour
{
    #region Inspector — Import

    [Title("Import — Text File → ScriptableObject Assets")]
    [Required, Tooltip("The .aidef (or .txt) file to parse.")]
    [SerializeField] private TextAsset sourceFile;

    [FolderPath(RequireExistingPath = false)]
    [Tooltip("Project-relative path (e.g. Assets/Enemy/AIActions) where assets will be created.")]
    [SerializeField] private string outputDirectory = "Assets/Scripts/Enemy/AIActions/Generated";

    [ReadOnly]
    [SerializeField] private string lastImportResult;

    #endregion

    #region Inspector — Export

    [Title("Export — ScriptableObject Assets → Text File")]
    [ListDrawerSettings(ShowFoldout = true)]
    [SerializeField] private List<AIActionBase> actionsToExport = new();

    [Sirenix.OdinInspector.FilePath(Extensions = "aidef,txt", RequireExistingPath = false)]
    [Tooltip("Project-relative path for the output text file (e.g. Assets/AIData/enemies.aidef).")]
    [SerializeField] private string exportPath = "Assets/Scripts/Enemy/AIActions/Generated/exported.aidef";

    [ReadOnly]
    [SerializeField] private string lastExportResult;

    #endregion

    #region Odin Buttons

    [Button(ButtonSizes.Large, Name = "Import Actions"), GUIColor(0.4f, 0.8f, 0.4f)]
    private void ImportActions()
    {
        if (sourceFile == null) { Debug.LogError("[AIDefTool] No source file assigned."); return; }
        if (string.IsNullOrWhiteSpace(outputDirectory)) { Debug.LogError("[AIDefTool] No output directory set."); return; }

        try
        {
            var gen = EnemyAiCodeGeneratorFactory.Create();
            List<GeneratedAction> results = gen.Import(sourceFile.text, outputDirectory);
            lastImportResult = $"✓ Imported {results.Count} action(s) → {outputDirectory}";
            Debug.Log($"[AIDefTool] {lastImportResult}");
            foreach (var r in results)
                Debug.Log($"  • {r.Name} ({r.Asset.GetType().Name})");
        }
        catch (ParseException pe)
        {
            lastImportResult = $"✗ Parse error: {pe.Message}";
            Debug.LogError($"[AIDefTool] {lastImportResult}");
        }
        catch (CodeGenException ce)
        {
            lastImportResult = $"✗ Code-gen error: {ce.Message}";
            Debug.LogError($"[AIDefTool] {lastImportResult}");
        }
        catch (Exception e)
        {
            lastImportResult = $"✗ {e.GetType().Name}: {e.Message}";
            Debug.LogError($"[AIDefTool] {lastImportResult}\n{e.StackTrace}");
        }
    }

    [Button(ButtonSizes.Large, Name = "Export Actions"), GUIColor(0.4f, 0.6f, 1f)]
    private void ExportActions()
    {
        if (actionsToExport == null || actionsToExport.Count == 0)
        { Debug.LogWarning("[AIDefTool] No actions to export."); return; }
        if (string.IsNullOrWhiteSpace(exportPath))
        { Debug.LogError("[AIDefTool] No export path set."); return; }

        try
        {
            var gen = EnemyAiCodeGeneratorFactory.Create();
            string text = gen.Export(actionsToExport);

            string fullPath = Path.Combine(Application.dataPath, "..", exportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, text);

            AssetDatabase.Refresh();
            lastExportResult = $"✓ Exported {actionsToExport.Count} action(s) → {exportPath}";
            Debug.Log($"[AIDefTool] {lastExportResult}");
        }
        catch (Exception e)
        {
            lastExportResult = $"✗ {e.GetType().Name}: {e.Message}";
            Debug.LogError($"[AIDefTool] {lastExportResult}");
        }
    }

    [Button("Validate Source File Only"), GUIColor(0.9f, 0.9f, 0.4f)]
    private void ValidateSource()
    {
        if (sourceFile == null) { Debug.LogError("[AIDefTool] No source file assigned."); return; }
        try
        {
            var tokens = new Lexer(sourceFile.text).Tokenize();
            var doc    = new AiDefParser(tokens).ParseDocument();
            Debug.Log($"[AIDefTool] ✓ Validation passed — {doc.Actions.Count} action(s) found.");
        }
        catch (ParseException pe)
        {
            Debug.LogError($"[AIDefTool] ✗ Parse error: {pe.Message}");
        }
    }

    #endregion
}
#endif



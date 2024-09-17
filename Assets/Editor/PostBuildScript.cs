using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using UnityEngine;

public class PostBuildScript : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        // テンプレートファイルのパス
        string templatePath = Path.Combine(Application.dataPath, "Resources", "config_template.ini");

        // 出力先のパス
        string outputPath = Path.Combine(Path.GetDirectoryName(report.summary.outputPath), "config.ini");

        // テンプレートファイルをコピー
        File.Copy(templatePath, outputPath, true);

        Debug.Log($"config.ini has been copied to: {outputPath}");
    }
}
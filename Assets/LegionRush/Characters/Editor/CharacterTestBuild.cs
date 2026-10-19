using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using LegionRush.EditorTools;

namespace LegionRush.Characters.EditorTools
{
    // Build WebGL de desenvolvimento para testar personagens e tipos de inimigo no navegador, sem tocar no build
    // principal (Builds/WebGL/LegionRush3D). Abrir com "?lrlevel=6&lrspeed=2" para entrar direto na fase.
    // Batch: -executeMethod LegionRush.Characters.EditorTools.CharacterTestBuild.BuildBatch
    public static class CharacterTestBuild
    {
        private const string Output = "Builds/WebGL/CharTest";

        [MenuItem("Legion Rush/Personagens/Build WebGL de teste")]
        public static void BuildMenu() => Build();

        public static void BuildBatch() => EditorApplication.Exit(Build() ? 0 : 1);

        private static bool Build()
        {
            try
            {
                Directory.CreateDirectory(Output);
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
                PlayerSettings.WebGL.decompressionFallback = false;
                PlayerSettings.WebGL.template = "PROJECT:LegionRush";
                PlayerSettings.runInBackground = true;
                var options = new BuildPlayerOptions
                {
                    scenes = ProjectSetup.BuildScenePaths(),
                    locationPathName = Output,
                    target = BuildTarget.WebGL,
                    targetGroup = BuildTargetGroup.WebGL,
                    options = BuildOptions.Development
                };
                BuildSummary summary = BuildPipeline.BuildPlayer(options).summary;
                Debug.Log($"[LegionRush] WebGL de teste: {summary.result}, {summary.totalErrors} erros, " +
                          $"{summary.totalSize / (1024f * 1024f):0.0} MB em {Output}.");
                return summary.result == BuildResult.Succeeded;
            }
            catch (Exception exception)
            {
                Debug.LogError("[LegionRush] WebGL de teste falhou: " + exception);
                return false;
            }
        }
    }
}

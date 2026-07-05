using Subspace;
using UnityEditor;
using UnityEngine;

namespace Subspace.Editor
{
    [CustomEditor(typeof(SubspaceGameDirector))]
    public sealed class SubspaceGameDirectorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            if (EditorApplication.isPlaying)
            {
                if (GUILayout.Button("\u8c03\u8bd5\uff1a\u7acb\u5373\u589e\u52a0 99999 \u5206"))
                {
                    foreach (var targetObject in targets)
                    {
                        if (targetObject is SubspaceGameDirector director)
                        {
                            director.AddScoreForDebug();
                            EditorUtility.SetDirty(director);
                        }
                    }
                }

                if (GUILayout.Button("\u8c03\u8bd5\uff1a\u7acb\u5373\u6263\u9664 99999 \u5206"))
                {
                    foreach (var targetObject in targets)
                    {
                        if (targetObject is SubspaceGameDirector director)
                        {
                            director.SubtractScoreForDebug();
                            EditorUtility.SetDirty(director);
                        }
                    }
                }

                if (GUILayout.Button("\u8c03\u8bd5\uff1a\u76f4\u63a5\u5931\u8d25"))
                {
                    foreach (var targetObject in targets)
                    {
                        if (targetObject is SubspaceGameDirector director)
                        {
                            director.FailImmediatelyForDebug();
                            EditorUtility.SetDirty(director);
                        }
                    }
                }
            }
            else if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("\u8fdb\u5165 Play \u6a21\u5f0f\u540e\u53ef\u70b9\u51fb\u8c03\u8bd5\u6309\u94ae\uff1a\u589e\u52a0/\u6263\u9664 99999 \u5206\uff0c\u6216\u76f4\u63a5\u5931\u8d25\u3002", MessageType.Info);
            }
        }
    }
}

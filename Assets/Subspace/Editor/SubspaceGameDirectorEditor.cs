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
            if (EditorApplication.isPlaying && GUILayout.Button("下次攻击强制失败（扣除 9999 分）"))
            {
                foreach (var targetObject in targets)
                {
                    if (targetObject is SubspaceGameDirector director)
                    {
                        director.ForceFailureForTest();
                        EditorUtility.SetDirty(director);
                    }
                }
            }
            else if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 Play 模式后可点击测试按钮：下次攻击强制失败（扣除 9999 分）。", MessageType.Info);
            }
        }
    }
}

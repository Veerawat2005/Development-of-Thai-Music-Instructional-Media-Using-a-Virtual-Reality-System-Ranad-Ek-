using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// ผูกไฟล์เสียงเข้ากับลูกระนาดทุกลูกใน Scene ที่เปิดอยู่ โดยไม่ต้องสร้าง Scene ใหม่
/// (งาน UI/การจัดวางที่ทำไว้แล้วไม่หาย)
///
/// หาไฟล์เสียงจาก "เลขลูก" ในชื่อไฟล์ ไม่สนว่าต่อท้ายด้วยความถี่หรืออะไร เช่น
///   ranad_bar01_175Hz  -> RanadKey_01
///   ranad_bar02_194Hz  -> RanadKey_02
/// รองรับชื่อแบบเก่าด้วย: Ranad_01 -> RanadKey_01
///
/// **ไฟล์นี้ต้องอยู่ในโฟลเดอร์ชื่อ "Editor" เท่านั้น** เช่น Assets/Editor/
/// วิธีใช้: เมนู Tools > VR Ranad Ek > Assign Note Sounds To Keys แล้วกด Ctrl+S เพื่อเซฟ Scene
/// </summary>
public static class RanadNoteSoundAssigner
{
    /// <summary>ค้นหา AudioClip ของลูกระนาดลูกที่ index (1-21) ไม่พบคืนค่า null</summary>
    public static AudioClip FindClip(int index)
    {
        string[] prefixes =
        {
            $"ranad_bar{index:00}",  // ranad_bar01_175Hz
            $"ranad_{index:00}"      // Ranad_01
        };

        foreach (string prefix in prefixes)
        {
            string[] guids = AssetDatabase.FindAssets($"{prefix} t:AudioClip", new[] { "Assets" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();

                // ต้องเป็นชื่อเดียวกัน หรือมี "_" ต่อท้าย (กัน bar01 ไปจับ bar010)
                if (fileName == prefix || fileName.StartsWith(prefix + "_"))
                {
                    return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                }
            }
        }
        return null;
    }

    [MenuItem("Tools/VR Ranad Ek/Assign Note Sounds To Keys")]
    public static void AssignToOpenScene()
    {
        RanadKey[] keys = Object.FindObjectsByType<RanadKey>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (keys.Length == 0)
        {
            Debug.LogWarning("[NoteSoundAssigner] ไม่พบลูกระนาด (RanadKey) ใน Scene ที่เปิดอยู่เลย");
            return;
        }

        int assigned = 0;
        List<string> missing = new List<string>();

        foreach (RanadKey key in keys)
        {
            string[] parts = key.gameObject.name.Split('_');
            if (parts.Length < 2 || !int.TryParse(parts[parts.Length - 1], out int index))
            {
                missing.Add($"{key.gameObject.name} (ชื่อไม่ลงท้ายด้วยเลขลูก)");
                continue;
            }

            AudioClip clip = FindClip(index);
            if (clip == null)
            {
                missing.Add($"{key.gameObject.name} (ไม่พบไฟล์เสียงลูกที่ {index:00})");
                continue;
            }

            Undo.RecordObject(key, "Assign Note Sound");
            key.noteSound = clip;
            EditorUtility.SetDirty(key);
            EditorSceneManager.MarkSceneDirty(key.gameObject.scene);
            assigned++;
        }

        string report = $"[NoteSoundAssigner] ผูกเสียงสำเร็จ {assigned}/{keys.Length} ลูก";
        if (missing.Count > 0)
        {
            report += "\nยังไม่ได้ผูก:\n - " + string.Join("\n - ", missing);
            Debug.LogWarning(report);
        }
        else
        {
            Debug.Log(report + "\nกด Ctrl+S เพื่อเซฟ Scene แล้วลอง Play ได้เลย");
        }
    }
}

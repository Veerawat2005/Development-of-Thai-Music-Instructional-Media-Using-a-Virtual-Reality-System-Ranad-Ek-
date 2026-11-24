using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// แก้ปัญหา UI ใน VR: เปลี่ยน EventSystem ให้ใช้ XR UI Input Module แทน Standalone Input Module
/// (ตัวเก่าอ่านระบบ Input เก่า ทำให้เกิด InvalidOperationException ทุกเฟรม
/// เมื่อโปรเจกต์ตั้งเป็น Input System ใหม่) และเปลี่ยน Canvas แบบ World Space
/// ให้ใช้ Tracked Device Graphic Raycaster เพื่อให้ controller ในโลก VR กดปุ่มได้
///
/// **ไฟล์นี้ต้องอยู่ในโฟลเดอร์ชื่อ "Editor" เท่านั้น** เช่น Assets/Editor/
/// วิธีใช้: เมนู Tools > VR Ranad Ek > Fix UI Input (XR) แล้วกด Ctrl+S
/// </summary>
public static class XRUIInputFixer
{
    private const string XRUIModuleType = "UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule";
    private const string TrackedRaycasterType = "UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster";

    [MenuItem("Tools/VR Ranad Ek/Fix UI Input (XR)")]
    public static void FixOpenScene()
    {
        // หา type ด้วยชื่อ เพื่อไม่ให้ compile พังถ้า namespace ของแพ็กเกจต่างเวอร์ชัน
        Type xrModule = FindType(XRUIModuleType);
        Type trackedRaycaster = FindType(TrackedRaycasterType);

        if (xrModule == null || trackedRaycaster == null)
        {
            Debug.LogError("[XRUIInputFixer] ไม่พบคลาส XRUIInputModule / TrackedDeviceGraphicRaycaster " +
                "ตรวจสอบว่าติดตั้ง XR Interaction Toolkit แล้ว หรือใช้วิธีแก้ด้วยมือแทน (Add Component ชื่อเดียวกัน)");
            return;
        }

        int changes = 0;

        // 1) EventSystem: เอาโมดูลอื่นออก ใส่ XR UI Input Module
        EventSystem[] systems = UnityEngine.Object.FindObjectsByType<EventSystem>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (systems.Length == 0)
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem));
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
            systems = new[] { es.GetComponent<EventSystem>() };
            changes++;
        }

        foreach (EventSystem es in systems)
        {
            foreach (BaseInputModule module in es.GetComponents<BaseInputModule>())
            {
                if (module.GetType() != xrModule)
                {
                    Undo.DestroyObjectImmediate(module);
                    changes++;
                }
            }
            if (es.GetComponent(xrModule) == null)
            {
                Undo.AddComponent(es.gameObject, xrModule);
                changes++;
            }
            EditorSceneManager.MarkSceneDirty(es.gameObject.scene);
        }

        // 2) Canvas แบบ World Space: เปลี่ยน raycaster ให้รองรับ controller
        Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Canvas canvas in canvases)
        {
            if (canvas.renderMode != RenderMode.WorldSpace) continue;

            GraphicRaycaster oldRaycaster = canvas.GetComponent<GraphicRaycaster>();
            if (oldRaycaster != null)
            {
                Undo.DestroyObjectImmediate(oldRaycaster);
                changes++;
            }
            if (canvas.GetComponent(trackedRaycaster) == null)
            {
                Undo.AddComponent(canvas.gameObject, trackedRaycaster);
                changes++;
            }
            if (canvas.worldCamera == null && Camera.main != null)
            {
                Undo.RecordObject(canvas, "Set Event Camera");
                canvas.worldCamera = Camera.main;
                changes++;
            }
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        }

        Debug.Log($"[XRUIInputFixer] แก้เสร็จ ({changes} รายการ) กด Ctrl+S เพื่อเซฟ Scene แล้ว Clear Console ก่อน Play ใหม่");
    }

    private static Type FindType(string fullName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type t = assembly.GetType(fullName);
            if (t != null) return t;
        }
        return null;
    }
}

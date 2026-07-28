using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using YARG.Menu;
using YARG.Menu.Main;
using YARG.Menu.Navigation;

/// <summary>
/// After an upstream update (see Tools/CareersUpdate/update-from-upstream.ps1),
/// run this once with MenuScene open to re-wire Careers onto the fresh YARG menus.
/// </summary>
public static class ApplyCareerHooksAfterUpstream
{
    private const string MenuScenePath = "Assets/Scenes/MenuScene.unity";

    private const string CareerModernPrefab =
        "Assets/Prefabs/CareersM/01_CareerViews/CareerFlow/CareerCareerMenuModern 1.prefab";
    private const string GigModernPrefab =
        "Assets/Prefabs/CareersM/02_GigViews/Coverflow/CareerGigMenuModern.prefab";
    private const string TrophyPrefab =
        "Assets/Prefabs/CareersM/03_Trophy/CampaignTrophyMenu.prefab";
    private const string MenuEntryPrefab =
        "Assets/Prefabs/Menu/MainMenu/MenuEntry.prefab";

    private static readonly (string path, MenuManager.Menu menu)[] PrefabMenuBindings =
    {
        (CareerModernPrefab, MenuManager.Menu.CareerCareerModern),
        (GigModernPrefab, MenuManager.Menu.CareerGigModern),
        (TrophyPrefab, MenuManager.Menu.CareerTrophy),
        ("Assets/Prefabs/CareersM/01_CareerViews/CareersViewClassic.prefab", MenuManager.Menu.Career),
        ("Assets/Prefabs/CareersM/02_GigViews/CareerGigMenuClassic.prefab", MenuManager.Menu.CareerGigClassic),
    };

    [MenuItem("Tools/YARG Careers/Apply Menu Hooks After Upstream Update", false, 10)]
    public static void Apply()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = SceneManager.GetActiveScene();
        if (!scene.path.Replace('\\', '/').EndsWith("MenuScene.unity"))
        {
            if (!EditorUtility.DisplayDialog(
                    "Open MenuScene?",
                    "This tool should run with MenuScene open.\n\nOpen Assets/Scenes/MenuScene.unity now?",
                    "Open MenuScene",
                    "Cancel"))
            {
                return;
            }

            scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        }

        var report = new System.Text.StringBuilder();
        report.AppendLine("Careers menu hooks applied:");

        var menuManager = Object.FindFirstObjectByType<MenuManager>(FindObjectsInactive.Include);
        if (menuManager == null)
        {
            EditorUtility.DisplayDialog("Careers Hooks", "No MenuManager found in MenuScene.", "OK");
            return;
        }

        // Must run before anything else - nested MainMenu duplicates crash MenuManager.Awake
        RepairDuplicateMainMenus(menuManager, report);

        FixCareerPrefabMenuEnums(report);

        EnsureMenuChild(menuManager, MenuManager.Menu.CareerCareerModern, CareerModernPrefab,
            "CareerCareerMenuModern", report);
        EnsureMenuChild(menuManager, MenuManager.Menu.CareerGigModern, GigModernPrefab,
            "CareerGigModern", report);
        EnsureMenuChild(menuManager, MenuManager.Menu.CareerTrophy, TrophyPrefab,
            "CampaignTrophyMenu", report);

        EnsureCareersMainMenuButton(menuManager, report);

        ValidateNoDuplicateMenuKeys(menuManager, report);

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();

        Debug.Log(report.ToString());
        EditorUtility.DisplayDialog(
            "Careers Hooks",
            report + "\nSave the scene (Ctrl+S), then enter Play Mode and click Careers.",
            "OK");
    }

    [MenuItem("Tools/YARG Careers/Repair Nested MainMenu Duplicates", false, 12)]
    public static void RepairOnly()
    {
        var menuManager = Object.FindFirstObjectByType<MenuManager>(FindObjectsInactive.Include);
        if (menuManager == null)
        {
            EditorUtility.DisplayDialog("Careers Repair", "No MenuManager found. Open MenuScene first.", "OK");
            return;
        }

        var report = new System.Text.StringBuilder();
        report.AppendLine("Repair:");
        RepairDuplicateMainMenus(menuManager, report);
        ValidateNoDuplicateMenuKeys(menuManager, report);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log(report.ToString());
        EditorUtility.DisplayDialog("Careers Repair", report.ToString(), "OK");
    }

    /// <summary>
    /// A prior bug instantiated the full MainMenu prefab under Menu Options.
    /// That nests a second MenuObject with Menu.MainMenu and makes
    /// MenuManager.ToDictionary throw - so the main menu never opens.
    /// </summary>
    private static void RepairDuplicateMainMenus(MenuManager menuManager, System.Text.StringBuilder report)
    {
        var allMainMenus = menuManager.GetComponentsInChildren<MainMenu>(true);
        if (allMainMenus.Length <= 1)
        {
            report.AppendLine("  no nested MainMenu duplicates");
            return;
        }

        // Keep the one that is a direct child of Menu Manager
        MainMenu keep = null;
        foreach (var mm in allMainMenus)
        {
            if (mm.transform.parent == menuManager.transform)
            {
                keep = mm;
                break;
            }
        }

        if (keep == null)
            keep = allMainMenus[0];

        int removed = 0;
        foreach (var mm in allMainMenus)
        {
            if (mm == keep)
                continue;

            report.AppendLine($"  REMOVED nested duplicate MainMenu at {GetTransformPath(mm.transform)}");
            Undo.DestroyObjectImmediate(mm.gameObject);
            removed++;
        }

        report.AppendLine($"  kept MainMenu at {GetTransformPath(keep.transform)} (removed {removed})");
    }

    private static void ValidateNoDuplicateMenuKeys(MenuManager menuManager, System.Text.StringBuilder report)
    {
        var objects = menuManager.GetComponentsInChildren<MenuObject>(true);
        var groups = objects.GroupBy(m => m.Menu).Where(g => g.Count() > 1).ToList();
        if (groups.Count == 0)
        {
            report.AppendLine("  MenuObject keys OK (no duplicates)");
            return;
        }

        foreach (var g in groups)
        {
            report.AppendLine($"  WARNING duplicate MenuObject key {g.Key}:");
            foreach (var mo in g)
                report.AppendLine($"    - {GetTransformPath(mo.transform)}");
        }
    }

    private static void FixCareerPrefabMenuEnums(System.Text.StringBuilder report)
    {
        foreach (var (path, menu) in PrefabMenuBindings)
        {
            var prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabRoot == null)
            {
                report.AppendLine($"  skip missing prefab: {path}");
                continue;
            }

            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var menuObject = contents.GetComponent<MenuObject>() ??
                                 contents.GetComponentInChildren<MenuObject>(true);
                if (menuObject == null)
                {
                    menuObject = contents.AddComponent<MenuObject>();
                    report.AppendLine($"  added MenuObject on {System.IO.Path.GetFileName(path)}");
                }

                var so = new SerializedObject(menuObject);
                var menuProp = so.FindProperty("<Menu>k__BackingField") ?? so.FindProperty("_menu");
                var hideProp = so.FindProperty("<HideBelow>k__BackingField") ?? so.FindProperty("_hideBelow");
                if (menuProp == null)
                {
                    report.AppendLine($"  could not find Menu property on {path}");
                    continue;
                }

                var before = menuProp.enumValueIndex;
                menuProp.enumValueIndex = (int)menu;
                if (hideProp != null)
                    hideProp.boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                report.AppendLine($"  prefab {System.IO.Path.GetFileName(path)}: Menu {before} -> {(int)menu} ({menu})");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
    }

    private static void EnsureMenuChild(
        MenuManager menuManager,
        MenuManager.Menu menu,
        string prefabPath,
        string objectName,
        System.Text.StringBuilder report)
    {
        foreach (var existing in menuManager.GetComponentsInChildren<MenuObject>(true))
        {
            if (existing.Menu != menu)
                continue;

            // Only accept direct-ish children under Menu Manager (not nested deep duplicates)
            if (!IsUnderMenuManagerRoot(existing.transform, menuManager.transform))
                continue;

            SetMenuEnum(existing, menu);
            existing.gameObject.SetActive(false);
            report.AppendLine($"  menu already registered: {menu} ({existing.gameObject.name})");
            return;
        }

        foreach (Transform child in menuManager.transform)
        {
            if (child.name != objectName &&
                child.name.IndexOf(objectName, System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            var mo = child.GetComponent<MenuObject>() ?? child.gameObject.AddComponent<MenuObject>();
            SetMenuEnum(mo, menu);
            child.gameObject.SetActive(false);
            report.AppendLine($"  re-bound existing '{child.name}' -> {menu}");
            return;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            report.AppendLine($"  FAILED to load prefab for {menu}: {prefabPath}");
            return;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, menuManager.transform);
        Undo.RegisterCreatedObjectUndo(instance, $"Register {menu}");
        instance.name = objectName;
        instance.SetActive(false);

        var menuObject = instance.GetComponent<MenuObject>() ??
                         instance.GetComponentInChildren<MenuObject>(true);
        if (menuObject == null)
            menuObject = instance.AddComponent<MenuObject>();

        SetMenuEnum(menuObject, menu);
        report.AppendLine($"  registered {menu} as '{objectName}'");
    }

    private static bool IsUnderMenuManagerRoot(Transform t, Transform menuManager)
    {
        // Direct child, or nested under a direct child that is the menu root
        while (t != null)
        {
            if (t.parent == menuManager)
                return true;
            t = t.parent;
        }
        return false;
    }

    private static void SetMenuEnum(MenuObject menuObject, MenuManager.Menu menu)
    {
        var so = new SerializedObject(menuObject);
        var menuProp = so.FindProperty("<Menu>k__BackingField") ?? so.FindProperty("_menu");
        var hideProp = so.FindProperty("<HideBelow>k__BackingField") ?? so.FindProperty("_hideBelow");
        if (menuProp != null)
            menuProp.enumValueIndex = (int)menu;
        if (hideProp != null)
            hideProp.boolValue = true;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(menuObject);
    }

    private static void EnsureCareersMainMenuButton(MenuManager menuManager, System.Text.StringBuilder report)
    {
        var mainMenu = FindMainMenu(menuManager, report);
        if (mainMenu == null)
            return;

        // Remove any leftover broken Careers entries that are actually nested MainMenus
        CleanupBadCareersEntries(mainMenu, report);

        var careers = FindCareersEntry(mainMenu);
        if (careers == null)
        {
            careers = CreateCareersEntry(mainMenu, report);
            if (careers == null)
                return;
        }
        else
        {
            report.AppendLine($"  found existing Careers entry: {GetTransformPath(careers.transform)}");
        }

        ApplyCareersAppearance(careers, report);
        WireCareersClick(careers, mainMenu, report);
    }

    private static void CleanupBadCareersEntries(MainMenu mainMenu, System.Text.StringBuilder report)
    {
        // A "Careers" object that contains MainMenu / MenuObject.MainMenu is the broken nest
        foreach (var t in mainMenu.GetComponentsInChildren<Transform>(true).ToArray())
        {
            if (t == null || t == mainMenu.transform)
                continue;
            if (!t.name.Equals("Careers", System.StringComparison.OrdinalIgnoreCase) &&
                !t.name.Equals("MainMenu", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Nested MainMenu under the real MainMenu
            if (t.GetComponent<MainMenu>() != null && t != mainMenu.transform)
            {
                report.AppendLine($"  removing bad nested object '{t.name}' at {GetTransformPath(t)}");
                Undo.DestroyObjectImmediate(t.gameObject);
            }
        }

        // Remove a bare MenuEntry named "Careers" if CareersM already exists (upgrade path)
        var options = mainMenu.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == "Menu Options");
        if (options == null)
            return;

        var careersM = options.Find("CareersM");
        var bareCareers = options.Find("Careers");
        if (careersM != null && bareCareers != null && bareCareers.GetComponent<MainMenu>() == null)
        {
            report.AppendLine("  removing leftover bare 'Careers' entry (CareersM present)");
            Undo.DestroyObjectImmediate(bareCareers.gameObject);
        }
    }

    private static MainMenu FindMainMenu(MenuManager menuManager, System.Text.StringBuilder report)
    {
        // Prefer direct child of Menu Manager
        foreach (Transform child in menuManager.transform)
        {
            var mm = child.GetComponent<MainMenu>();
            if (mm != null)
            {
                report.AppendLine($"  found MainMenu: {GetTransformPath(mm.transform)}");
                return mm;
            }
        }

        var mainMenu = Object.FindFirstObjectByType<MainMenu>(FindObjectsInactive.Include);
        if (mainMenu != null)
        {
            report.AppendLine($"  found MainMenu (fallback): {GetTransformPath(mainMenu.transform)}");
            return mainMenu;
        }

        report.AppendLine("  FAILED: MainMenu component not found in scene.");
        return null;
    }

    private static NavigatableButton FindCareersEntry(MainMenu mainMenu)
    {
        foreach (var nav in mainMenu.GetComponentsInChildren<NavigatableButton>(true))
        {
            if (nav == null)
                continue;
            // Skip anything that is part of a nested MainMenu
            if (nav.GetComponentInParent<MainMenu>() != mainMenu)
                continue;
            if (nav.gameObject.name == "CareersM" ||
                nav.gameObject.name == "Careers" ||
                nav.gameObject.name.Equals("Career", System.StringComparison.OrdinalIgnoreCase))
            {
                return nav;
            }
        }

        foreach (var tmp in mainMenu.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (tmp == null || string.IsNullOrEmpty(tmp.text))
                continue;
            if (tmp.GetComponentInParent<MainMenu>() != mainMenu)
                continue;
            if (!tmp.text.Trim().Equals("CAREERS", System.StringComparison.OrdinalIgnoreCase) &&
                !tmp.text.Trim().Equals("Careers", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var nav = tmp.GetComponentInParent<NavigatableButton>(true);
            if (nav != null)
                return nav;
        }

        return null;
    }

    private static NavigatableButton CreateCareersEntry(MainMenu mainMenu, System.Text.StringBuilder report)
    {
        var transforms = mainMenu.GetComponentsInChildren<Transform>(true);
        var parent = transforms.FirstOrDefault(t => t.name == "Menu Options")
                     ?? transforms.FirstOrDefault(t =>
                         t.name.IndexOf("Option", System.StringComparison.OrdinalIgnoreCase) >= 0)
                     ?? mainMenu.transform;

        // Clone an existing styled entry (Content / Replays) so Selection + Badge Container match.
        // Use Object.Instantiate on the in-scene instance - NOT InstantiatePrefab(source),
        // which can resolve to the whole MainMenu prefab and nest a duplicate.
        var templateGo = parent.Find("Content")?.gameObject
                         ?? parent.Find("Replays")?.gameObject
                         ?? parent.GetComponentsInChildren<NavigatableButton>(true)
                             .Select(b => b.gameObject)
                             .FirstOrDefault(go => go.name is "Quickplay" or "Practice" or "Profiles");

        GameObject instance;
        if (templateGo != null)
        {
            instance = Object.Instantiate(templateGo, parent);
            instance.name = "CareersM";
            Undo.RegisterCreatedObjectUndo(instance, "Clone CareersM MenuEntry");
            report.AppendLine($"  cloned '{templateGo.name}' -> CareersM under {parent.name}");
        }
        else
        {
            var entryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MenuEntryPrefab);
            if (entryPrefab == null)
            {
                report.AppendLine($"  FAILED: no template entry and missing {MenuEntryPrefab}");
                return null;
            }

            instance = (GameObject)PrefabUtility.InstantiatePrefab(entryPrefab, parent);
            if (instance == null)
            {
                report.AppendLine("  FAILED: could not instantiate MenuEntry prefab.");
                return null;
            }

            Undo.RegisterCreatedObjectUndo(instance, "Create CareersM MenuEntry");
            instance.name = "CareersM";
            report.AppendLine($"  created CareersM from MenuEntry prefab under {parent.name}");
        }

        instance.transform.SetSiblingIndex(0);
        PrefabUtility.RecordPrefabInstancePropertyModifications(mainMenu);

        return instance.GetComponent<NavigatableButton>() ??
               instance.GetComponentInChildren<NavigatableButton>(true);
    }

    /// <summary>
    /// Match the user's CareersM setup: CAREERS label, ALPHA orange badge, visible badge container.
    /// </summary>
    private static void ApplyCareersAppearance(NavigatableButton careers, System.Text.StringBuilder report)
    {
        careers.gameObject.name = "CareersM";

        // Primary label TMP (not the badge TMP)
        var badge = careers.GetComponentInChildren<Badge>(true);
        TextMeshProUGUI labelTmp = null;
        foreach (var tmp in careers.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (tmp == null)
                continue;
            if (badge != null && tmp.transform.IsChildOf(badge.transform))
                continue;
            labelTmp = tmp;
            break;
        }

        if (labelTmp != null)
        {
            labelTmp.text = "CAREERS";
            if (labelTmp.gameObject.name.IndexOf("Text", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                labelTmp.gameObject.name.IndexOf("Content", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                labelTmp.gameObject.name.IndexOf("History", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                labelTmp.gameObject.name.IndexOf("Replay", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                labelTmp.gameObject.name = "Careers (Text)";
            }

            EditorUtility.SetDirty(labelTmp);
        }

        // Orange ALPHA badge (matches current MenuScene CareersM overrides)
        var alphaColor = new Color(1f, 0.53971946f, 0.23113209f, 1f);
        if (badge != null)
        {
            var so = new SerializedObject(badge);
            var textProp = so.FindProperty("badgeText");
            var colorProp = so.FindProperty("badgeColor");
            if (textProp != null)
                textProp.stringValue = "ALPHA";
            if (colorProp != null)
                colorProp.colorValue = alphaColor;
            so.ApplyModifiedProperties();

            // Badge.UpdateElements is private; mirror it for immediate editor/play visibility
            var imgField = typeof(Badge).GetField("badgeImgComponent",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var textField = typeof(Badge).GetField("badgeTextComponent",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var img = imgField?.GetValue(badge) as RawImage;
            var badgeTmp = textField?.GetValue(badge) as TextMeshProUGUI;
            if (img != null)
                img.color = alphaColor;
            if (badgeTmp != null)
            {
                badgeTmp.color = alphaColor;
                badgeTmp.text = "ALPHA";
                EditorUtility.SetDirty(badgeTmp);
            }

            EditorUtility.SetDirty(badge);
        }

        // Ensure Badge Container is active (Practice has it off)
        var badgeContainer = careers.transform.Find("Badge Container");
        if (badgeContainer != null)
            badgeContainer.gameObject.SetActive(true);

        PrefabUtility.RecordPrefabInstancePropertyModifications(careers);
        report.AppendLine("  applied CareersM appearance (CAREERS + ALPHA badge)");
    }

    private static void WireCareersClick(
        NavigatableButton careers,
        MainMenu mainMenu,
        System.Text.StringBuilder report)
    {
        var onClickEvent = GetOnClickEvent(careers);
        if (onClickEvent == null)
        {
            report.AppendLine("  FAILED: NavigatableButton._onClick not found.");
            return;
        }

        while (onClickEvent.GetPersistentEventCount() > 0)
            UnityEventTools.RemovePersistentListener(onClickEvent, 0);

        UnityAction action = mainMenu.Career;
        UnityEventTools.AddPersistentListener(onClickEvent, action);

        var button = careers.GetComponent<Button>();
        if (button != null)
        {
            while (button.onClick.GetPersistentEventCount() > 0)
                UnityEventTools.RemovePersistentListener(button.onClick, 0);
            UnityEventTools.AddPersistentListener(button.onClick, action);
        }

        EditorUtility.SetDirty(careers);
        PrefabUtility.RecordPrefabInstancePropertyModifications(careers);
        report.AppendLine("  wired CareersM -> MainMenu.Career()");
    }

    private static Button.ButtonClickedEvent GetOnClickEvent(NavigatableButton button)
    {
        var field = typeof(NavigatableButton).GetField("_onClick",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return field?.GetValue(button) as Button.ButtonClickedEvent;
    }

    private static string GetTransformPath(Transform t)
    {
        var path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}

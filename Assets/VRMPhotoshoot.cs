using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UniVRM10;
using Random = UnityEngine.Random;
using System.Threading.Tasks;

public class VRMPhotoshoot : MonoBehaviour
{
    public GameObject VRM_A;
    public GameObject VRM_B;
    public Camera shootingCamera;
    public RuntimeAnimatorController baseAnimatorController;

    private string NeckBoneName = "J_Bip_C_Neck";
    private string UpperChestBoneName = "J_Bip_C_UpperChest";
    private float radius_upperbody_min = 3f;
    private float radius_upperbody_max = 5f;
    private float radius_body_min = 10f;
    private float radius_body_max = 12f;
    private int shots = 10;
    private int startPhotoNumber = 1;
    private float waitTime = 0f;
    private bool overwriteExistingFiles = false;
    private bool disableSilhouetteMode = true;
    private bool VRMBrandomMode = true;

    private Animator animatorA;
    private Animator animatorB;
    private List<AnimationClip> animationClips = new List<AnimationClip>();
    private string currentPoseName;
    private Transform focusTarget_upperbody;
    private Transform focusTarget_body;
    private string VRMDirA = "E:/AI/VRM/model";
    private string VRMDirB = "E:/AI/VRM/sotai";
    private string output_A = "E:/AI/VRM/output_A";
    private string output_B = "E:/AI/VRM/output_B";
    private const int MAX_CAMERA_POSITION_ATTEMPTS = 10;

#if UNITY_EDITOR
    public static string basePath = Path.Combine(Application.dataPath, "Resources");
#else
    public static string basePath = System.IO.Path.GetDirectoryName(Application.dataPath);
#endif

    private string[] blendShapeNames = new string[]
    {
        "happy",
        "angry",
        "sad",
        "relaxed",
        "surprised",
        "neutral"
    };

    private List<GameObject> _vrmBObjects = new List<GameObject>();
    private string currentBlendShapeName;
    private float currentBlendShapeValue;
    private async void Start()
    {
        if (shootingCamera == null)
        {
            Debug.LogError("Camera is not set. Please set it in the inspector.");
            return;
        }

        if (baseAnimatorController == null)
        {
            Debug.LogError("Base Animator Controller is not set. Please set it in the inspector.");
            return;
        }

        LoadSettings(Path.Combine(basePath, "config.ini"));

        AnimationClip[] clips = Resources.LoadAll<AnimationClip>("");
        animationClips.AddRange(clips);

        if (animationClips.Count == 0)
        {
            Debug.LogError("No animation clips found in Resources folder");
            return;
        }

        Directory.CreateDirectory(output_A);
        Directory.CreateDirectory(output_B);

        DirectoryInfo dirAInfo = new DirectoryInfo(VRMDirA);
        DirectoryInfo dirBInfo = new DirectoryInfo(VRMDirB);

        string modelbasePath = dirAInfo.Parent.FullName;

        string vrmDirNameA = dirAInfo.Name;
        string vrmDirNameB = dirBInfo.Name;

        var vrmAFiles = Directory.GetFiles(Path.Combine(modelbasePath, vrmDirNameA), "*.vrm").ToList();
        var vrmBFiles = Directory.GetFiles(Path.Combine(modelbasePath, vrmDirNameB), "*.vrm").ToList();

        await ProcessVRMFiles(vrmAFiles, vrmBFiles);
    }

    void LoadSettings(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"Settings file {filePath} not found. Using default settings.");
            return;
        }
        else
        {
            Debug.Log($"Settings file {filePath} found.");
        }

        var lines = File.ReadAllLines(filePath);
        Debug.Log($"Config file content:\n{string.Join("\n", lines)}");
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";") || line.StartsWith("#"))
                continue;

            var parts = line.Split(new[] { '=' }, 2);
            if (parts.Length != 2)
                continue;

            var key = parts[0].Trim();
            var value = parts[1].Trim();

            switch (key)
            {
                case "NeckBoneName":
                    NeckBoneName = value;
                    break;
                case "UpperChestBoneName":
                    UpperChestBoneName = value;
                    break;
                case "radius_upperbody_min":
                    if (float.TryParse(value, out float radiusUpperbodyMin))
                        radius_upperbody_min = radiusUpperbodyMin;
                    break;
                case "radius_upperbody_max":
                    if (float.TryParse(value, out float radiusUpperbodyMax))
                        radius_upperbody_max = radiusUpperbodyMax;
                    break;
                case "radius_body_min":
                    if (float.TryParse(value, out float radiusBodyMin))
                        radius_body_min = radiusBodyMin;
                    break;
                case "radius_body_max":
                    if (float.TryParse(value, out float radiusBodyMax))
                        radius_body_max = radiusBodyMax;
                    break;
                case "shots":
                    if (int.TryParse(value, out int shotsValue))
                        shots = shotsValue;
                    break;
                case "startPhotoNumber":
                    if (int.TryParse(value, out int startPhotoNum))
                        startPhotoNumber = startPhotoNum;
                    break;
                case "waitTime":
                    if (float.TryParse(value, out float wait))
                        waitTime = wait;
                    break;
                case "overwriteExistingFiles":
                    if (bool.TryParse(value, out bool overwrite))
                        overwriteExistingFiles = overwrite;
                    break;
                case "disableSilhouetteMode":
                    if (bool.TryParse(value, out bool disableSilhouette))
                        disableSilhouetteMode = disableSilhouette;
                    break;
                case "VRMBrandomMode":
                    if (bool.TryParse(value, out bool VRMBrandom))
                        VRMBrandomMode = VRMBrandom;
                    break;
                case "VRMDirA":
                    VRMDirA = value;
                    break;
                case "VRMDirB":
                    VRMDirB = value;
                    break;
                case "output_A":
                    output_A = value;
                    break;
                case "output_B":
                    output_B = value;
                    break;
                case "blendShapeNames":
                    blendShapeNames = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
                    break;
                default:
                    Debug.LogWarning($"Unknown setting: {key}");
                    break;
            }
        }
    }

    async Task ProcessVRMFiles(List<string> vrmAFiles, List<string> vrmBFiles)
    {
        // 既存の写真ファイル名を取得
        HashSet<string> existingPhotosA = overwriteExistingFiles ? new HashSet<string>() : GetExistingPhotoNumbers(output_A);
        HashSet<string> existingPhotosB = overwriteExistingFiles ? new HashSet<string>() : GetExistingPhotoNumbers(output_B);

        // VRM_Bモデルの読み込み
        foreach (var vrmB in vrmBFiles)
        {
            var loadVrm = await LoadVRM(vrmB);
            SetMeshUpdateWhenOffscreenForGameObject(loadVrm);
            loadVrm.SetActive(false);
            _vrmBObjects.Add(loadVrm);
        }

        var photoNumber = startPhotoNumber;
        foreach (var vrmFile in vrmAFiles)
        {
            HashSet<int> photosToTakeForThisModel = new HashSet<int>();
            for (int i = photoNumber; i < photoNumber + shots; i++)
            {
                bool allPhotosExist = true;  // 全ての写真が存在するかを確認

                // VRMBrandomMode の場合はサフィックスなし、false の場合はサフィックスありで写真をチェック
                if (VRMBrandomMode)
                {
                    // 連番だけをチェック
                    string photoFileNameA = $"{i:D6}.webp";
                    string photoFileNameB = $"{i:D6}.webp";

                    bool photoExistsA = existingPhotosA.Contains(photoFileNameA);
                    bool photoExistsB = existingPhotosB.Contains(photoFileNameB);

                    // どちらかの写真が存在しない場合
                    if (!photoExistsA || !photoExistsB)
                    {
                        allPhotosExist = false;

                        // 既存の写真を削除
                        if (photoExistsA)
                        {
                            string fullPathA = Path.Combine(output_A, photoFileNameA);
                            File.Delete(fullPathA);
                            existingPhotosA.Remove(photoFileNameA);
                        }
                        if (photoExistsB)
                        {
                            string fullPathB = Path.Combine(output_B, photoFileNameB);
                            File.Delete(fullPathB);
                            existingPhotosB.Remove(photoFileNameB);
                        }
                    }
                }
                else
                {
                    // サフィックス付きのファイルをチェック (_01, _02, _03, ...)
                    for (int j = 1; j <= _vrmBObjects.Count; j++)
                    {
                        string photoFileNameA = $"{i:D6}_{j:D2}.webp";
                        string photoFileNameB = $"{i:D6}_{j:D2}.webp";

                        bool photoExistsA = existingPhotosA.Contains(photoFileNameA);
                        bool photoExistsB = existingPhotosB.Contains(photoFileNameB);

                        // どちらかの写真が存在しない場合
                        if (!photoExistsA || !photoExistsB)
                        {
                            allPhotosExist = false;

                            // 既存の写真を削除
                            if (photoExistsA)
                            {
                                string fullPathA = Path.Combine(output_A, photoFileNameA);
                                File.Delete(fullPathA);
                                existingPhotosA.Remove(photoFileNameA);
                            }
                            if (photoExistsB)
                            {
                                string fullPathB = Path.Combine(output_B, photoFileNameB);
                                File.Delete(fullPathB);
                                existingPhotosB.Remove(photoFileNameB);
                            }
                        }
                    }
                }

                // どちらかの写真が存在しない場合、撮影リストに追加
                if (!allPhotosExist)
                {
                    photosToTakeForThisModel.Add(i);
                }
            }

            // 撮影する写真がすでに全て存在する場合はスキップ
            if (photosToTakeForThisModel.Count == 0)
            {
                Debug.Log($"モデル {vrmFile} の全ての写真が既に存在します。処理をスキップします。");
                photoNumber += shots;
                continue;
            }

            // VRM_Aの読み込み
            VRM_A = await LoadVRM(vrmFile);
            VRM_A.SetActive(true);

            GameObject VRM_A_SILHOUETTE = null;

            if (!disableSilhouetteMode)
            {
                VRM_A_SILHOUETTE = Instantiate(VRM_A);
                SetMaterialsToBlack(VRM_A_SILHOUETTE);
                VRM_A_SILHOUETTE.SetActive(false);
            }

            var vrmBObjects = new List<GameObject>(_vrmBObjects);

            if (VRM_A_SILHOUETTE != null)
            {
                vrmBObjects.Add(VRM_A_SILHOUETTE);
            }

            animatorA = VRM_A.GetComponent<Animator>();
            animatorA.runtimeAnimatorController = baseAnimatorController;

            SetFocusTargets();
            SetMeshUpdateWhenOffscreenForGameObject(VRM_A);

            if (VRMBrandomMode)
            {
                // ランダムモードでの撮影処理
                await StartPhotoshoot(vrmBObjects, photosToTakeForThisModel);
            }
            else
            {
                // 全てのVRM_Bに対して写真撮影を実行
                await StartPhotoshootAllVRMB(vrmBObjects, photosToTakeForThisModel);
            }

            photoNumber += shots;

            Destroy(VRM_A);
            if (VRM_A_SILHOUETTE != null)
            {
                Destroy(VRM_A_SILHOUETTE);
            }
        }

        _vrmBObjects.ForEach(Destroy);

        Debug.Log("Photoshoot complete. All required photos taken.");
    #if UNITY_EDITOR
        EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }

    async Task<GameObject> LoadVRM(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError($"File not found: {path}");
            throw new FileNotFoundException("File not found", path);
        }

        try
        {
            return (await Vrm10.LoadPathAsync(path)).gameObject;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load VRM: {e.Message}");
            throw;
        }
    }

    void SetFocusTargets()
    {
        focusTarget_upperbody = FindBoneByName(VRM_A.transform, NeckBoneName);
        focusTarget_body = FindBoneByName(VRM_A.transform, UpperChestBoneName);

        if (focusTarget_upperbody == null)
        {
            Debug.LogWarning(
                $"Upperbody focus target bone '{NeckBoneName}' not found. Using VRM_A root transform instead.");
            focusTarget_upperbody = VRM_A.transform;
        }

        if (focusTarget_body == null)
        {
            Debug.LogWarning(
                $"Fullbody focus target bone '{UpperChestBoneName}' not found. Using VRM_A root transform instead.");
            focusTarget_body = VRM_A.transform;
        }
    }

    Transform FindBoneByName(Transform parent, string boneName)
    {
        if (parent.name == boneName)
            return parent;

        foreach (Transform child in parent)
        {
            Transform result = FindBoneByName(child, boneName);
            if (result != null)
                return result;
        }

        return null;
    }

    void SetMeshUpdateWhenOffscreenForGameObject(GameObject go)
    {
        SkinnedMeshRenderer[] renderers = go.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            renderer.updateWhenOffscreen = true;
        }
    }

    private async Task StartPhotoshoot(List<GameObject> vrmBObjects, HashSet<int> photosToTake)
    {
        foreach (int photoNumber in photosToTake)
        {
            await TakeSinglePhoto(photoNumber, vrmBObjects);
        }
    }

    private async Task StartPhotoshootAllVRMB(List<GameObject> vrmBObjects, HashSet<int> photosToTake)
    {
        foreach (int photoNumber in photosToTake)
        {
            await TakeSinglePhotoAllVRMB(photoNumber, vrmBObjects);
        }
    }

    HashSet<string> GetExistingPhotoNumbers(string directory)
    {
        HashSet<string> existingNumbers = new HashSet<string>();

        if (Directory.Exists(directory))
        {
            string[] files = Directory.GetFiles(directory, "*.webp");
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                existingNumbers.Add(fileName);
            }
        }

        return existingNumbers;
    }

    private async Task TakeSinglePhoto(int photoNumber, List<GameObject> vrmBObjects)
    {
        if (!VRM_A.activeInHierarchy)
        {
            Debug.LogError("VRM_A is inactive. Stopping operation.");
            return;
        }

        VRM_B = vrmBObjects[Random.Range(0, vrmBObjects.Count)];
        animatorB = VRM_B.GetComponent<Animator>();

        await ApplyRandomPose();

        await Task.Yield();
        await Task.Delay(100);

        bool photoATaken = false;
        bool photoBTaken = false;
        int retryLimit = 3;

        while ((!photoATaken || !photoBTaken) && retryLimit-- > 0)
        {
            var cameraSetup = await TrySetCameraPositionAsync();
            if (!cameraSetup.success)
            {
                await ApplyRandomPose();
                await Task.Yield();
                await Task.Delay(100);
                continue;
            }

            await Task.Delay((int)(waitTime * 1000));

            if (!photoATaken)
            {
                photoATaken = await TakePhotoWithRetry(VRM_A, output_A, photoNumber.ToString("D6"));
            }

            if (photoATaken && !photoBTaken)
            {
                VRM_A.SetActive(false);
                await Task.Yield();

                VRM_B.SetActive(true);
                await Task.Yield();
                await Task.Delay(100);

                await ApplyPoseToVRM_B();

                // ブレンドシェイプを適用
                if (currentBlendShapeName != null)
                {
                    ApplyRandomBlendShape(VRM_B, currentBlendShapeName, currentBlendShapeValue, false);
                }

                AdjustVRM_BToCamera(cameraSetup.targetFocus);

                await Task.Yield();
                await Task.Delay(100);

                photoBTaken = await TakePhotoWithRetry(VRM_B, output_B, photoNumber.ToString("D6"));

                VRM_B.SetActive(false);
                await Task.Yield();

                VRM_A.SetActive(true);
                await Task.Yield();

                if (!photoBTaken)
                {
                    string fileName = $"{photoNumber:D6}.webp";
                    string filePath = Path.Combine(output_A, fileName);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
            }

            if (!photoATaken || !photoBTaken)
            {
                await ApplyRandomPose();
                await Task.Yield();
                await Task.Delay(100);
            }
        }

        if (!photoATaken || !photoBTaken)
        {
            Debug.LogError($"Failed to take photo {photoNumber} for VRM_A");
        }

        VRM_B.SetActive(false);
    }

    private async Task TakeSinglePhotoAllVRMB(int photoNumber, List<GameObject> vrmBObjects)
    {
        if (!VRM_A.activeInHierarchy)
        {
            Debug.LogError("VRM_A is inactive. Stopping operation.");
            return;
        }

        await ApplyRandomPose();

        await Task.Yield();
        await Task.Delay(100);

        var cameraSetup = await TrySetCameraPositionAsync();
        if (!cameraSetup.success)
        {
            Debug.LogError($"Failed to set camera position for photo {photoNumber}");
            return;
        }

        await Task.Delay((int)(waitTime * 1000));

        string originalPhotoNumber = photoNumber.ToString("D6");

        // VRM_Aの写真をメモリ上に保持
        Texture2D vrmAScreenshot = await CaptureScreenshot(VRM_A);

        if (vrmAScreenshot != null)
        {
            VRM_A.SetActive(false);
            await Task.Yield();

            bool allPhotosTaken = true;

            for (int i = 0; i < vrmBObjects.Count; i++)
            {
                VRM_B = vrmBObjects[i];
                if (VRM_B == null)
                {
                    Debug.LogWarning($"VRM_B at index {i} is null. Skipping this model.");
                    continue;
                }

                animatorB = VRM_B.GetComponent<Animator>();

                VRM_B.SetActive(true);
                await Task.Yield();
                await Task.Delay(100);

                await ApplyPoseToVRM_B();

                // ブレンドシェイプを適用
                if (currentBlendShapeName != null)
                {
                    ApplyRandomBlendShape(VRM_B, currentBlendShapeName, currentBlendShapeValue, false);
                }

                AdjustVRM_BToCamera(cameraSetup.targetFocus);

                await Task.Yield();
                await Task.Delay(100);

                string suffixedPhotoNumber = $"{originalPhotoNumber}_{(i + 1):D2}";
                bool photoBTaken = await TakePhotoWithRetry(VRM_B, output_B, suffixedPhotoNumber);

                if (photoBTaken)
                {
                    // VRM_Aの画像を保存（接尾辞付き）
                    string filePathA = Path.Combine(output_A, $"{suffixedPhotoNumber}.webp");
                    File.WriteAllBytes(filePathA, vrmAScreenshot.EncodeToPNG());
                }
                else
                {
                    allPhotosTaken = false;
                    Debug.LogWarning($"Failed to take photo for VRM_B {suffixedPhotoNumber}");
                }

                VRM_B.SetActive(false);
                await Task.Yield();
            }

            VRM_A.SetActive(true);
            await Task.Yield();

            // すべての写真が正常に撮影されなかった場合、全ての写真を削除
            if (!allPhotosTaken)
            {
                for (int i = 0; i < vrmBObjects.Count; i++)
                {
                    string suffixedPhotoNumber = $"{originalPhotoNumber}_{(i + 1):D2}";
                    string filePathA = Path.Combine(output_A, $"{suffixedPhotoNumber}.webp");
                    if (File.Exists(filePathA))
                    {
                        File.Delete(filePathA);
                    }
                    string filePathB = Path.Combine(output_B, $"{suffixedPhotoNumber}.webp");
                    if (File.Exists(filePathB))
                    {
                        File.Delete(filePathB);
                    }
                }
                Debug.LogWarning($"Deleted all photos for {originalPhotoNumber} due to incomplete set");
            }

            // メモリ解放
            Destroy(vrmAScreenshot);
        }
        else
        {
            Debug.LogError($"Failed to capture screenshot for VRM_A {photoNumber}");
        }
    }

    private async Task<Texture2D> CaptureScreenshot(GameObject target)
    {
        target.SetActive(true);
        await Task.Yield();

        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = null;

        RenderTexture renderTexture = new RenderTexture(1024, 1024, 24);
        shootingCamera.targetTexture = renderTexture;
        shootingCamera.Render();

        RenderTexture.active = renderTexture;
        Texture2D screenshot = new Texture2D(1024, 1024, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);
        screenshot.Apply();

        RenderTexture.active = currentRT;
        shootingCamera.targetTexture = null;

        Destroy(renderTexture);

        return screenshot;
    }

    async Task ApplyRandomPose()
    {
        if (!VRM_A.activeInHierarchy)
        {
            Debug.LogError("VRM_A is inactive. Stopping operation.");
            return;
        }

        if (animationClips.Count > 0)
        {
            AnimationClip clip = animationClips[Random.Range(0, animationClips.Count)];
            if (clip != null)
            {
                VRM_A.SetActive(true);
                currentPoseName = clip.name;
                animatorA.Play(currentPoseName, 0, 0);
                await Task.Yield();

                animatorA.Play(currentPoseName, 0, Random.Range(0f, clip.length));
                animatorA.Update(0);

                await Task.Yield();
                await Task.Delay(100);

                animatorA.StopPlayback();

                if (blendShapeNames.Length > 0)
                {
                    currentBlendShapeName = blendShapeNames[Random.Range(0, blendShapeNames.Length)];
                    currentBlendShapeValue = Random.Range(0.5f, 1f);

                    ApplyRandomBlendShape(VRM_A, currentBlendShapeName, currentBlendShapeValue, true);
                    if (VRM_B != null)
                    {
                        ApplyRandomBlendShape(VRM_B, currentBlendShapeName, currentBlendShapeValue, false);
                    }
                }

                await Task.Delay(100);
            }
            else
            {
                Debug.LogError("Failed to load animation clip");
            }
        }
        else
        {
            Debug.LogWarning("No animation clips found in the specified resource path.");
        }
    }

    async Task ApplyPoseToVRM_B()
    {
        foreach (Transform boneA in animatorA.GetComponentsInChildren<Transform>())
        {
            Transform boneB = FindCorrespondingBone(animatorB, boneA.name);
            if (boneB != null)
            {
                boneB.localPosition = boneA.localPosition;
                boneB.localRotation = boneA.localRotation;
            }
        }
        await Task.Yield();
    }

    Transform FindCorrespondingBone(Animator targetAnimator, string boneName)
    {
        Transform[] allBones = targetAnimator.GetComponentsInChildren<Transform>();
        return Array.Find(allBones, b => b.name == boneName);
    }

    async Task<(bool success, Vector3 cameraPosition, Quaternion cameraRotation, Transform targetFocus)> TrySetCameraPositionAsync()
    {
        for (int attempt = 0; attempt < MAX_CAMERA_POSITION_ATTEMPTS; attempt++)
        {
            var result = SetCameraPosition();
            if (result.success)
            {
                await Task.Yield();
                if (IsSubjectInView())
                {
                    return result;
                }
            }
        }

        return (false, Vector3.zero, Quaternion.identity, null);
    }

    (bool success, Vector3 cameraPosition, Quaternion cameraRotation, Transform targetFocus) SetCameraPosition()
    {
        Vector3 randomDirection;
        do
        {
            randomDirection = Random.onUnitSphere;
        } while (Mathf.Abs(randomDirection.y) > 0.4f);

        bool isUpperBody = false;
        if (currentPoseName.StartsWith("010_"))
        {
            isUpperBody = Random.value > 0.5f;
        }

        float minRadius, maxRadius;
        Transform targetFocus;

        if (isUpperBody)
        {
            targetFocus = focusTarget_upperbody;
            minRadius = radius_upperbody_min;
            maxRadius = radius_upperbody_max;
        }
        else
        {
            targetFocus = focusTarget_body;
            minRadius = radius_body_min;
            maxRadius = radius_body_max;
        }

        if (targetFocus == null)
        {
            return (false, Vector3.zero, Quaternion.identity, null);
        }

        float randomRadius = Random.Range(minRadius, maxRadius);
        Vector3 cameraPosition = targetFocus.position + randomDirection * randomRadius;
        Quaternion cameraRotation = Quaternion.LookRotation(targetFocus.position - cameraPosition);

        shootingCamera.transform.position = cameraPosition;
        shootingCamera.transform.rotation = cameraRotation;

        return (true, cameraPosition, cameraRotation, targetFocus);
    }

    bool IsSubjectInView()
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(shootingCamera);
        Bounds bounds = new Bounds(VRM_A.transform.position, Vector3.one * 2f);
        return GeometryUtility.TestPlanesAABB(planes, bounds);
    }

    async Task<bool> TakePhotoWithRetry(GameObject target, string directory, string photoNumber)
    {
        await Task.Yield();

        bool photoTaken = false;
        string fileName = $"{photoNumber}.webp";
        string filePath = Path.Combine(directory, fileName);

        if (File.Exists(filePath) && !overwriteExistingFiles)
        {
            Debug.Log($"File {filePath} already exists, skipping photo.");
            return false;
        }

        try
        {
            photoTaken = TakePhoto(target, directory, photoNumber);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error taking photo: {e.Message}");
        }

        return photoTaken;
    }

    bool TakePhoto(GameObject target, string directory, string photoNumber)
    {
        string fileName = $"{photoNumber}.webp";
        string filePath = Path.Combine(directory, fileName);

        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = null;

        RenderTexture renderTexture = new RenderTexture(1024, 1024, 24);
        shootingCamera.targetTexture = renderTexture;
        shootingCamera.Render();

        RenderTexture.active = renderTexture;
        Texture2D screenshot = new Texture2D(1024, 1024, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);
        screenshot.Apply();

        byte[] bytes = screenshot.EncodeToPNG();
        File.WriteAllBytes(filePath, bytes);

        RenderTexture.active = currentRT;
        shootingCamera.targetTexture = null;

        Destroy(renderTexture);
        Destroy(screenshot);

        Debug.Log($"Photo taken: {filePath}");
        return true;
    }

    void ApplyRandomBlendShape(GameObject vrm, string selectedBlendShape, float value, bool needWarn)
    {
        // Vrm10Instanceコンポーネントを取得
        var vrmInstance = vrm.GetComponent<UniVRM10.Vrm10Instance>();
        if (vrmInstance == null)
        {
            if (needWarn)
            {
                Debug.LogError("Vrm10Instanceが見つかりません");
            }
            return;
        }

        // 既存の表情をリセット
        vrmInstance.Runtime.Expression.SetWeights(new Dictionary<UniVRM10.ExpressionKey, float>());

        // ExpressionKeyを作成
        UniVRM10.ExpressionKey expressionKey;

        // selectedBlendShapeがExpressionPresetに存在するか確認
        if (Enum.TryParse<UniVRM10.ExpressionPreset>(selectedBlendShape, true, out var preset))
        {
            // プリセット表情の場合
            expressionKey = UniVRM10.ExpressionKey.CreateFromPreset(preset);
        }
        else
        {
            // カスタム表情の場合
            expressionKey = UniVRM10.ExpressionKey.CreateCustom(selectedBlendShape);
        }

        // 表情を適用
        vrmInstance.Runtime.Expression.SetWeight(expressionKey, value);
    }



    void SetMaterialsToBlack(GameObject vrm)
    {
        var renderers = vrm.GetComponentsInChildren<Renderer>();

        foreach (var renderer in renderers)
        {
            foreach (var material in renderer.materials)
            {
                if (material.HasProperty("_Color"))
                {
                    material.SetColor("_Color", Color.black);
                }

                if (material.HasProperty("_LitColor"))
                {
                    material.SetColor("_LitColor", Color.black);
                }

                if (material.HasProperty("_ShadeColor"))
                {
                    material.SetColor("_ShadeColor", Color.black);
                }

                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", Color.black);
                }

                if (material.HasProperty("_RimColor"))
                {
                    material.SetColor("_RimColor", Color.black);
                }

                if (material.HasTexture("_MatcapTex"))
                {
                    material.SetTexture("_MatcapTex", null);
                }
            }
        }

        var vrmInstance = vrm.GetComponent<UniVRM10.Vrm10Instance>();
        if (vrmInstance != null && vrmInstance.SpringBone != null)
        {
            foreach (var spring in vrmInstance.SpringBone.Springs)
            {
                spring.Joints.Clear();
            }
            Debug.Log("シルエットモデルのSpringBoneのジョイントがクリアされ、無効化されました。");
        }
    }

    void AdjustVRM_BToCamera(Transform originalTargetFocus)
    {
        Transform targetFocusB;
        if (originalTargetFocus == focusTarget_upperbody)
        {
            targetFocusB = FindBoneByName(VRM_B.transform, NeckBoneName);
        }
        else
        {
            targetFocusB = FindBoneByName(VRM_B.transform, UpperChestBoneName);
        }

        if (targetFocusB != null)
        {
            Vector3 offset = targetFocusB.position - originalTargetFocus.position;
            VRM_B.transform.position -= offset;

            Quaternion rotationDiff = originalTargetFocus.rotation * Quaternion.Inverse(targetFocusB.rotation);
            VRM_B.transform.rotation = rotationDiff * VRM_B.transform.rotation;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UniVRM10;
using Random = UnityEngine.Random;

public class VRMPhotoshoot : MonoBehaviour
{
    public GameObject VRM_A;
    public GameObject VRM_B;
    public Camera shootingCamera;
    public RuntimeAnimatorController baseAnimatorController;
    private  string NeckBoneName = "J_Bip_C_Neck";
    private  string UpperChestBoneName = "J_Bip_C_UpperChest";
    private  float radius_upperbody_min = 3f;
    private  float radius_upperbody_max = 5f;
    private  float radius_body_min = 10f;
    private  float radius_body_max = 12f;
    private  int shots = 10;
    private  int startPhotoNumber = 1;
    private  float waitTime = 0f;
    private bool overwriteExistingFiles = false;
    private bool disableSilhouetteMode = true;
    private Animator animatorA;
    private Animator animatorB;
    private List<AnimationClip> animationClips = new List<AnimationClip>();
    private string currentPoseName;
    private Transform focusTarget_upperbody;
    private Transform focusTarget_body;
    private string VRMDirA = "E:/AI/kari/model";
    private string VRMDirB = "E:/AI/kari/sotai";

    private string output_A = "photos_A";
    private string output_B = "photos_B";

    private const int MAX_CAMERA_POSITION_ATTEMPTS = 10;

#if UNITY_EDITOR
    public static string basePath = Path.Combine(Application.dataPath, "Resources");
#else
    public static string basePath = System.IO.Path.GetDirectoryName(Application.dataPath);
#endif


    private string[] blendShapeNames = new string[]
    {
        "Fcl_ALL_Neutral",
        "Fcl_ALL_Angry",
        "Fcl_ALL_Fun",
        "Fcl_ALL_Joy",
        "Fcl_ALL_Sorrow",
        "Fcl_ALL_Surprised"
    };

    private List<GameObject> _vrmBObjects = new List<GameObject>();

    private async void Start()
    {
        if (shootingCamera == null)
        {
            Debug.LogError("Camera is not set. Please set them in the inspector.");
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

        // DirectoryInfoを使って親ディレクトリを取得
        DirectoryInfo dirAInfo = new DirectoryInfo(VRMDirA);
        DirectoryInfo dirBInfo = new DirectoryInfo(VRMDirB);

        // modelbasePath は一つ上のディレクトリ
        string modelbasePath = dirAInfo.Parent.FullName;

        // vrmDirNameA と vrmDirNameB は末端のディレクトリ名
        string vrmDirNameA = dirAInfo.Name;
        string vrmDirNameB = dirBInfo.Name;


        // VRMファイルのリストを取得
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
            // VRM_Aの読み込み
            VRM_A = await LoadVRM(vrmFile);
            VRM_A.SetActive(true);

            GameObject VRM_A_SILHOUETTE = null;

            // シルエットモードが無効な場合のみシルエットを生成
            if (!disableSilhouetteMode)
            {
                VRM_A_SILHOUETTE = Instantiate(VRM_A);
                SetMaterialsToBlack(VRM_A_SILHOUETTE);
                VRM_A_SILHOUETTE.SetActive(false);
            }

            var vrmBObjects = new List<GameObject>(_vrmBObjects);

            // シルエットが生成されている場合にのみリストに追加
            if (VRM_A_SILHOUETTE != null)
            {
                vrmBObjects.Add(VRM_A_SILHOUETTE);
            }

            animatorA = VRM_A.GetComponent<Animator>();
            animatorA.runtimeAnimatorController = baseAnimatorController;

            SetFocusTargets();
            SetMeshUpdateWhenOffscreenForGameObject(VRM_A);

            // 写真撮影を開始
            await StartPhotoshoot(vrmBObjects, photoNumber);

            photoNumber += shots;

            // VRM_Aおよびシルエットの破棄
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

    private async Task StartPhotoshoot(List<GameObject> vrmBObjects, int startPhotoNumber)
    {
        HashSet<int> existingPhotosA = overwriteExistingFiles ? new HashSet<int>() : GetExistingPhotoNumbers(output_A);
        HashSet<int> existingPhotosB = overwriteExistingFiles ? new HashSet<int>() : GetExistingPhotoNumbers(output_B);
        HashSet<int> photosToTake = new HashSet<int>();

        // overwriteExistingFiles が true の場合は既存のファイルを無視して全て撮影する
        for (int i = startPhotoNumber; i < startPhotoNumber + shots; i++)
        {
            if (overwriteExistingFiles || (!existingPhotosA.Contains(i) && !existingPhotosB.Contains(i)))
            {
                photosToTake.Add(i); // 上書きするか、新規ファイルの場合に撮影対象として追加
            }
        }

        foreach (int photoNumber in photosToTake)
        {
            await TakeSinglePhoto(photoNumber, vrmBObjects);
        }
    }

    HashSet<int> GetExistingPhotoNumbers(string directory)
    {
        HashSet<int> existingNumbers = new HashSet<int>();

        if (Directory.Exists(directory))
        {
            string[] files = Directory.GetFiles(directory, "*.webp");
            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                if (int.TryParse(fileName, out int number))
                {
                    existingNumbers.Add(number);
                }
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

        // VRM_Bをランダムに選択
        VRM_B = vrmBObjects[Random.Range(0, vrmBObjects.Count)];
        animatorB = VRM_B.GetComponent<Animator>();

        await ApplyRandomPose();

        // ポーズ適用後にフレームの更新を待つ
        await Task.Yield();
        await Task.Delay(100); // 必要に応じて待機時間を調整

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
                photoATaken = await TakePhotoWithRetry(VRM_A, output_A, photoNumber);
            }

            if (photoATaken && !photoBTaken)
            {
                VRM_A.SetActive(false);
                await Task.Yield();

                VRM_B.SetActive(true);
                await Task.Yield();
                await Task.Delay(100); // モデル切り替えの待機時間を追加

                // VRM_Bのポーズを適用し、位置を調整する
                await ApplyPoseToVRM_B();
                AdjustVRM_BToCamera(cameraSetup.targetFocus);

                await Task.Yield();
                await Task.Delay(100);

                photoBTaken = await TakePhotoWithRetry(VRM_B, output_B, photoNumber);

                VRM_B.SetActive(false);
                await Task.Yield();

                VRM_A.SetActive(true);
                await Task.Yield();

                if (!photoBTaken)
                {
                    string fileName = $"{photoNumber:D6}.webp";
                    string filePath = Path.Combine(output_A, fileName);
                    File.Delete(filePath);
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
            // カメラの位置と回転を保持したまま、VRM_Bの位置を調整
            Vector3 offset = targetFocusB.position - originalTargetFocus.position;
            VRM_B.transform.position -= offset;

            // VRM_Bの回転も調整して、カメラに対して同じ向きになるようにする
            Quaternion rotationDiff = originalTargetFocus.rotation * Quaternion.Inverse(targetFocusB.rotation);
            VRM_B.transform.rotation = rotationDiff * VRM_B.transform.rotation;
        }
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
                await Task.Delay(100); // ポーズ適用の待機時間を増加

                animatorA.StopPlayback();

                if (blendShapeNames.Length > 0)
                {
                    var selectedBlendShape = blendShapeNames[Random.Range(0, blendShapeNames.Length)];
                    ApplyRandomBlendShape(VRM_A, selectedBlendShape, true);
                    ApplyRandomBlendShape(VRM_B, selectedBlendShape, false);
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
        await Task.Yield(); // ポーズ適用後にフレーム更新を待つ
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
                await Task.Yield(); // カメラ位置設定後にフレーム更新を待つ
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

    async Task<bool> TakePhotoWithRetry(GameObject target, string directory, int photoNumber)
    {
        await Task.Yield();

        bool photoTaken = false;
        string fileName = $"{photoNumber:D6}.webp";
        string filePath = Path.Combine(directory, fileName);

        // 上書きしない場合にファイルが存在していたらスキップする
        if (File.Exists(filePath) && !overwriteExistingFiles)
        {
            Debug.Log($"File {filePath} already exists, skipping photo.");
            return false; // 上書きしない場合はスキップ
        }

        try
        {
            // ファイルが既に存在していても上書きする
            photoTaken = TakePhoto(target, directory, photoNumber);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error taking photo: {e.Message}");
        }

        return photoTaken;
    }

    bool TakePhoto(GameObject target, string directory, int photoNumber)
    {
        string fileName = $"{photoNumber:D6}.webp";
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

    void ApplyRandomBlendShape(GameObject vrm, string selectedBlendShape, bool needWarn)
    {
        SkinnedMeshRenderer smr = vrm.GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr == null)
        {
            if (needWarn)
            {
                Debug.LogError("SkinnedMeshRenderer not found on VRM");
            }
            return;
        }

        if (smr.sharedMesh.blendShapeCount == 0)
        {
            if (needWarn)
            {
                Debug.LogWarning("No blend shapes found on VRM. Skipping blend shape application.");
            }
            return;
        }

        for (int i = 0; i < smr.sharedMesh.blendShapeCount; i++)
        {
            smr.SetBlendShapeWeight(i, 0);
        }

        float randomValue = Random.Range(0.5f, 1f);
        int blendShapeIndex = smr.sharedMesh.GetBlendShapeIndex(selectedBlendShape);

        if (blendShapeIndex != -1)
        {
            smr.SetBlendShapeWeight(blendShapeIndex, randomValue * 100);
        }
        else if (needWarn)
        {
            Debug.LogWarning($"Blend shape {selectedBlendShape} not found");
        }
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

        // シルエットモデルのSpringBoneを無効化
        var vrmInstance = vrm.GetComponent<UniVRM10.Vrm10Instance>();
        if (vrmInstance != null && vrmInstance.SpringBone != null)
        {
            // 各スプリングの更新を無効化
            foreach (var spring in vrmInstance.SpringBone.Springs)
            {
                spring.Joints.Clear();  // スプリングのジョイントをクリアすることで動作を停止
            }
            Debug.Log("シルエットモデルのSpringBoneのジョイントがクリアされ、無効化されました。");
        }
    }
}


using System.Collections.Generic;
using Survival.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Survival.EditorTools
{
    /// <summary>
    /// Nướng mặt lưới đi đường của sân đấu TỪ COLLIDER, rồi lưu thành asset.
    ///
    /// Chạy bằng menu <c>Survival > Bake NavMesh</c>, và phải chạy lại mỗi khi dựng lại bản đồ.
    ///
    /// Lý do không dùng cửa sổ Navigation có sẵn nằm trong chú thích của <see cref="NavMeshProvider"/>:
    /// lệnh bake của Unity đọc mesh, mà mesh cây rộng gấp 7 lần thân cây, nên mặt lưới cấm cả
    /// những khoảng rừng người chơi đi lọt.
    ///
    /// Toàn bộ thông số đọc từ <see cref="NavMeshProvider"/> trong scene chứ không viết cứng ở đây —
    /// muốn chỉnh thì sửa trên Inspector rồi bake lại, không phải mở code.
    /// </summary>
    public static class NavMeshBaker
    {
        private const string AssetPath = "Assets/_Project/Nav/ArenaNavMesh.asset";

        [MenuItem("Survival/Bake NavMesh", priority = 20)]
        public static void Bake()
        {
            var provider = Object.FindObjectOfType<NavMeshProvider>();
            if (provider == null)
            {
                EditorUtility.DisplayDialog(
                    "Chưa có NavMeshProvider",
                    "Scene chưa có object nào mang component NavMeshProvider.\n\n" +
                    "Thêm nó vào nhóm --- Systems ---, khai báo layer hình học và các nhánh bị chặn, rồi bake lại.",
                    "OK");
                return;
            }

            if (provider.GeometryMask.value == 0)
            {
                EditorUtility.DisplayDialog(
                    "Chưa chọn layer hình học",
                    "Trường 'Lấy hình học từ đâu' đang trống nên không có collider nào được thu thập.\n\n" +
                    "Chọn Ground, Wall và Obstacle rồi bake lại.",
                    "OK");
                return;
            }

            // Đánh dấu theo NHÁNH: mọi collider nằm dưới các gốc này đều là chỗ không đi được.
            // Cách này gọn hơn hẳn việc gắn cờ cho từng vật, và quan trọng hơn là nó không thể
            // bị bỏ sót khi công cụ dựng bản đồ sinh thêm vật mới.
            var markups = new List<NavMeshBuildMarkup>();
            foreach (var root in provider.BlockedRoots)
            {
                if (root == null)
                    continue;

                markups.Add(new NavMeshBuildMarkup
                {
                    root = root,
                    overrideArea = true,
                    area = NavMeshProvider.NotWalkableArea,
                });
            }

            var sources = new List<NavMeshBuildSource>();
            var volume = new Bounds(Vector3.zero, provider.VolumeSize);

            NavMeshBuilder.CollectSources(
                volume,
                provider.GeometryMask.value,
                NavMeshCollectGeometry.PhysicsColliders,
                defaultArea: 0,
                markups,
                sources);

            if (sources.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Không thu được hình học nào",
                    "Không có collider nào nằm trên các layer đã chọn và trong khối bake.\n\n" +
                    "Kiểm tra lại layer và kích thước khối bake.",
                    "OK");
                return;
            }

            // Nướng đè lên chính asset cũ nếu nó đã tồn tại, để KHÔNG ĐỔI GUID.
            // Xoá rồi tạo lại sẽ sinh GUID mới và làm đứt tham chiếu đã lưu trong scene —
            // đúng cái bẫy đã một lần làm mất sạch Animator Controller của dự án này.
            var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(AssetPath);
            NavMeshData data;
            if (existing != null)
            {
                NavMeshBuilder.UpdateNavMeshData(existing, provider.BuildSettings, sources, volume);
                data = existing;
                EditorUtility.SetDirty(data);
            }
            else
            {
                data = NavMeshBuilder.BuildNavMeshData(
                    provider.BuildSettings, sources, volume, Vector3.zero, Quaternion.identity);
                data.name = "ArenaNavMesh";

                string folder = System.IO.Path.GetDirectoryName(AssetPath).Replace('\\', '/');
                if (!AssetDatabase.IsValidFolder(folder))
                    AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(folder).Replace('\\', '/'),
                                               System.IO.Path.GetFileName(folder));

                AssetDatabase.CreateAsset(data, AssetPath);
            }

            AssetDatabase.SaveAssets();

            // Mặt lưới cũ nướng bằng cửa sổ Navigation phải được dọn đi, nếu không hai lớp lưới
            // chồng lên nhau và việc hỏi đường có thể rơi trúng lớp cũ vốn đang sai.
            UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();

            provider.SetBakedData(data);
            EditorUtility.SetDirty(provider);
            EditorSceneManager.MarkSceneDirty(provider.gameObject.scene);

            var triangulation = NavMesh.CalculateTriangulation();
            Debug.Log(
                $"[NavMeshBaker] Xong. Thu {sources.Count} collider, ra {triangulation.vertices.Length} đỉnh / " +
                $"{triangulation.indices.Length / 3} tam giác. Bán kính thân dùng để bake = {provider.BuildSettings.agentRadius}.");
        }
    }
}

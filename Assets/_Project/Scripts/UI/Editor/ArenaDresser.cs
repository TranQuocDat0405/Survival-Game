using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Survival.EditorTools
{
    /// <summary>
    /// Rải cây cỏ đá trang trí quanh đấu trường.
    ///
    /// Dựng bằng code thay vì kéo thả từng cái vì hai lý do:
    ///   - Rải tay vài trăm vật thể vừa lâu vừa dễ để lọt một cái vào giữa sân,
    ///     và cái đó sẽ che khuất tầm nhìn đúng lúc đang đánh nhau.
    ///   - Chạy lại lệnh này là ra cùng một kết quả (dùng hạt ngẫu nhiên cố định),
    ///     nên chỉnh mật độ hay bán kính rồi chạy lại là xong, không phải dọn tay.
    ///
    /// LUẬT QUAN TRỌNG: mọi vật trang trí đều bị GỠ HẾT COLLIDER.
    /// AI của quái đi thẳng tới player chứ không tìm đường vòng (xem ghi chú về NavMesh
    /// trong README nộp bài). Nếu cây có va chạm, quái sẽ húc vào gốc cây rồi đứng đó rung.
    /// Đấu trường thông thoáng là điều kiện để kiểu AI này hoạt động đúng.
    ///
    /// Chạy qua menu: Survival > Dress Arena.
    /// </summary>
    public static class ArenaDresser
    {
        private const string NatureFolder = "Assets/_Project/Art/Environment/Nature";
        private const string ContainerName = "--- Decor ---";

        /// <summary>Bán kính vùng chơi để trống hoàn toàn. Không đặt gì bên trong.</summary>
        private const float PlayableRadius = 21f;

        /// <summary>Nửa cạnh sân, trùng với tường vô hình.</summary>
        private const float ArenaHalfExtent = 30f;

        /// <summary>
        /// Rừng trồng lan ra tới đâu, tính từ tâm.
        ///
        /// Phải lớn hơn hẳn nửa cạnh sân. Lý do: mặt nền là một hình VUÔNG, còn cây thì
        /// rải theo hình TRÒN. Nếu vành rừng dừng đúng ở mép sân thì bốn góc vuông lộ ra
        /// trống trơn, và người chơi nhìn thấy đường mép thẳng tắp của mặt nền —
        /// đọc ra ngay là bản đồ chưa làm xong.
        /// Trồng lan ra tận 46 thì rừng phủ kín cả bốn góc và che hẳn đường mép đó đi.
        /// </summary>
        private const float ForestOuterRadius = 46f;

        [MenuItem("Survival/Dress Arena")]
        public static void Dress()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

            var container = GameObject.Find(ContainerName);
            if (container != null)
                Object.DestroyImmediate(container);

            container = new GameObject(ContainerName);
            container.isStatic = true;

            // Hạt cố định để mỗi lần chạy ra cùng một khu rừng.
            Random.InitState(20260813);

            // Tiền tố phải khớp CHÍNH XÁC tên file. Trước đây lọc "PineTree_" trong khi
            // file thật tên là "Pine_", nên toàn bộ 5 cây thông bị bỏ sót mà không có
            // lỗi nào báo ra — vành rừng vì thế chỉ có mỗi một dáng cây.
            // TwistedTree tách riêng vì lá của nó màu ĐỎ (đo được texture là RGB 167,23,23),
            // trong khi CommonTree và Pine là xanh. Trộn đều ba loại thì một phần ba khu rừng
            // ngả đỏ và cả cảnh chuyển sang tông thu, lệch hẳn với mặt cỏ xanh.
            // Giữ nó lại làm điểm nhấn thưa thớt thì lại đẹp: vài đốm đỏ giữa rừng xanh.
            var bigTrees = LoadAll("CommonTree_", "Pine_");
            var accentTrees = LoadAll("TwistedTree_");
            var deadTrees = LoadAll("DeadTree_");
            var bushes = LoadAll("Bush_", "Fern_", "Plant_");
            var grass = LoadAll("Grass_", "Clover_");
            var rocks = LoadAll("Rock_", "Pebble_");
            var flowers = LoadAll("Flower_", "Mushroom_");
            var petals = LoadAll("Petal_");
            var pathStones = LoadAll("RockPath_");

            int placed = 0;

            // ------------------------------------------------------------------
            // TỈ LỆ Ở ĐÂY ĐƯỢC TÍNH THEO CHIỀU CAO NHÂN VẬT (khoảng 1.4 unit).
            //
            // Bộ Stylized Nature dựng theo tỉ lệ người thật: cây cao tới 27 unit,
            // cụm cỏ cao 2 unit. Đặt nguyên xi vào đây thì cỏ CAO HƠN cả nhân vật
            // và che mất con quái đang lao tới — người chơi ăn đòn mà không hiểu từ đâu.
            //
            // NGUYÊN TẮC BỐ CỤC (học từ map trong video tham chiếu):
            //   - Vùng đánh nhau ở giữa: chỉ có thứ THẤP hơn đầu gối nhân vật.
            //   - Càng ra xa tâm, cây cỏ càng cao và càng dày, tạo thành khung bao quanh.
            //   - Rải theo CỤM chứ không rải đều, vì thiên nhiên thật không mọc đều tăm tắp;
            //     rải đều cho ra cảm giác lốm đốm nhân tạo.
            //   - Vài tảng đá lớn làm MỐC để người chơi định vị được mình đang ở đâu trên sân.
            // ------------------------------------------------------------------

            // Vành rừng bao quanh, trồng thành hai lớp.
            //
            // Lớp trong dày đặc ngay sát mép vùng chơi: đây là "bức tường" mà mắt người chơi
            // đọc được, nói rõ tới đây là hết sân — quan trọng vì tường thật thì vô hình.
            // Lớp ngoài thưa dần và trải rất xa, để khi nhìn về phía chân trời thấy rừng
            // kéo dài chứ không phải một vành cây rồi hết.
            placed += ScatterRing(container.transform, bigTrees, count: 300, inner: PlayableRadius + 1.5f, outer: ArenaHalfExtent + 1f, scaleMin: 0.28f, scaleMax: 0.48f);
            placed += ScatterRing(container.transform, bigTrees, count: 320, inner: ArenaHalfExtent, outer: ForestOuterRadius, scaleMin: 0.30f, scaleMax: 0.55f);
            placed += ScatterRing(container.transform, deadTrees, count: 55, inner: PlayableRadius + 2f, outer: ForestOuterRadius - 6f, scaleMin: 0.22f, scaleMax: 0.38f);

            // Cây lá đỏ rải thưa làm điểm nhấn, khoảng một phần mười số cây xanh.
            placed += ScatterRing(container.transform, accentTrees, count: 62, inner: PlayableRadius + 2f, outer: ForestOuterRadius - 4f, scaleMin: 0.28f, scaleMax: 0.46f);

            // Bụi cây lấp chân rừng, để giữa gốc cây và mặt cỏ không bị hở một khoảng trống.
            placed += ScatterRing(container.transform, bushes, count: 240, inner: PlayableRadius - 0.5f, outer: ArenaHalfExtent + 8f, scaleMin: 0.28f, scaleMax: 0.62f);

            // ĐÁ LỚN LÀM MỐC. Đặt ở vành ngoài vùng chơi chứ không phải giữa sân:
            // chúng không có va chạm nên quái sẽ đi xuyên qua, mà chỗ ít đánh nhau nhất
            // là chỗ điều đó khó bị để ý nhất.
            placed += ScatterRing(container.transform, rocks, count: 26, inner: PlayableRadius - 5f, outer: PlayableRadius + 3f, scaleMin: 0.55f, scaleMax: 1.05f);

            // CON ĐƯỜNG LÁT ĐÁ vắt ngang sân.
            // Đây là thứ tạo khác biệt lớn nhất giữa "một bãi cỏ có rải cây" và "một map game".
            // Cây cỏ rải ngẫu nhiên dù dày tới đâu vẫn đọc ra là thiên nhiên vô chủ;
            // một con đường thì lập tức nói rằng chỗ này có người từng đi qua, có chủ ý sắp đặt.
            // Nó cũng cho người chơi một cái mốc để định vị mình đang ở đâu trên sân.
            placed += ScatterPath(container.transform, pathStones, PlayableRadius);

            // Trong vùng chơi rải theo CỤM: mỗi cụm là một nhúm cỏ hoa mọc quây quần,
            // giữa các cụm là khoảng đất trống. Mắt đọc ra ngay là cỏ mọc tự nhiên,
            // khác hẳn kiểu rải đều cho cảm giác lốm đốm như bụi bẩn trên màn hình.
            placed += ScatterClusters(container.transform, grass, clusters: 150, perCluster: 8, clusterRadius: 1.7f, areaRadius: PlayableRadius, scaleMin: 0.18f, scaleMax: 0.36f);
            placed += ScatterClusters(container.transform, flowers, clusters: 75, perCluster: 6, clusterRadius: 1.3f, areaRadius: PlayableRadius, scaleMin: 0.16f, scaleMax: 0.32f);
            placed += ScatterDisc(container.transform, rocks, count: 150, radius: PlayableRadius, scaleMin: 0.12f, scaleMax: 0.26f);

            // Cánh hoa rụng nằm sát đất, gần như phẳng. Chúng thêm chi tiết cho mặt sân
            // mà tuyệt đối không che được thứ gì, nên rải thoải mái.
            placed += ScatterClusters(container.transform, petals, clusters: 60, perCluster: 9, clusterRadius: 1.1f, areaRadius: PlayableRadius, scaleMin: 0.20f, scaleMax: 0.40f);

            StripColliders(container);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[ArenaDresser] Đã đặt {placed} vật trang trí, tất cả đều không có va chạm.");
        }

        private static List<GameObject> LoadAll(params string[] prefixes)
        {
            var result = new List<GameObject>();

            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { NatureFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);

                foreach (var prefix in prefixes)
                {
                    if (!name.StartsWith(prefix))
                        continue;

                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (asset != null)
                        result.Add(asset);
                    break;
                }
            }

            return result;
        }

        /// <summary>Rải trong một vành khuyên, dùng cho cây to bao quanh sân.</summary>
        private static int ScatterRing(Transform parent, List<GameObject> pool, int count, float inner, float outer, float scaleMin, float scaleMax)
        {
            if (pool.Count == 0)
                return 0;

            int placed = 0;
            for (int i = 0; i < count; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);

                // Lấy căn bậc hai để mật độ trải đều theo DIỆN TÍCH.
                // Bốc bán kính tuyến tính sẽ khiến vòng trong dày đặc còn vòng ngoài thưa thớt.
                float t = Mathf.Sqrt(Random.value);
                float radius = Mathf.Lerp(inner, outer, t);

                var position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                if (Place(parent, pool, position, scaleMin, scaleMax))
                    placed++;
            }
            return placed;
        }

        /// <summary>Rải trong một hình tròn đặc, dùng cho cỏ và sỏi trong vùng chơi.</summary>
        private static int ScatterDisc(Transform parent, List<GameObject> pool, int count, float radius, float scaleMin, float scaleMax)
        {
            if (pool.Count == 0)
                return 0;

            int placed = 0;
            for (int i = 0; i < count; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float r = radius * Mathf.Sqrt(Random.value);

                var position = new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);

                // Chừa trống ngay giữa sân, nơi player xuất hiện.
                if (position.magnitude < 3f)
                    continue;

                if (Place(parent, pool, position, scaleMin, scaleMax))
                    placed++;
            }
            return placed;
        }

        /// <summary>
        /// Lát một con đường đá cong vắt ngang sân.
        ///
        /// Đường đi theo một đường cong mềm chứ không phải đường thẳng: đường thẳng tắp
        /// nhìn ra ngay là do máy vẽ, còn đường cong nhẹ thì giống lối mòn người đi tạo thành.
        /// Đá được đặt hơi lún xuống dưới mặt đất một chút và lệch ngẫu nhiên sang hai bên,
        /// để mép đường không phải là một vệt đều tăm tắp.
        /// </summary>
        private static int ScatterPath(Transform parent, List<GameObject> pool, float areaRadius)
        {
            if (pool.Count == 0)
                return 0;

            int placed = 0;

            // Hai đầu đường nằm ở rìa sân, hướng bốc ngẫu nhiên nhưng gần như đối nhau,
            // để con đường thật sự vắt qua sân chứ không quẩn ở một góc.
            float startAngle = Random.Range(0f, Mathf.PI * 2f);
            float endAngle = startAngle + Mathf.PI + Random.Range(-0.6f, 0.6f);

            var start = new Vector3(Mathf.Cos(startAngle), 0f, Mathf.Sin(startAngle)) * (areaRadius + 3f);
            var end = new Vector3(Mathf.Cos(endAngle), 0f, Mathf.Sin(endAngle)) * (areaRadius + 3f);

            // Điểm điều khiển lệch khỏi tâm để đường võng sang một bên.
            var control = Vector3.Lerp(start, end, 0.5f)
                + new Vector3(Random.Range(-10f, 10f), 0f, Random.Range(-10f, 10f));

            const int steps = 120;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;

                // Đường cong bậc hai: nội suy hai lần cho ra một đường cong trơn.
                var a = Vector3.Lerp(start, control, t);
                var b = Vector3.Lerp(control, end, t);
                var point = Vector3.Lerp(a, b, t);

                if (point.magnitude > areaRadius + 2f)
                    continue;

                // Lệch ngang ngẫu nhiên để mép đường lởm chởm tự nhiên.
                var tangent = (b - a).normalized;
                var side = Vector3.Cross(Vector3.up, tangent);
                point += side * Random.Range(-0.7f, 0.7f);

                var prefab = pool[Random.Range(0, pool.Count)];
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                if (instance == null)
                    continue;

                // Lún nhẹ xuống đất để viên đá trông như đã nằm đó lâu ngày,
                // chứ không phải vừa được đặt lên trên mặt cỏ.
                instance.transform.position = point + Vector3.down * 0.04f;
                instance.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                instance.transform.localScale = Vector3.one * Random.Range(0.30f, 0.52f);
                instance.isStatic = true;
                placed++;
            }

            return placed;
        }

        /// <summary>
        /// Rải theo cụm: bốc một số tâm cụm rồi mọc vài cây quanh mỗi tâm.
        ///
        /// Vì sao không rải đều: cỏ ngoài đời mọc thành đám, chỗ dày chỗ trống.
        /// Rải đều khắp sân cho ra mật độ đồng nhất mọi nơi, nhìn giống hạt nhiễu
        /// phủ lên màn hình hơn là một thảm cỏ. Rải theo cụm tạo ra nhịp dày–thưa,
        /// và chính khoảng trống giữa các cụm mới làm nổi bật cụm cỏ lên.
        /// </summary>
        private static int ScatterClusters(Transform parent, List<GameObject> pool,
            int clusters, int perCluster, float clusterRadius, float areaRadius, float scaleMin, float scaleMax)
        {
            if (pool.Count == 0)
                return 0;

            int placed = 0;

            for (int c = 0; c < clusters; c++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float r = areaRadius * Mathf.Sqrt(Random.value);
                var center = new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);

                if (center.magnitude < 3.5f)
                    continue;   // chừa trống chỗ player xuất hiện

                // Số lượng mỗi cụm dao động để các cụm không giống hệt nhau.
                int amount = Random.Range(Mathf.Max(2, perCluster - 3), perCluster + 3);

                for (int i = 0; i < amount; i++)
                {
                    float a = Random.Range(0f, Mathf.PI * 2f);
                    float d = clusterRadius * Mathf.Sqrt(Random.value);
                    var position = center + new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d);

                    if (position.magnitude > areaRadius)
                        continue;

                    if (Place(parent, pool, position, scaleMin, scaleMax))
                        placed++;
                }
            }

            return placed;
        }

        private static bool Place(Transform parent, List<GameObject> pool, Vector3 position, float scaleMin, float scaleMax)
        {
            var prefab = pool[Random.Range(0, pool.Count)];
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (instance == null)
                return false;

            instance.transform.position = position;

            // Xoay ngẫu nhiên quanh trục đứng và đổi kích thước một chút, để cùng một model
            // lặp lại hàng chục lần mà mắt không nhận ra là đồ sao chép.
            instance.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            instance.transform.localScale = Vector3.one * Random.Range(scaleMin, scaleMax);
            instance.isStatic = true;

            return true;
        }

        /// <summary>
        /// Gỡ toàn bộ collider của phần trang trí.
        /// Đây không phải bước dọn dẹp cho gọn mà là YÊU CẦU để AI hoạt động đúng.
        /// </summary>
        private static void StripColliders(GameObject root)
        {
            var colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = colliders.Length - 1; i >= 0; i--)
                Object.DestroyImmediate(colliders[i]);
        }
    }
}

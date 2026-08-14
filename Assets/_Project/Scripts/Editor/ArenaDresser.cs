using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Survival.EditorTools
{
    /// <summary>
    /// Rải cây cỏ đá trang trí quanh đấu trường.
    ///
    /// Dựng bằng code thay vì kéo thả từng cái vì hai lý do:
    ///   - Rải tay vài nghìn vật thể vừa lâu vừa dễ để lọt một cái vào giữa sân,
    ///     và cái đó sẽ che khuất tầm nhìn đúng lúc đang đánh nhau.
    ///   - Chạy lại lệnh này là ra cùng một kết quả (dùng hạt ngẫu nhiên cố định),
    ///     nên chỉnh mật độ hay bán kính rồi chạy lại là xong, không phải dọn tay.
    ///
    /// ==================== LUẬT VA CHẠM ====================
    /// Vật trang trí chia làm HAI LOẠI, và đây là quyết định thiết kế quan trọng nhất file này:
    ///
    ///   VẬT ĐẶC   — cây, đá tảng, thân cây đổ, gốc cây.
    ///               Có va chạm, nằm ở layer Obstacle. Người chơi, quái và ĐẠN đều bị chặn.
    ///
    ///   VẬT LẶT VẶT — cỏ, hoa, cánh hoa rụng, sỏi, bụi thấp, đá lát đường.
    ///               Không va chạm. Đi xuyên qua được, và đó mới là điều người chơi mong đợi:
    ///               không ai muốn bị một nhánh cỏ chặn đường.
    ///
    /// Cây dùng CAPSULE BÓ SÁT THÂN chứ không bọc cả tán lá (xem <see cref="TrunkRadiusFactor"/>).
    /// Nếu bọc cả tán, người chơi sẽ bị chặn từ cách gốc cây mấy unit — cảm giác như đâm vào
    /// tường vô hình, rất khó chịu và không ai hiểu vì sao.
    ///
    /// Vì đã có vật cản nên quái BẮT BUỘC phải biết né. Phần đó nằm ở
    /// <c>EnemyActor.MoveTowardsTarget</c>: quái dò một tia hình cầu phía trước, gặp vật cản
    /// thì trượt vòng qua. Không có nó thì quái húc thẳng vào gốc cây rồi đứng rung tại chỗ.
    ///
    /// Mật độ cây ở vành rừng được tính để rừng ĐI XUYÊN QUA ĐƯỢC chứ không thành mê cung:
    /// khoảng 300 cây trải trên vành khuyên rộng ~1400 unit vuông, tức mỗi cây chiếm ~4.8 unit vuông,
    /// khoảng cách trung bình giữa hai gốc ~2.2 unit. Thân cây bán kính ~0.3 nên khe hở còn ~1.6 unit,
    /// rộng hơn người chơi (đường kính ~1.0). Nhờ vậy không bao giờ có túi cụt nhốt người chơi lại.
    ///
    /// Chạy qua menu: Survival > Dress Arena.
    /// </summary>
    public static class ArenaDresser
    {
        private const string NatureFolder = "Assets/_Project/Art/Environment/Nature";
        private const string ContainerName = "--- Decor ---";

        /// <summary>Layer Obstacle. Khớp với ma trận va chạm: chặn Player, Enemy và cả hai loại đạn.</summary>
        private const int ObstacleLayer = 14;

        /// <summary>Bán kính vùng chơi. Bên trong chỉ có vật thấp, và rất ít vật đặc.</summary>
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

        /// <summary>
        /// Ngoài khoảng này thì cây chỉ còn là hình nền, không gắn va chạm nữa.
        ///
        /// Tường vô hình nằm ở 30 nên người chơi không bao giờ ra tới đó được.
        /// Gắn va chạm cho vài trăm cây mà không ai chạm tới chỉ tốn bộ nhớ
        /// và tốn thời gian dựng cây va chạm lúc mở màn.
        /// </summary>
        private const float ColliderCutoffRadius = 32f;

        /// <summary>
        /// Bán kính thân cây, tính theo phần trăm bề ngang cả tán.
        ///
        /// Đây là con số quan trọng nhất về mặt cảm giác chơi. Cây cao 10 unit thì tán rộng
        /// khoảng 6 unit nhưng thân chỉ khoảng 0.8. Lấy 14% của nửa bề ngang cho ra
        /// đúng cỡ cái thân — người chơi lách được giữa hai gốc cây, và chỉ bị chặn
        /// khi thật sự đâm vào thân, đúng như những gì mắt nhìn thấy.
        /// </summary>
        private const float TrunkRadiusFactor = 0.14f;

        /// <summary>
        /// Đá và gỗ thì đặc gần hết khối, chỉ thu BỀ NGANG vào một chút cho đỡ vướng ở góc.
        ///
        /// Chỉ thu ngang, KHÔNG thu chiều cao. Thu cả chiều cao thì khối va chạm thấp hơn hình
        /// mà mắt nhìn thấy, và mũi tên sẽ bay lọt qua ngay bên trên một tảng đá trông rất đặc.
        /// Đo được lúc trước: đá cao 0.34 nhưng đỉnh khối va chạm chỉ tới 0.28.
        /// </summary>
        private const float SolidShrinkFactor = 0.82f;

        /// <summary>
        /// Độ cao tối thiểu của vật cản đặt trong vùng chơi.
        ///
        /// Đầu nỏ của nhân vật nằm ở độ cao 0.75, nên bất cứ thứ gì thấp hơn mức đó
        /// đều bị mũi tên bay vượt qua bên trên — đúng về mặt vật lý, nhưng người chơi
        /// nhìn vào chỉ thấy "bắn xuyên qua tảng đá" và cho là lỗi.
        ///
        /// Ép mọi vật cản trong sân cao hơn đầu nỏ thì luật trở nên nhất quán và dễ đoán:
        /// đã chặn được người thì cũng chặn được đạn. Vẫn thấp hơn nhiều so với nhân vật
        /// cao 1.4 nên không bao giờ che khuất được con quái đứng sau.
        /// </summary>
        private const float MinSolidHeightInPlayArea = 0.85f;

        /// <summary>
        /// Đếm số vật đã gắn va chạm, chỉ để in ra dòng tổng kết cuối cùng.
        ///
        /// Dùng biến tĩnh thay vì truyền tham số qua từng hàm: đây là công cụ Editor chạy
        /// một mạch trên một luồng, và nếu nhét thêm một tham số đếm vào mọi hàm rải
        /// thì danh sách tham số dài ra mà chẳng nói thêm được gì về việc rải cây.
        /// Được đặt lại về 0 ở đầu mỗi lần chạy.
        /// </summary>
        private static int _solidCount;

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

            // Xoá bộ nhớ đệm chiều cao: sau khi đổi thiết lập nhập model thì cỡ thật đổi theo,
            // giữ lại số cũ sẽ tính ra hệ số sai mà không hề có dấu hiệu gì.
            NaturalHeightCache.Clear();
            NaturalWidthCache.Clear();

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
            var pebbles = LoadAll("Rock_Medium", "Pebble_");
            // Hoa và nấm phải TÁCH RIÊNG dù cùng là chi tiết nhỏ trên mặt đất.
            // Vì bây giờ kích thước được đặt theo CHIỀU CAO, mọi thứ trong cùng một pool
            // sẽ cao bằng nhau. Mà cỡ tự nhiên của chúng chênh nhau năm lần
            // (khóm hoa 2.05–2.49, cây nấm 0.46–0.77), nên gộp chung sẽ cho ra
            // những cây nấm to ngang khóm hoa — nhìn ra ngay là sai tỉ lệ.
            var flowers = LoadAll("Flower_");
            var mushrooms = LoadAll("Mushroom_");
            var petals = LoadAll("Petal_");
            var pathStones = LoadAll("RockPath_");

            // Bộ vật thể bổ sung, lấy từ Ultimate Nature Pack.
            // Vùng giữa sân trước đây chỉ có cỏ và sỏi nên nhìn rất trống, mà lại không thể
            // trồng cây vào đó (cây cao sẽ che mất quái). Thân cây đổ, gốc cây và đá phủ rêu
            // giải đúng bài toán này: chúng THẤP nên không che tầm nhìn, nhưng lại ĐỦ TO
            // để mắt đọc ra là một vật thể thật, và đủ đặc để làm chỗ nấp.
            var logs = LoadAll("WoodLog", "TreeStump");
            var mossyRocks = LoadAll("Rock_Moss_");
            var berryBushes = LoadAll("BushBerries_");

            int placed = 0;
            _solidCount = 0;

            // ------------------------------------------------------------------
            // TỈ LỆ Ở ĐÂY ĐƯỢC TÍNH THEO CHIỀU CAO NHÂN VẬT (khoảng 1.4 unit).
            //
            // Bộ Stylized Nature dựng theo tỉ lệ người thật: cây cao tới 27 unit,
            // cụm cỏ cao 2 unit. Đặt nguyên xi vào đây thì cỏ CAO HƠN cả nhân vật
            // và che mất con quái đang lao tới — người chơi ăn đòn mà không hiểu từ đâu.
            //
            // NGUYÊN TẮC BỐ CỤC (học từ map trong video tham chiếu):
            //   - Vùng đánh nhau ở giữa: chỉ có thứ THẤP hơn thắt lưng nhân vật.
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
            placed += ScatterRing(container.transform, bigTrees, count: 300, inner: PlayableRadius + 1.5f, outer: ArenaHalfExtent + 1f, minHeight: 2.2f, maxHeight: 3.6f, solid: true);
            placed += ScatterRing(container.transform, bigTrees, count: 320, inner: ArenaHalfExtent, outer: ForestOuterRadius, minHeight: 2.6f, maxHeight: 4.5f, solid: true);
            placed += ScatterRing(container.transform, deadTrees, count: 55, inner: PlayableRadius + 2f, outer: ForestOuterRadius - 6f, minHeight: 1.8f, maxHeight: 3.0f, solid: true);

            // Cây lá đỏ rải thưa làm điểm nhấn, khoảng một phần mười số cây xanh.
            placed += ScatterRing(container.transform, accentTrees, count: 62, inner: PlayableRadius + 2f, outer: ForestOuterRadius - 4f, minHeight: 2.2f, maxHeight: 3.6f, solid: true);

            // Bụi cây lấp chân rừng, để giữa gốc cây và mặt cỏ không bị hở một khoảng trống.
            // KHÔNG có va chạm: bụi thấp thì người chơi lướt qua được, chặn lại sẽ rất bực.
            // Bắt đầu từ NGOÀI mép vùng chơi, vì chúng cao tới 1.0 — vượt ngưỡng 0.65 dành cho vùng đánh nhau.
            placed += ScatterRing(container.transform, bushes, count: 240, inner: PlayableRadius + 0.5f, outer: ArenaHalfExtent + 8f, minHeight: 0.5f, maxHeight: 1.0f, solid: false);

            // ĐÁ LỚN LÀM MỐC ở vành ngoài vùng chơi. Giờ chúng ĐẶC, nên vừa là mốc định vị
            // vừa là chỗ nấp thật sự khi bị quái vây.
            placed += ScatterLandmarks(container.transform, mossyRocks, count: 26, inner: PlayableRadius - 5f, outer: PlayableRadius + 3f, minSpacing: 4.5f, minHeight: MinSolidHeightInPlayArea, maxHeight: 1.15f);

            // ---------- VÙNG GIỮA SÂN ----------
            // Trước đây chỗ này gần như trống, chỉ có cỏ và sỏi vụn nên nhìn như một bãi cỏ
            // chưa làm xong. Bổ sung theo hai hướng: nhiều chi tiết nhỏ hơn để mặt đất có gì mà nhìn,
            // và vài vật thể ĐẶC cỡ vừa để sân có cấu trúc chứ không phải một mặt phẳng rỗng.

            // Thân cây đổ và gốc cây: thấp (~0.6 unit) nên không che quái, nhưng đủ to để làm mốc.
            // Giãn cách tối thiểu 6 unit để chúng không bao giờ chụm lại thành hàng rào nhốt người chơi.
            placed += ScatterLandmarks(container.transform, logs, count: 13, inner: 6.5f, outer: PlayableRadius - 2f, minSpacing: 6f, minHeight: MinSolidHeightInPlayArea, maxHeight: 1.05f);

            // Đá phủ rêu cỡ vừa, rải xen giữa để sân không chỉ có mỗi gỗ.
            placed += ScatterLandmarks(container.transform, mossyRocks, count: 14, inner: 6f, outer: PlayableRadius - 2f, minSpacing: 5.5f, minHeight: MinSolidHeightInPlayArea, maxHeight: 1.10f);

            // CON ĐƯỜNG LÁT ĐÁ vắt ngang sân.
            // Đây là thứ tạo khác biệt lớn nhất giữa "một bãi cỏ có rải cây" và "một map game".
            // Cây cỏ rải ngẫu nhiên dù dày tới đâu vẫn đọc ra là thiên nhiên vô chủ;
            // một con đường thì lập tức nói rằng chỗ này có người từng đi qua, có chủ ý sắp đặt.
            // Nó cũng cho người chơi một cái mốc để định vị mình đang ở đâu trên sân.
            placed += ScatterPath(container.transform, pathStones, PlayableRadius);

            // Trong vùng chơi rải theo CỤM: mỗi cụm là một nhúm cỏ hoa mọc quây quần,
            // giữa các cụm là khoảng đất trống. Mắt đọc ra ngay là cỏ mọc tự nhiên,
            // khác hẳn kiểu rải đều cho cảm giác lốm đốm như bụi bẩn trên màn hình.
            // MỌI CHIỀU CAO DƯỚI ĐÂY ĐỀU PHẢI ≤ 0.65 — ngưỡng của vùng đánh nhau.
            // Và chúng phải chênh nhau rõ rệt: cỏ cao hơn hoa, hoa cao hơn nấm, nấm cao hơn sỏi.
            // Nếu mọi thứ cao xấp xỉ nhau thì mặt sân thành một thảm đều tăm tắp,
            // mắt không còn phân biệt được cái gì với cái gì.
            placed += ScatterClusters(container.transform, grass, clusters: 240, perCluster: 8, clusterRadius: 1.7f, areaRadius: PlayableRadius, minHeight: 0.30f, maxHeight: 0.55f);
            placed += ScatterClusters(container.transform, flowers, clusters: 120, perCluster: 6, clusterRadius: 1.3f, areaRadius: PlayableRadius, minHeight: 0.28f, maxHeight: 0.48f);
            placed += ScatterClusters(container.transform, mushrooms, clusters: 45, perCluster: 4, clusterRadius: 0.9f, areaRadius: PlayableRadius, minHeight: 0.10f, maxHeight: 0.20f);

            // Bụi mọng thấp rải quanh các mốc, thêm chút màu khác cho đỡ đơn điệu một tông xanh.
            placed += ScatterClusters(container.transform, berryBushes, clusters: 34, perCluster: 3, clusterRadius: 1.6f, areaRadius: PlayableRadius - 1f, minHeight: 0.35f, maxHeight: 0.55f);

            // Sỏi vụn phải THẬT nhỏ. Đây là hạt rải trên mặt đất, không phải đá tảng —
            // để cao 0.2 thì cả sân đầy những cục đá xám cỡ bằng cái mũ.
            placed += ScatterDisc(container.transform, pebbles, count: 240, radius: PlayableRadius, minHeight: 0.02f, maxHeight: 0.05f);

            // Cánh hoa rụng nằm sát đất, gần như phẳng. Chúng thêm chi tiết cho mặt sân
            // mà tuyệt đối không che được thứ gì, nên rải thoải mái.
            placed += ScatterClusters(container.transform, petals, clusters: 95, perCluster: 9, clusterRadius: 1.1f, areaRadius: PlayableRadius, minHeight: 0.05f, maxHeight: 0.11f);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[ArenaDresser] Đã đặt {placed} vật trang trí, trong đó {_solidCount} vật có va chạm (layer Obstacle).");
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
        private static int ScatterRing(Transform parent, List<GameObject> pool, int count, float inner, float outer,
            float minHeight, float maxHeight, bool solid)
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

                var instance = Place(parent, pool, position, minHeight, maxHeight);
                if (instance == null)
                    continue;

                placed++;

                // Chỉ gắn va chạm cho cây nằm trong tầm với của người chơi.
                if (solid && radius <= ColliderCutoffRadius && AddTrunkBlocker(instance))
                    _solidCount++;
            }
            return placed;
        }

        /// <summary>Rải trong một hình tròn đặc, dùng cho cỏ và sỏi trong vùng chơi.</summary>
        private static int ScatterDisc(Transform parent, List<GameObject> pool, int count, float radius, float minHeight, float maxHeight)
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

                if (Place(parent, pool, position, minHeight, maxHeight) != null)
                    placed++;
            }
            return placed;
        }

        /// <summary>
        /// Đặt các vật thể ĐẶC làm mốc, có giãn cách tối thiểu giữa chúng.
        ///
        /// Giãn cách là bắt buộc chứ không phải cho đẹp: hai tảng đá đặt sát nhau tạo thành
        /// một khe hẹp, và quái đuổi theo người chơi sẽ kẹt ở đó. Ép khoảng cách tối thiểu
        /// lớn hơn hẳn bề ngang một con quái thì không bao giờ sinh ra khe như vậy.
        /// </summary>
        private static int ScatterLandmarks(Transform parent, List<GameObject> pool, int count,
            float inner, float outer, float minSpacing, float minHeight, float maxHeight)
        {
            if (pool.Count == 0)
                return 0;

            // Chặn ngay tại đây thay vì tin vào con số bên gọi truyền xuống.
            // Mọi vật đi qua hàm này đều là vật ĐẶC, nên đều phải cao hơn đầu nỏ —
            // nếu không thì nó chặn được người mà không chặn được đạn, và luật trở nên khó đoán.
            minHeight = Mathf.Max(minHeight, MinSolidHeightInPlayArea);
            maxHeight = Mathf.Max(maxHeight, minHeight);

            var taken = new List<Vector3>(count);
            int placed = 0;
            int guard = count * 40;   // trần số lần thử, tránh lặp vô hạn khi chỗ đã chật

            while (placed < count && guard-- > 0)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float t = Mathf.Sqrt(Random.value);
                float radius = Mathf.Lerp(inner, outer, t);
                var position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

                bool tooClose = false;
                for (int i = 0; i < taken.Count; i++)
                {
                    if ((taken[i] - position).sqrMagnitude < minSpacing * minSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose)
                    continue;

                var instance = Place(parent, pool, position, minHeight, maxHeight);
                if (instance == null)
                    continue;

                taken.Add(position);
                placed++;

                if (AddSolidBlocker(instance))
                    _solidCount++;
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

            // Điểm điều khiển chỉ lệch NHẸ khỏi tâm.
            //
            // Trước đây lệch tới 10 unit, và hậu quả là con đường võng hẳn ra sát rìa sân:
            // người chơi đứng giữa sân đánh nhau thì không bao giờ nhìn thấy nó.
            // Mà chỗ người chơi đứng nhiều nhất mới đúng là chỗ cần một cái mốc để định vị.
            // Lệch ít thì đường vẫn cong tự nhiên nhưng luôn đi vòng qua gần tâm.
            var control = Vector3.Lerp(start, end, 0.5f)
                + new Vector3(Random.Range(-4f, 4f), 0f, Random.Range(-4f, 4f));

            const int steps = 58;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;

                // Đường cong bậc hai: nội suy hai lần cho ra một đường cong trơn.
                var a = Vector3.Lerp(start, control, t);
                var b = Vector3.Lerp(control, end, t);
                var point = Vector3.Lerp(a, b, t);

                if (point.magnitude > areaRadius + 2f)
                    continue;

                var tangent = (b - a).normalized;
                var side = Vector3.Cross(Vector3.up, tangent);

                // Lát vài viên NGANG mặt đường chứ không phải một viên.
                // Một hàng đá đơn nhìn ra chỉ là chuỗi sỏi rơi vãi; phải rộng cỡ hai bước chân
                // thì mắt mới đọc thành lối mòn có người đi.
                //
                // Nhưng vị trí ngang KHÔNG được bốc ngẫu nhiên trong cả dải.
                // Bốc ngẫu nhiên thì hai viên rơi trúng gần nhau và chồng lên nhau,
                // nhìn kỹ ra ngay là một đống đá lộn xộn chứ không phải mặt đường được lát.
                // Thay vào đó chia dải thành các RÃNH CÁCH ĐỀU, mỗi rãnh một viên,
                // rồi rung nhẹ quanh rãnh — vẫn tự nhiên mà không bao giờ đè nhau.
                const int lanes = 3;
                for (int k = 0; k < lanes; k++)
                {
                    // Bỏ bớt ngẫu nhiên để mặt đường thưa và mòn, chứ không lát kín như sân gạch.
                    if (Random.value < 0.28f)
                        continue;

                    float laneOffset = Mathf.Lerp(-0.85f, 0.85f, k / (float)(lanes - 1));
                    var spot = point + side * (laneOffset + Random.Range(-0.10f, 0.10f));

                    var prefab = pool[Random.Range(0, pool.Count)];
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                    if (instance == null)
                        continue;

                    instance.transform.position = spot;
                    instance.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    // Rãnh cách nhau 0.85 nên bề ngang phải nhỏ hơn mức đó thì mới không đè nhau.
                    ScaleToWidth(instance, prefab, 0.40f, 0.72f);
                    AlignToGround(instance, sink: 0f);

                    // Lún nhẹ xuống đất để viên đá trông như đã nằm đó lâu ngày,
                    // chứ không phải vừa được đặt lên trên mặt cỏ.
                    // Phải hạ SAU khi canh đáy chạm đất, nếu không sẽ bị canh ngược lên lại.
                    instance.transform.position += Vector3.down * 0.03f;

                    instance.isStatic = true;
                    StripColliders(instance);
                    placed++;
                }
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
            int clusters, int perCluster, float clusterRadius, float areaRadius, float minHeight, float maxHeight)
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

                    if (Place(parent, pool, position, minHeight, maxHeight) != null)
                        placed++;
                }
            }

            return placed;
        }

        /// <summary>
        /// Dựng một vật trang trí. Mặc định KHÔNG có va chạm — bên gọi tự quyết định
        /// có gắn thêm hay không. Cách này an toàn hơn mặc định ngược lại: quên gắn va chạm
        /// thì chỉ là đi xuyên qua một cục đá, còn quên gỡ va chạm thì cả bãi cỏ thành tường.
        /// </summary>
        private static GameObject Place(Transform parent, List<GameObject> pool, Vector3 position, float minHeight, float maxHeight)
        {
            var prefab = pool[Random.Range(0, pool.Count)];
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (instance == null)
                return null;

            instance.transform.position = position;

            // Xoay ngẫu nhiên quanh trục đứng và đổi kích thước một chút, để cùng một model
            // lặp lại hàng chục lần mà mắt không nhận ra là đồ sao chép.
            instance.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            ScaleToHeight(instance, prefab, minHeight, maxHeight);
            AlignToGround(instance);
            instance.isStatic = true;

            // Model gốc có thể mang sẵn collider bọc cả khối. Gỡ sạch rồi tự dựng lại
            // theo đúng hình dạng mình muốn, thay vì phải đoán xem bộ asset đã đặt gì trong đó.
            StripColliders(instance);

            return instance;
        }

        /// <summary>
        /// Đổi kích thước bằng cách nói rõ MUỐN CAO BAO NHIÊU UNIT, không phải bằng hệ số nhân.
        ///
        /// ĐÂY LÀ CHỖ ĐÃ SAI HAI LẦN LIỀN, và cách sửa mới là cách duy nhất chặn được tận gốc.
        ///
        /// Lần một: hàm này gán thẳng <c>localScale = 0.55</c>. Cách đó chỉ đúng khi model có
        /// scale gốc bằng 1. Bộ Stylized Nature đúng như vậy nên chạy tốt suốt. Nhưng bộ
        /// Ultimate Nature xuất mesh theo đơn vị xăng-ti-mét: mesh chỉ to 0.005 unit và node gốc
        /// mang sẵn scale 100 để bù lại. Gán đè 0.55 lên đó đã XOÁ MẤT con số 100, và mấy tảng đá
        /// bị thu về một phần trăm — to bằng hạt đậu. Cả cụm vật cản coi như không tồn tại,
        /// mà không hề có lỗi nào báo ra.
        ///
        /// Lần hai: chuyển sang NHÂN vào scale gốc thì hết bị thu nhỏ, nhưng vẫn sai cỡ,
        /// vì hệ số nhân chỉ có nghĩa khi mọi model trong cùng một nhóm to xấp xỉ nhau.
        /// Thực tế đo được thì không: nhóm <c>Plant</c> có model cao 0.25 và model cao 3.76,
        /// chênh nhau mười lăm lần. Cùng một hệ số cho ra cái thì tí xíu, cái thì che kín màn hình.
        ///
        /// Cách làm hiện tại: đo chiều cao thật của model rồi tính ngược ra hệ số cần thiết.
        /// Nhờ vậy luật "trong vùng chơi không có gì cao quá 0.65 unit" được bảo đảm BẰNG CẤU TRÚC —
        /// muốn vi phạm cũng không được — thay vì phải tin rằng hệ số đã chọn là đúng.
        /// Thêm một bộ asset lạ với quy ước đơn vị bất kỳ cũng không cần chỉnh gì.
        /// </summary>
        private static void ScaleToHeight(GameObject instance, GameObject prefab, float minHeight, float maxHeight)
        {
            float natural = GetNaturalHeight(prefab);

            Vector3 nativeScale = prefab.transform.localScale;
            if (nativeScale.sqrMagnitude < 0.000001f)
                nativeScale = Vector3.one;

            // Model không có mesh (hoặc mesh phẳng tuyệt đối) thì không suy ra được hệ số,
            // giữ nguyên cỡ gốc còn hơn là chia cho số không.
            float factor = natural > 0.0001f
                ? Random.Range(minHeight, maxHeight) / natural
                : 1f;

            instance.transform.localScale = nativeScale * factor;
        }

        /// <summary>
        /// Hạ vật xuống cho ĐÁY CHẠM MẶT ĐẤT.
        ///
        /// Không phải model nào cũng đặt điểm gốc dưới chân. Bộ Stylized Nature thì có,
        /// nên đặt ở y = 0 là cây đứng đúng trên mặt cỏ. Nhưng bộ Ultimate Nature lại đặt điểm gốc
        /// ở GIỮA KHỐI, nên cùng cách đặt đó khiến tảng đá chôn mất một nửa xuống đất:
        /// đo được đáy nằm ở y = −0.58 trong khi đỉnh chỉ tới y = 0.41.
        ///
        /// Hệ quả không chỉ là xấu. Đầu nỏ nằm ở độ cao 0.75, nên một tảng đá "cao 0.99"
        /// mà thực tế chỉ nhô lên 0.41 sẽ để mũi tên bay lọt qua bên trên — nhìn ra đúng như
        /// bắn xuyên qua đá. Chính chỗ này đã làm phép thử chặn đạn thất bại.
        ///
        /// Lún nhẹ một chút là cố ý: để hở đúng bằng 0 thì mặt đáy và mặt đất trùng khít nhau,
        /// và card đồ hoạ không quyết được nên vẽ mặt nào trước, sinh ra vệt nhấp nháy.
        /// </summary>
        private static void AlignToGround(GameObject instance, float sink = 0.02f)
        {
            var renderers = instance.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0)
                return;

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            instance.transform.position += Vector3.up * (-bounds.min.y - sink);
        }

        /// <summary>
        /// Đổi kích thước theo BỀ NGANG mong muốn thay vì chiều cao.
        ///
        /// Dùng cho đá lát đường. Với một phiến đá nằm bẹt thì chiều cao gần như vô nghĩa —
        /// mọi viên đều dày cỡ 0.05 tới 0.10 — còn thứ quyết định nó chiếm bao nhiêu mặt đường
        /// lại là bề ngang. Co giãn theo chiều cao ở đây cho ra kết quả loạn hẳn:
        /// đo được trong pool có viên rộng 0.43 và có phiến rộng tới 2.27, chênh nhau năm lần,
        /// nên phiến to đè lên cả hai viên bên cạnh dù rãnh đã cách đều.
        /// </summary>
        private static void ScaleToWidth(GameObject instance, GameObject prefab, float minWidth, float maxWidth)
        {
            float natural = GetNaturalWidth(prefab);

            Vector3 nativeScale = prefab.transform.localScale;
            if (nativeScale.sqrMagnitude < 0.000001f)
                nativeScale = Vector3.one;

            float factor = natural > 0.0001f
                ? Random.Range(minWidth, maxWidth) / natural
                : 1f;

            instance.transform.localScale = nativeScale * factor;
        }

        /// <summary>Bề ngang thật của model khi đặt vào cảnh, lấy cạnh dài hơn trong hai chiều ngang.</summary>
        private static float GetNaturalWidth(GameObject prefab)
        {
            if (NaturalWidthCache.TryGetValue(prefab, out float cached))
                return cached;

            float width = 0f;

            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null)
                    continue;

                float chainX = 1f, chainZ = 1f;
                var node = filter.transform;
                while (node != null)
                {
                    chainX *= node.localScale.x;
                    chainZ *= node.localScale.z;
                    node = node.parent;
                }

                var size = filter.sharedMesh.bounds.size;
                width = Mathf.Max(width, Mathf.Max(size.x * chainX, size.z * chainZ));
            }

            NaturalWidthCache[prefab] = width;
            return width;
        }

        private static readonly Dictionary<GameObject, float> NaturalWidthCache = new Dictionary<GameObject, float>();

        /// <summary>
        /// Chiều cao thật của model khi đặt vào cảnh, tính bằng unit.
        ///
        /// Phải tự nhân dồn scale theo cả chuỗi cha–con chứ KHÔNG dùng <c>Renderer.bounds</c>:
        /// với một prefab chưa được đặt vào cảnh, <c>bounds</c> trả về số không đáng tin.
        /// Chính chỗ này đã làm tôi đọc ra WoodLog cao 0.75 trong khi cỡ thật của nó là 2.67.
        ///
        /// Kết quả được nhớ lại vì hàm rải gọi tới nó vài nghìn lần trong một lượt dựng,
        /// mà số model gốc thì chỉ vài chục.
        /// </summary>
        private static float GetNaturalHeight(GameObject prefab)
        {
            if (NaturalHeightCache.TryGetValue(prefab, out float cached))
                return cached;

            float height = 0f;

            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null)
                    continue;

                float chain = 1f;
                var node = filter.transform;
                while (node != null)
                {
                    chain *= node.localScale.y;
                    node = node.parent;
                }

                height = Mathf.Max(height, filter.sharedMesh.bounds.size.y * chain);
            }

            NaturalHeightCache[prefab] = height;
            return height;
        }

        private static readonly Dictionary<GameObject, float> NaturalHeightCache = new Dictionary<GameObject, float>();

        /// <summary>Va chạm bó sát THÂN cây: một cột đứng mảnh xuyên giữa tán lá.</summary>
        private static bool AddTrunkBlocker(GameObject instance)
        {
            if (!TryComputeLocalBounds(instance, out Bounds local))
                return false;

            var capsule = instance.AddComponent<CapsuleCollider>();
            capsule.direction = 1;   // trục Y
            capsule.center = local.center;
            capsule.radius = Mathf.Max(local.extents.x, local.extents.z) * TrunkRadiusFactor;
            capsule.height = local.size.y;

            instance.layer = ObstacleLayer;
            return true;
        }

        /// <summary>Va chạm bọc gần hết khối, dùng cho đá tảng, thân cây đổ, gốc cây.</summary>
        private static bool AddSolidBlocker(GameObject instance)
        {
            if (!TryComputeLocalBounds(instance, out Bounds local))
                return false;

            var box = instance.AddComponent<BoxCollider>();
            box.center = local.center;
            box.size = new Vector3(
                local.size.x * SolidShrinkFactor,
                local.size.y,                       // giữ nguyên chiều cao, xem ghi chú ở SolidShrinkFactor
                local.size.z * SolidShrinkFactor);

            instance.layer = ObstacleLayer;
            return true;
        }

        /// <summary>
        /// Tính khung bao của model TRONG HỆ TRỤC CỦA CHÍNH NÓ.
        ///
        /// Không dùng <c>Renderer.bounds</c> được: cái đó là khung bao thẳng trục THẾ GIỚI,
        /// mà mỗi vật lại bị xoay ngẫu nhiên quanh trục đứng. Một thân cây đổ dài 2 unit
        /// xoay 45 độ sẽ cho ra khung bao thế giới rộng tới 1.4 unit theo cả hai chiều —
        /// gắn va chạm theo số đó thì vật cản phình to hơn hẳn hình mà mắt nhìn thấy.
        /// Nên ở đây ta lấy 8 đỉnh khung bao của từng mesh rồi đổi về hệ trục của vật.
        /// </summary>
        private static bool TryComputeLocalBounds(GameObject instance, out Bounds local)
        {
            local = default;
            bool any = false;

            var toLocal = instance.transform.worldToLocalMatrix;

            foreach (var filter in instance.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh;
                if (mesh == null)
                    continue;

                var meshBounds = mesh.bounds;
                var matrix = toLocal * filter.transform.localToWorldMatrix;

                for (int corner = 0; corner < 8; corner++)
                {
                    var point = new Vector3(
                        (corner & 1) == 0 ? meshBounds.min.x : meshBounds.max.x,
                        (corner & 2) == 0 ? meshBounds.min.y : meshBounds.max.y,
                        (corner & 4) == 0 ? meshBounds.min.z : meshBounds.max.z);

                    var localPoint = matrix.MultiplyPoint3x4(point);

                    if (!any)
                    {
                        local = new Bounds(localPoint, Vector3.zero);
                        any = true;
                    }
                    else
                    {
                        local.Encapsulate(localPoint);
                    }
                }
            }

            return any && local.size.y > 0.0001f;
        }

        private static void StripColliders(GameObject root)
        {
            var colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = colliders.Length - 1; i >= 0; i--)
                Object.DestroyImmediate(colliders[i]);
        }
    }
}

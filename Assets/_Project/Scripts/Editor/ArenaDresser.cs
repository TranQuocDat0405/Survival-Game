using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Survival.EditorTools
{
    /// <summary>
    /// Dựng toàn bộ khung cảnh của màn chơi: cây cỏ, đá, con đường, vành đá rìa map và tường vô hình.
    ///
    /// Dựng bằng code thay vì kéo thả từng cái vì hai lý do:
    ///   - Rải tay vài nghìn vật thể vừa lâu vừa dễ để lọt một cái vào giữa sân,
    ///     và cái đó sẽ che khuất tầm nhìn đúng lúc đang đánh nhau.
    ///   - Chạy lại lệnh này là ra cùng một kết quả (dùng hạt ngẫu nhiên cố định),
    ///     nên chỉnh mật độ rồi chạy lại là xong, không phải dọn tay.
    ///
    /// ==================== BỐ CỤC MÀN CHƠI ====================
    /// Sân là HÌNH VUÔNG chứ không phải hình tròn. Lý do là về cảm giác chơi chứ không phải kỹ thuật:
    /// với sân vuông, người chơi nhìn là đoán được mình còn bao xa tới rìa; còn sân tròn thì
    /// đi hướng nào cũng thấy rìa lùi ra xa như nhau, không có mốc nào để định vị.
    /// Nhưng cây và đá vẫn được rải LƯỢN NGẪU NHIÊN dọc theo cạnh, để không lộ ra đường thẳng do máy vẽ.
    ///
    ///   ±16          sân trống — chỗ đánh nhau chính, chỉ có vật thấp
    ///   ±16.5 ⟶ ±33  rừng ĐI VÀO ĐƯỢC — cây đặc, len lỏi qua được, dùng để né tránh
    ///   ±33.2 ⟶ ±35  VÀNH ĐÁ rìa map — đá tảng cao hơn đầu người, xếp sát nhau
    ///   ±35          tường vô hình
    ///   ±35 ⟶ ±50    phông nền — thông cao, không va chạm, chỉ để che chân trời
    ///
    /// ==================== LUẬT VA CHẠM ====================
    ///   VẬT ĐẶC     — cây, đá tảng, thân cây đổ, gốc cây.
    ///                 Có va chạm, nằm ở layer Obstacle. Người chơi, quái và ĐẠN đều bị chặn.
    ///   VẬT LẶT VẶT — cỏ, hoa, cánh hoa rụng, sỏi, bụi thấp, đá lát đường.
    ///                 Không va chạm — không ai muốn bị một nhánh cỏ chặn đường.
    ///
    /// Cây dùng CAPSULE BÓ SÁT THÂN chứ không bọc cả tán lá (xem <see cref="TrunkRadiusFactor"/>).
    /// Nếu bọc cả tán, người chơi bị chặn từ cách gốc mấy unit — cảm giác như đâm vào tường vô hình.
    ///
    /// Mật độ rừng được tính để ĐI LỌT QUA ĐƯỢC chứ không thành mê cung: khe hở trung bình
    /// giữa hai gốc khoảng 2 unit, rộng gấp đôi người chơi. Vì rừng đi vào được và cây thì đặc,
    /// quái BẮT BUỘC phải biết tìm đường vòng — phần đó nằm ở AI của quái, không phải ở đây.
    ///
    /// Chạy qua menu: Survival > Dress Arena.
    /// </summary>
    public static class ArenaDresser
    {
        private const string NatureFolder = "Assets/_Project/Art/Environment/Nature";
        private const string ContainerName = "--- Decor ---";
        private const string BoundaryName = "--- Boundary ---";

        /// <summary>Layer Wall — tường vô hình chặn player và quái.</summary>
        private const int WallLayer = 13;

        /// <summary>Layer Obstacle — cây, đá tảng. Chặn cả người lẫn đạn.</summary>
        private const int ObstacleLayer = 14;

        /// <summary>Nửa cạnh sân trống ở giữa. Trong vùng này chỉ có vật thấp hơn đầu gối.</summary>
        private const float OpenFieldExtent = 16f;

        /// <summary>Rừng bắt đầu từ đây, chừa một khe nhỏ so với sân trống cho đỡ đột ngột.</summary>
        private const float ForestInnerExtent = 16.5f;

        /// <summary>Rừng đi vào được kéo dài tới đây.</summary>
        private const float ForestOuterExtent = 33f;

        /// <summary>Vành đá rìa map bắt đầu từ đây.</summary>
        private const float RockRimInnerExtent = 33.2f;

        /// <summary>
        /// Nửa cạnh sân — mép ngoài cùng người chơi đi tới được. Sân vuông 70x70.
        ///
        /// Tường vô hình đặt đúng ở đây, tức là NGAY SAU vành đá. Người chơi đi tới thấy
        /// gờ đá cao hơn đầu chắn ngang thì hiểu ngay là hết đường — chứ không phải
        /// bị một thân cây chặn lại rồi tưởng game lỗi.
        /// </summary>
        private const float ArenaExtent = 35f;

        /// <summary>
        /// Phông nền trồng ra tới đâu.
        ///
        /// Đo bằng chính camera chứ không đoán: camera nghiêng 52 độ, đứng ở mép sân vẫn
        /// nhìn thêm được khoảng 19 unit về phía trước. Đứng ở GÓC sân là trường hợp xa nhất,
        /// nên phông phải phủ tới khoảng 50 thì chân trời mới kín ở mọi hướng.
        /// </summary>
        private const float BackdropExtent = 50f;

        /// <summary>
        /// Bán kính thân cây, tính theo phần trăm bề ngang cả tán.
        ///
        /// Đây là con số quan trọng nhất về cảm giác chơi. Cây cao 10 unit thì tán rộng
        /// khoảng 6 unit nhưng thân chỉ khoảng 0.8. Lấy 14% của nửa bề ngang cho ra
        /// đúng cỡ cái thân — người chơi lách được giữa hai gốc, và chỉ bị chặn khi
        /// thật sự đâm vào thân, đúng như những gì mắt nhìn thấy.
        /// </summary>
        private const float TrunkRadiusFactor = 0.14f;

        /// <summary>
        /// Đá và gỗ thì đặc gần hết khối, chỉ thu BỀ NGANG vào một chút cho đỡ vướng ở góc.
        ///
        /// Chỉ thu ngang, KHÔNG thu chiều cao. Thu cả chiều cao thì khối va chạm thấp hơn hình
        /// mà mắt nhìn thấy, và mũi tên sẽ bay lọt qua ngay bên trên một tảng đá trông rất đặc.
        /// </summary>
        private const float SolidShrinkFactor = 0.82f;

        /// <summary>
        /// Độ cao tối thiểu của vật cản đặt trong sân trống.
        ///
        /// Đầu nỏ của nhân vật nằm ở độ cao 0.75, nên bất cứ thứ gì thấp hơn mức đó
        /// đều bị mũi tên bay vượt qua bên trên — đúng về vật lý, nhưng người chơi nhìn vào
        /// chỉ thấy "bắn xuyên qua tảng đá" và cho là lỗi. Ép mọi vật cản cao hơn đầu nỏ
        /// thì luật trở nên nhất quán: đã chặn được người thì cũng chặn được đạn.
        /// </summary>
        private const float MinSolidHeightInPlayArea = 0.85f;

        /// <summary>
        /// Đếm số vật đã gắn va chạm, chỉ để in ra dòng tổng kết.
        /// Dùng biến tĩnh thay vì truyền tham số qua từng hàm rải, vì đây là công cụ Editor
        /// chạy một mạch trên một luồng. Được đặt lại về 0 ở đầu mỗi lần chạy.
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

            Random.InitState(20260813);
            NaturalHeightCache.Clear();
            NaturalWidthCache.Clear();
            _solidCount = 0;

            // Tiền tố phải khớp CHÍNH XÁC tên file. Trước đây lọc "PineTree_" trong khi
            // file thật tên là "Pine_", nên toàn bộ 5 cây thông bị bỏ sót mà không có
            // lỗi nào báo ra — vành rừng vì thế chỉ có mỗi một dáng cây.
            // TwistedTree tách riêng vì lá của nó màu ĐỎ (đo được texture là RGB 167,23,23);
            // trộn đều thì một phần ba khu rừng ngả đỏ và cả cảnh chuyển sang tông thu.
            var bigTrees = LoadAll("CommonTree_", "Pine_");
            var accentTrees = LoadAll("TwistedTree_");
            var deadTrees = LoadAll("DeadTree_");
            var bushes = LoadAll("Bush_Common", "Fern_", "Plant_");
            var grass = LoadAll("Grass_", "Clover_");
            var pebbles = LoadAll("Rock_Medium", "Pebble_");
            var flowers = LoadAll("Flower_");
            var mushrooms = LoadAll("Mushroom_");
            var petals = LoadAll("Petal_");

            // Bộ RockPath_ là một BỘ KIT LÁT ĐƯỜNG chứ không phải mười viên đá rời.
            // Tác giả cố tình làm phiến to (Wide 2.27, Thin 1.44) để lát nền, và viên nhỏ
            // (Small 0.43-0.57) để chèn khe.
            //
            // CHỈ DÙNG ĐÁ LÁT TRÒN. Hai thứ đã thử rồi loại bỏ, ghi lại để khỏi thử lại:
            //
            // 1. Đá lát VUÔNG. Trộn 50/50 thì cả con đường thành sân gạch, vì đá vuông nặng ký
            //    hơn hẳn về thị giác — nó đọc ra ngay là gạch do người cắt. Giảm xuống 20% thì
            //    hết cảm giác sân gạch, nhưng khi đó mỗi viên vuông lại đứng lạc lõng giữa đá
            //    cuội tròn. Không có liều lượng nào vừa cả: nhiều thì nhân tạo, ít thì lạc quẻ.
            //
            // 2. Sỏi tự nhiên và đá phủ rêu. Thêm vào với ý nối mặt đường với thảm cỏ, nhưng
            //    chúng SẪM MÀU hơn hẳn đá lát, nên trên nền đá nhạt lại hoá thành những đốm
            //    lốm đốm trông như vết bẩn chứ không phải chuyển tiếp mượt.
            //
            // Mép đường vẫn tan dần vào cỏ, nhưng bằng cách GIẢM MẬT ĐỘ chứ không đổi loại đá —
            // cách đó vừa mượt hơn vừa giữ mặt đường thống nhất một chất liệu.
            var pathSlabs = LoadAll("RockPath_Round_Wide", "RockPath_Round_Thin");
            var pathPebbles = LoadAll("RockPath_Round_Small");
            var logs = LoadAll("WoodLog", "TreeStump");
            var mossyRocks = LoadAll("Rock_Moss_");
            var berryBushes = LoadAll("BushBerries_");

            int placed = 0;

            // ---------- VÀNH ĐÁ RÌA MAP ----------
            // Dựng TRƯỚC mọi thứ khác để nó luôn được ưu tiên chỗ, và để dễ kiểm tra bằng mắt.
            placed += BuildRockRim(container.transform, mossyRocks);

            // ---------- PHÔNG NỀN NGOÀI TƯỜNG ----------
            // Cây rất cao, KHÔNG va chạm (người chơi không bao giờ ra tới đó nên gắn chỉ tốn máy).
            // Cao 5-9 để che kín chân trời: camera nghiêng nên đứng ở mép sân vẫn nhìn rất xa,
            // rừng thấp thì mắt nhìn vượt qua ngọn cây và thấy khoảng trống phía sau.
            placed += ScatterBand(container.transform, bigTrees, count: 620,
                inner: ArenaExtent + 0.5f, outer: BackdropExtent, minHeight: 5f, maxHeight: 9f, solid: false);

            // ---------- RỪNG ĐI VÀO ĐƯỢC ----------
            // Cây ĐẶC. Mật độ tính để lách qua được: 520 cây trên vùng ~3400 unit vuông
            // là mỗi cây chiếm ~6.5 unit vuông, khoảng cách trung bình giữa hai gốc ~2.5 unit.
            // Thân cây bán kính ~0.3 nên khe hở còn ~1.9 — rộng gần gấp đôi người chơi.
            placed += ScatterBand(container.transform, bigTrees, count: 520,
                inner: ForestInnerExtent, outer: ForestOuterExtent, minHeight: 3.0f, maxHeight: 5.5f, solid: true);
            placed += ScatterBand(container.transform, deadTrees, count: 70,
                inner: ForestInnerExtent + 2f, outer: ForestOuterExtent, minHeight: 2.0f, maxHeight: 3.4f, solid: true);
            placed += ScatterBand(container.transform, accentTrees, count: 70,
                inner: ForestInnerExtent + 2f, outer: ForestOuterExtent, minHeight: 2.6f, maxHeight: 4.2f, solid: true);

            // Bụi thấp lấp chân rừng cho đỡ hở khoảng trống giữa gốc cây và mặt cỏ. Không va chạm.
            placed += ScatterBand(container.transform, bushes, count: 420,
                inner: ForestInnerExtent, outer: ForestOuterExtent, minHeight: 0.5f, maxHeight: 1.1f, solid: false);
            placed += ScatterBand(container.transform, grass, count: 300,
                inner: ForestInnerExtent, outer: ForestOuterExtent, minHeight: 0.3f, maxHeight: 0.6f, solid: false);

            // ---------- SÂN TRỐNG Ở GIỮA ----------
            // Vật thấp hơn 0.65 để không bao giờ che mất con quái đang lao tới.
            // Chiều cao các nhóm phải chênh nhau rõ: cỏ cao hơn hoa, hoa cao hơn nấm, nấm cao hơn sỏi.
            // Nếu mọi thứ cao xấp xỉ nhau thì mặt sân thành một thảm đều tăm tắp, mắt không đọc ra gì.
            placed += ScatterClusters(container.transform, grass, clusters: 260, perCluster: 8, clusterRadius: 1.7f, areaExtent: OpenFieldExtent, minHeight: 0.30f, maxHeight: 0.55f);
            placed += ScatterClusters(container.transform, flowers, clusters: 130, perCluster: 6, clusterRadius: 1.3f, areaExtent: OpenFieldExtent, minHeight: 0.28f, maxHeight: 0.48f);
            placed += ScatterClusters(container.transform, mushrooms, clusters: 50, perCluster: 4, clusterRadius: 0.9f, areaExtent: OpenFieldExtent, minHeight: 0.10f, maxHeight: 0.20f);
            placed += ScatterClusters(container.transform, berryBushes, clusters: 36, perCluster: 3, clusterRadius: 1.6f, areaExtent: OpenFieldExtent - 1f, minHeight: 0.35f, maxHeight: 0.55f);
            placed += ScatterClusters(container.transform, petals, clusters: 100, perCluster: 9, clusterRadius: 1.1f, areaExtent: OpenFieldExtent, minHeight: 0.05f, maxHeight: 0.11f);

            // Sỏi vụn phải THẬT nhỏ — đây là hạt rải trên mặt đất, không phải đá tảng.
            placed += ScatterDisc(container.transform, pebbles, count: 260, areaExtent: OpenFieldExtent, minHeight: 0.02f, maxHeight: 0.05f);

            // Mốc định vị trong sân: thân cây đổ, gốc cây, đá phủ rêu. Thấp nên không che quái,
            // nhưng đủ to để mắt đọc ra là vật thể thật và đủ đặc để làm chỗ nấp.
            // Giãn cách tối thiểu dùng CHUNG một danh sách cho cả hai lượt gọi, nếu tách riêng
            // thì một khúc gỗ và một tảng đá vẫn có thể nằm sát nhau thành khe hẹp nhốt quái.
            var landmarkSpots = new List<Vector3>();
            placed += ScatterLandmarks(container.transform, logs, landmarkSpots, count: 16, inner: 6.5f, outer: OpenFieldExtent - 1f, minSpacing: 6f, minHeight: MinSolidHeightInPlayArea, maxHeight: 1.05f);
            placed += ScatterLandmarks(container.transform, mossyRocks, landmarkSpots, count: 18, inner: 6f, outer: OpenFieldExtent - 1f, minSpacing: 6f, minHeight: MinSolidHeightInPlayArea, maxHeight: 1.10f);

            // CON ĐƯỜNG LÁT ĐÁ vắt ngang sân — thứ tạo khác biệt lớn nhất giữa "bãi cỏ có rải đá"
            // và "một map game". Cây cỏ ngẫu nhiên dù dày tới đâu vẫn đọc ra là thiên nhiên vô chủ;
            // một con đường thì lập tức nói rằng chỗ này có người từng đi qua.
            placed += ScatterPath(container.transform, pathSlabs, pathPebbles, grass, flowers);

            int wallPieces = BuildBoundary();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[ArenaDresser] {placed} vật trang trí ({_solidCount} có va chạm), sân vuông {ArenaExtent * 2f}x{ArenaExtent * 2f}, {wallPieces} tấm tường.");
        }

        // ============================================================ RÌA MAP

        /// <summary>
        /// Xếp một gờ đá chạy vòng quanh rìa sân.
        ///
        /// VÌ SAO CẦN: người chơi phải HIỂU NGAY là tới đây hết đường. Nếu chỉ có tường vô hình
        /// giữa rừng thì cảm giác là bị kẹt vào một thân cây và game lỗi, chứ không phải "hết map".
        /// Đá tảng cao hơn đầu nhân vật, xếp liền một dải, thì mắt đọc ra ngay là bức vách
        /// không thể leo qua — không cần một dòng chữ hướng dẫn nào.
        ///
        /// Đá được xếp theo cạnh hình vuông nhưng vị trí có RUNG NGẪU NHIÊN vào/ra,
        /// để gờ đá trông như địa hình tự nhiên chứ không phải một bức tường gạch thẳng tắp.
        /// </summary>
        private static int BuildRockRim(Transform parent, List<GameObject> pool)
        {
            if (pool.Count == 0)
                return 0;

            int placed = 0;

            // Bước đặt phải NHỎ HƠN bề ngang viên đá thì dải mới liền mạch, không hở lỗ.
            const float step = 1.5f;
            float mid = (RockRimInnerExtent + ArenaExtent) * 0.5f;

            for (int side = 0; side < 4; side++)
            {
                for (float t = -ArenaExtent; t <= ArenaExtent; t += step)
                {
                    // Rung vào/ra để mép gờ đá lởm chởm tự nhiên.
                    float depth = mid + Random.Range(-0.7f, 0.7f);
                    float along = t + Random.Range(-0.4f, 0.4f);

                    Vector3 position;
                    switch (side)
                    {
                        case 0: position = new Vector3(along, 0f, depth); break;
                        case 1: position = new Vector3(along, 0f, -depth); break;
                        case 2: position = new Vector3(depth, 0f, along); break;
                        default: position = new Vector3(-depth, 0f, along); break;
                    }

                    // Cao hơn hẳn nhân vật (1.4) để đọc ra là leo không qua.
                    // Model đá gốc chỉ cao 0.47 tới 1.25 nên phải nới trần phóng to lên 5 lần,
                    // nếu giữ trần mặc định 1.4 thì gờ đá chỉ cao khoảng 0.6 — thấp hơn cả đầu gối.
                    var instance = Place(parent, pool, position, 2.5f, 4.0f, maxFactor: 5f);
                    if (instance == null)
                        continue;

                    placed++;
                    if (AddSolidBlocker(instance))
                        _solidCount++;
                }
            }

            return placed;
        }

        /// <summary>
        /// Dựng tường vô hình hình VUÔNG, đặt ngay sau vành đá.
        ///
        /// Tường chỉ là lớp bảo hiểm: thứ người chơi thật sự nhìn thấy và hiểu là gờ đá.
        /// Nhưng vẫn phải có, vì giữa hai tảng đá luôn còn khe nhỏ mà nhân vật có thể lách qua.
        /// </summary>
        private static int BuildBoundary()
        {
            var old = GameObject.Find(BoundaryName);
            if (old != null)
                Object.DestroyImmediate(old);

            // Dọn MỌI thứ còn nằm ở layer Wall, bất kể nó nằm ở đâu trong cây phân cấp.
            // Trước đây chỗ này chỉ quét ở gốc, và bốn bức tường cũ lại nằm dưới "--- Arena ---"
            // nên thoát hết. Tệ hơn nữa là chúng CÓ MeshRenderer: người chơi nhìn thấy một dải
            // xám chắn ngang chân trời và tưởng bản đồ bị hụt.
            var stale = new List<GameObject>();
            foreach (var go in Object.FindObjectsOfType<GameObject>())
            {
                if (go.layer != WallLayer)
                    continue;
                if (go.transform.root != null && go.transform.root.name == BoundaryName)
                    continue;
                stale.Add(go);
            }
            foreach (var go in stale)
                Object.DestroyImmediate(go);

            var root = new GameObject(BoundaryName);
            root.isStatic = true;

            var offsets = new[]
            {
                new Vector3(0f, 1.5f, ArenaExtent),
                new Vector3(0f, 1.5f, -ArenaExtent),
                new Vector3(ArenaExtent, 1.5f, 0f),
                new Vector3(-ArenaExtent, 1.5f, 0f),
            };
            var sizes = new[]
            {
                new Vector3(ArenaExtent * 2f + 2f, 4f, 1f),
                new Vector3(ArenaExtent * 2f + 2f, 4f, 1f),
                new Vector3(1f, 4f, ArenaExtent * 2f + 2f),
                new Vector3(1f, 4f, ArenaExtent * 2f + 2f),
            };
            var names = new[] { "Wall_N", "Wall_S", "Wall_E", "Wall_W" };

            for (int i = 0; i < 4; i++)
            {
                var piece = new GameObject(names[i]);
                piece.transform.SetParent(root.transform, false);
                piece.transform.position = offsets[i];
                piece.layer = WallLayer;
                piece.isStatic = true;
                piece.AddComponent<BoxCollider>().size = sizes[i];
            }

            return 4;
        }

        // ============================================================ CÁC KIỂU RẢI

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

        /// <summary>
        /// Rải trong một DẢI VUÔNG: nằm ngoài hình vuông <paramref name="inner"/>
        /// nhưng trong hình vuông <paramref name="outer"/>.
        ///
        /// Dùng phép bốc rồi loại: bốc một điểm bất kỳ trong hình vuông ngoài, rơi vào giữa
        /// thì bỏ và bốc lại. Cách này ngắn hơn hẳn so với tính toán từng cạnh một,
        /// và quan trọng hơn là nó rải ĐỀU THEO DIỆN TÍCH — chia theo cạnh thì bốn góc
        /// sẽ dày hơn hẳn bốn cạnh vì góc có diện tích lớn hơn.
        /// </summary>
        private static int ScatterBand(Transform parent, List<GameObject> pool, int count,
            float inner, float outer, float minHeight, float maxHeight, bool solid)
        {
            if (pool.Count == 0)
                return 0;

            int placed = 0;
            int guard = count * 12;

            while (placed < count && guard-- > 0)
            {
                var position = new Vector3(Random.Range(-outer, outer), 0f, Random.Range(-outer, outer));

                // Nằm lọt trong hình vuông trong thì bỏ, bốc lại.
                if (Mathf.Abs(position.x) < inner && Mathf.Abs(position.z) < inner)
                    continue;

                var instance = Place(parent, pool, position, minHeight, maxHeight);
                if (instance == null)
                    continue;

                placed++;
                if (solid && AddTrunkBlocker(instance))
                    _solidCount++;
            }

            return placed;
        }

        /// <summary>Rải đều trong hình vuông sân trống, dùng cho sỏi vụn.</summary>
        private static int ScatterDisc(Transform parent, List<GameObject> pool, int count, float areaExtent, float minHeight, float maxHeight)
        {
            if (pool.Count == 0)
                return 0;

            int placed = 0;
            for (int i = 0; i < count; i++)
            {
                var position = new Vector3(Random.Range(-areaExtent, areaExtent), 0f, Random.Range(-areaExtent, areaExtent));

                // Chừa trống ngay giữa sân, nơi player xuất hiện.
                if (position.magnitude < 3f)
                    continue;

                if (Place(parent, pool, position, minHeight, maxHeight) != null)
                    placed++;
            }
            return placed;
        }

        /// <summary>
        /// Rải theo cụm: bốc một số tâm cụm rồi mọc vài cây quanh mỗi tâm.
        ///
        /// Vì sao không rải đều: cỏ ngoài đời mọc thành đám, chỗ dày chỗ trống.
        /// Rải đều khắp sân cho ra mật độ đồng nhất mọi nơi, nhìn giống hạt nhiễu phủ lên màn hình
        /// hơn là một thảm cỏ. Rải theo cụm tạo ra nhịp dày–thưa, và chính khoảng trống
        /// giữa các cụm mới làm nổi bật cụm cỏ lên.
        /// </summary>
        private static int ScatterClusters(Transform parent, List<GameObject> pool,
            int clusters, int perCluster, float clusterRadius, float areaExtent, float minHeight, float maxHeight)
        {
            if (pool.Count == 0)
                return 0;

            int placed = 0;

            for (int c = 0; c < clusters; c++)
            {
                var center = new Vector3(Random.Range(-areaExtent, areaExtent), 0f, Random.Range(-areaExtent, areaExtent));
                if (center.magnitude < 3.5f)
                    continue;   // chừa trống chỗ player xuất hiện

                int amount = Random.Range(Mathf.Max(2, perCluster - 3), perCluster + 3);

                for (int i = 0; i < amount; i++)
                {
                    float a = Random.Range(0f, Mathf.PI * 2f);
                    float d = clusterRadius * Mathf.Sqrt(Random.value);
                    var position = center + new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d);

                    if (Mathf.Abs(position.x) > areaExtent || Mathf.Abs(position.z) > areaExtent)
                        continue;

                    if (Place(parent, pool, position, minHeight, maxHeight) != null)
                        placed++;
                }
            }

            return placed;
        }

        /// <summary>
        /// Đặt các vật thể ĐẶC làm mốc, có giãn cách tối thiểu giữa chúng.
        ///
        /// Giãn cách là bắt buộc chứ không phải cho đẹp: hai tảng đá đặt sát nhau tạo thành
        /// một khe hẹp, và quái đuổi theo người chơi sẽ kẹt ở đó.
        ///
        /// Danh sách vị trí đã dùng được truyền TỪ NGOÀI VÀO để nhiều lượt gọi khác nhau
        /// (khúc gỗ, tảng đá) cùng chia sẻ một danh sách. Nếu mỗi lượt tự giữ danh sách riêng
        /// thì một khúc gỗ và một tảng đá vẫn có thể rơi sát nhau — đúng cái khe hẹp cần tránh.
        /// </summary>
        private static int ScatterLandmarks(Transform parent, List<GameObject> pool, List<Vector3> taken,
            int count, float inner, float outer, float minSpacing, float minHeight, float maxHeight)
        {
            if (pool.Count == 0)
                return 0;

            // Chặn ngay tại đây thay vì tin vào con số bên gọi truyền xuống: mọi vật đi qua hàm này
            // đều ĐẶC nên đều phải cao hơn đầu nỏ, nếu không thì chặn được người mà không chặn được đạn.
            minHeight = Mathf.Max(minHeight, MinSolidHeightInPlayArea);
            maxHeight = Mathf.Max(maxHeight, minHeight);

            int placed = 0;
            int guard = count * 60;

            while (placed < count && guard-- > 0)
            {
                var position = new Vector3(Random.Range(-outer, outer), 0f, Random.Range(-outer, outer));
                if (position.magnitude < inner)
                    continue;

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

                // Nới trần phóng to lên 2.5: mốc trong sân bắt buộc phải cao hơn đầu nỏ 0.75,
                // mà đá phủ rêu nhỏ nhất chỉ cao 0.47 nên trần mặc định 1.4 không với tới nổi.
                var instance = Place(parent, pool, position, minHeight, maxHeight, maxFactor: 2.5f);
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
        /// Lát một con đường đá vắt ngang sân trống, ghép từ BA LỚP.
        ///
        /// Đây là cách bộ RockPath_ được thiết kế để dùng, và cũng là cách các game stylized
        /// dựng đường lát: không phải rải một cỡ đá đều nhau, mà chồng nhiều cỡ lên nhau.
        ///
        ///   LỚP 1 — phiến to lát nền, XOAY THEO HƯỚNG ĐƯỜNG. Đây là thứ tạo ra hình con đường.
        ///           Chồng mép lên nhau thoải mái: mặt đường thật vốn có phiến gối lên phiến.
        ///   LỚP 2 — đá nhỏ chèn vào khe giữa các phiến, xoay tự do. Lớp này lấp kín lỗ hổng
        ///           và phá vỡ sự đều đặn của lớp dưới.
        ///   LỚP 3 — đá vụn rải ra ngoài hai mép, mật độ GIẢM DẦN theo khoảng cách tới tim đường.
        ///           Đây là thứ quan trọng nhất về thẩm mỹ: mép đường tan dần vào cỏ thì con đường
        ///           trông như đã nằm đó lâu năm, thay vì vừa được dán vào.
        ///
        /// Cộng thêm vài nhúm cỏ và hoa mọc chen trong khe đá, để mặt đường không bị phẳng lì
        /// và để nối con đường với thảm cỏ xung quanh.
        /// </summary>
        private static int ScatterPath(Transform parent, List<GameObject> slabs, List<GameObject> pebbles,
            List<GameObject> grass, List<GameObject> flowers)
        {
            if (slabs.Count == 0 || pebbles.Count == 0)
                return 0;

            float startAngle = Random.Range(0f, Mathf.PI * 2f);
            float endAngle = startAngle + Mathf.PI + Random.Range(-0.6f, 0.6f);

            float reach = OpenFieldExtent + 3f;
            var start = new Vector3(Mathf.Cos(startAngle), 0f, Mathf.Sin(startAngle)) * reach;
            var end = new Vector3(Mathf.Cos(endAngle), 0f, Mathf.Sin(endAngle)) * reach;

            // Điểm điều khiển chỉ lệch nhẹ, để đường luôn đi vòng qua GẦN TÂM sân —
            // chỗ người chơi đứng nhiều nhất mới là chỗ cần một cái mốc để định vị.
            var control = Vector3.Lerp(start, end, 0.5f)
                + new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));

            const float halfWidth = 1.3f;   // mặt đường rộng 2.6 unit, cỡ hai rưỡi thân người
            int placed = 0;

            // ---------- LỚP 1: phiến to lát nền ----------
            foreach (var s in SampleCurve(start, control, end, 0.85f))
            {
                for (int k = 0; k < 3; k++)
                {
                    float lateral = Mathf.Lerp(-halfWidth * 0.62f, halfWidth * 0.62f, k / 2f) + Random.Range(-0.28f, 0.28f);
                    var spot = s.Point + s.Side * lateral;
                    if (Mathf.Abs(spot.x) > OpenFieldExtent || Mathf.Abs(spot.z) > OpenFieldExtent)
                        continue;

                    // Xoay THEO HƯỚNG ĐƯỜNG, chỉ lệch chút cho tự nhiên. Chính chỗ này làm mắt
                    // đọc ra là phiến đá được xếp xuôi theo lối đi, chứ không phải đá rơi vãi.
                    float yaw = Quaternion.LookRotation(s.Tangent, Vector3.up).eulerAngles.y + Random.Range(-22f, 22f);
                    if (PlacePathPiece(parent, slabs, spot, yaw, 0.95f, 1.55f))
                        placed++;
                }
            }

            // ---------- LỚP 2: đá nhỏ chèn khe ----------
            foreach (var s in SampleCurve(start, control, end, 0.42f))
            {
                for (int k = 0; k < 2; k++)
                {
                    var spot = s.Point + s.Side * Random.Range(-halfWidth, halfWidth);
                    if (Mathf.Abs(spot.x) > OpenFieldExtent || Mathf.Abs(spot.z) > OpenFieldExtent)
                        continue;

                    if (PlacePathPiece(parent, pebbles, spot, Random.Range(0f, 360f), 0.34f, 0.62f))
                        placed++;
                }
            }

            // ---------- LỚP 3: mép đường tan dần vào cỏ ----------
            foreach (var s in SampleCurve(start, control, end, 0.55f))
            {
                for (int k = 0; k < 2; k++)
                {
                    // Càng ra xa tim đường thì khả năng có đá càng thấp.
                    float t = Random.value;
                    if (Random.value > 1f - t)
                        continue;

                    float lateral = halfWidth + t * 1.5f;
                    var spot = s.Point + s.Side * (Random.value < 0.5f ? -lateral : lateral);
                    if (Mathf.Abs(spot.x) > OpenFieldExtent || Mathf.Abs(spot.z) > OpenFieldExtent)
                        continue;

                    if (PlacePathPiece(parent, pebbles, spot, Random.Range(0f, 360f), 0.22f, 0.45f))
                        placed++;
                }
            }

            // ---------- Cỏ và hoa mọc chen trong khe đá ----------
            foreach (var s in SampleCurve(start, control, end, 1.1f))
            {
                if (Random.value < 0.45f)
                    continue;

                var pool = (Random.value < 0.65f || flowers.Count == 0) ? grass : flowers;
                if (pool.Count == 0)
                    continue;

                var spot = s.Point + s.Side * Random.Range(-halfWidth - 0.3f, halfWidth + 0.3f);
                if (Mathf.Abs(spot.x) > OpenFieldExtent || Mathf.Abs(spot.z) > OpenFieldExtent)
                    continue;

                if (Place(parent, pool, spot, 0.14f, 0.30f) != null)
                    placed++;
            }

            return placed;
        }

        /// <summary>Đặt một viên đá lát: co giãn theo BỀ NGANG, xoay theo góc cho sẵn, lún nhẹ xuống đất.</summary>
        private static bool PlacePathPiece(Transform parent, List<GameObject> pool, Vector3 spot, float yaw, float minWidth, float maxWidth)
        {
            var prefab = pool[Random.Range(0, pool.Count)];
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (instance == null)
                return false;

            instance.transform.position = spot;
            instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // Co giãn theo BỀ NGANG chứ không theo chiều cao: với một phiến đá nằm bẹt thì
            // chiều cao gần như vô nghĩa (mọi viên đều dày 0.05-0.10), còn thứ quyết định
            // nó chiếm bao nhiêu mặt đường lại là bề ngang.
            ScaleToWidth(instance, prefab, minWidth, maxWidth);
            AlignToGround(instance, sink: 0f);

            // Mỗi viên lún một độ sâu hơi khác nhau. Chúng CHỒNG MÉP lên nhau, nên nếu cùng nằm
            // đúng một cao độ thì hai mặt phẳng trùng nhau và card đồ hoạ không quyết được
            // vẽ mặt nào trước, sinh ra vệt nhấp nháy mỗi khi camera nhúc nhích.
            instance.transform.position += Vector3.down * Random.Range(0.015f, 0.06f);

            instance.isStatic = true;
            StripColliders(instance);
            return true;
        }

        /// <summary>Một điểm đã lấy mẫu trên đường cong: vị trí, hướng đi tới, và hướng ngang.</summary>
        private struct CurveSample
        {
            public Vector3 Point;
            public Vector3 Tangent;
            public Vector3 Side;
        }

        /// <summary>
        /// Lấy mẫu đường cong bậc hai theo khoảng cách ĐỀU NHAU dọc theo đường.
        ///
        /// Không chia đều tham số t được: đường cong bậc hai chạy nhanh ở đoạn giữa và chậm ở
        /// hai đầu, nên chia đều t sẽ cho ra đá dày cộm ở hai đầu và thưa ở giữa. Phải đi dọc
        /// đường cong và cộng dồn quãng đường thật, cứ đủ một bước thì lấy một mẫu.
        /// </summary>
        private static IEnumerable<CurveSample> SampleCurve(Vector3 start, Vector3 control, Vector3 end, float spacing)
        {
            const int fine = 600;
            Vector3 previous = start;
            float accumulated = spacing;   // phát mẫu ngay từ điểm đầu

            for (int i = 1; i <= fine; i++)
            {
                float t = i / (float)fine;
                var a = Vector3.Lerp(start, control, t);
                var b = Vector3.Lerp(control, end, t);
                var point = Vector3.Lerp(a, b, t);

                var delta = point - previous;
                accumulated += delta.magnitude;
                var tangent = delta.sqrMagnitude > 0.000001f ? delta.normalized : Vector3.forward;
                previous = point;

                if (accumulated < spacing)
                    continue;

                accumulated = 0f;
                yield return new CurveSample
                {
                    Point = point,
                    Tangent = tangent,
                    Side = Vector3.Cross(Vector3.up, tangent),
                };
            }
        }

        // ============================================================ ĐẶT MỘT VẬT

        /// <summary>
        /// Dựng một vật trang trí. Mặc định KHÔNG có va chạm — bên gọi tự quyết định có gắn thêm không.
        /// Cách này an toàn hơn mặc định ngược lại: quên gắn va chạm thì chỉ là đi xuyên qua một cục đá,
        /// còn quên gỡ va chạm thì cả bãi cỏ thành tường.
        /// </summary>
        private static GameObject Place(Transform parent, List<GameObject> pool, Vector3 position, float minHeight, float maxHeight, float maxFactor = 1.4f)
        {
            var prefab = pool[Random.Range(0, pool.Count)];
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (instance == null)
                return null;

            instance.transform.position = position;

            // Xoay ngẫu nhiên quanh trục đứng và đổi kích thước một chút, để cùng một model
            // lặp lại hàng trăm lần mà mắt không nhận ra là đồ sao chép.
            instance.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            ScaleToHeight(instance, prefab, minHeight, maxHeight, maxFactor);
            AlignToGround(instance);
            instance.isStatic = true;

            // Model gốc có thể mang sẵn collider bọc cả khối. Gỡ sạch rồi tự dựng lại
            // theo đúng hình dạng mình muốn, thay vì phải đoán bộ asset đã đặt gì trong đó.
            StripColliders(instance);

            return instance;
        }

        /// <summary>
        /// Đổi kích thước bằng cách nói rõ MUỐN CAO BAO NHIÊU UNIT, không phải bằng hệ số nhân.
        ///
        /// Đây là chỗ đã sai hai lần liền.
        /// Lần một: gán thẳng <c>localScale = 0.55</c>. Chỉ đúng khi model có scale gốc bằng 1.
        /// Bộ Ultimate Nature xuất mesh theo xăng-ti-mét (mesh 0.005, node gốc mang scale 100),
        /// nên gán đè đã xoá mất số 100 và mấy tảng đá bị thu về một phần trăm — to bằng hạt đậu.
        /// Lần hai: chuyển sang NHÂN vào scale gốc thì hết bị thu nhỏ, nhưng hệ số nhân chỉ có nghĩa
        /// khi mọi model trong cùng nhóm to xấp xỉ nhau — mà nhóm Plant có model cao 0.25 và
        /// model cao 3.76, chênh mười lăm lần.
        ///
        /// Cách hiện tại: đo chiều cao thật rồi tính ngược ra hệ số. Nhờ vậy luật
        /// "trong sân không có gì cao quá 0.65" được bảo đảm BẰNG CẤU TRÚC, và thêm bộ asset lạ
        /// với quy ước đơn vị bất kỳ cũng không cần chỉnh gì.
        /// </summary>
        /// <param name="maxFactor">
        /// Trần phóng to. Mặc định 1.4 để chặn tai nạn (xem <see cref="ClampFactor"/>),
        /// nhưng có những chỗ CỐ Ý phóng model lên nhiều lần — ví dụ đá tảng rìa map phải cao
        /// hơn đầu người trong khi model gốc chỉ cao nửa mét. Ở những chỗ đó bên gọi tự nới trần,
        /// và vì phải ghi rõ ra nên không thể vô tình phóng to nhầm.
        /// </param>
        private static void ScaleToHeight(GameObject instance, GameObject prefab, float minHeight, float maxHeight, float maxFactor = 1.4f)
        {
            float natural = GetNaturalHeight(prefab);

            Vector3 nativeScale = prefab.transform.localScale;
            if (nativeScale.sqrMagnitude < 0.000001f)
                nativeScale = Vector3.one;

            float factor = natural > 0.0001f
                ? Random.Range(minHeight, maxHeight) / natural
                : 1f;

            instance.transform.localScale = nativeScale * ClampFactor(factor, maxFactor);
        }

        /// <summary>
        /// Đổi kích thước theo BỀ NGANG. Dùng cho đá lát đường.
        ///
        /// Với một phiến đá nằm bẹt thì chiều cao gần như vô nghĩa — mọi viên đều dày cỡ 0.05
        /// tới 0.10 — còn thứ quyết định nó chiếm bao nhiêu mặt đường lại là bề ngang.
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

            instance.transform.localScale = nativeScale * ClampFactor(factor);
        }

        /// <summary>
        /// Chặn không cho phóng một model to hơn hẳn cỡ tự nhiên của nó.
        ///
        /// Đặt kích thước theo chiều cao chỉ đúng khi mọi model trong nhóm có tỉ lệ gần giống nhau.
        /// Nhóm Plant có model cao 0.25 và model cao 3.76; ép cả nhóm cao 1.0 thì con 0.25
        /// bị nhân bốn lần — cao thì đúng, nhưng BỀ NGANG cũng nở gấp bốn và cho ra một bụi cây
        /// to như cái xe nằm chình ình giữa cảnh.
        /// </summary>
        private static float ClampFactor(float factor, float maxFactor = 1.4f) => Mathf.Clamp(factor, 0.08f, maxFactor);

        /// <summary>
        /// Hạ vật xuống cho ĐÁY CHẠM MẶT ĐẤT.
        ///
        /// Không phải model nào cũng đặt điểm gốc dưới chân. Bộ Stylized Nature thì có,
        /// nhưng bộ Ultimate Nature đặt điểm gốc ở GIỮA KHỐI, nên đặt ở y = 0 là tảng đá
        /// chôn mất một nửa xuống đất: đo được đáy nằm ở −0.58 trong khi đỉnh chỉ tới 0.41.
        /// Hệ quả không chỉ là xấu — đầu nỏ ở độ cao 0.75 nên mũi tên bay lọt qua bên trên
        /// một tảng đá trông rất đặc.
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
        /// Chiều cao thật của model khi đặt vào cảnh, tính bằng unit.
        ///
        /// Phải tự nhân dồn scale theo cả chuỗi cha–con chứ KHÔNG dùng <c>Renderer.bounds</c>:
        /// với một prefab chưa đặt vào cảnh, <c>bounds</c> trả về số không đáng tin — chính chỗ này
        /// đã làm tôi đọc ra WoodLog cao 0.75 trong khi cỡ thật của nó là 2.67.
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

        /// <summary>Bề ngang thật của model, lấy cạnh dài hơn trong hai chiều ngang.</summary>
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

        private static readonly Dictionary<GameObject, float> NaturalHeightCache = new Dictionary<GameObject, float>();
        private static readonly Dictionary<GameObject, float> NaturalWidthCache = new Dictionary<GameObject, float>();

        // ============================================================ VA CHẠM

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
        /// mà mỗi vật lại bị xoay ngẫu nhiên quanh trục đứng. Một thân cây đổ dài 2 unit xoay 45 độ
        /// sẽ cho ra khung bao thế giới rộng tới 1.4 unit theo cả hai chiều — gắn va chạm theo số đó
        /// thì vật cản phình to hơn hẳn hình mà mắt nhìn thấy.
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

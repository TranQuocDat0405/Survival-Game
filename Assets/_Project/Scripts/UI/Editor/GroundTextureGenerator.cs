using System.IO;
using UnityEditor;
using UnityEngine;

namespace Survival.EditorTools
{
    /// <summary>
    /// Sinh texture mặt cỏ lát liền mạch.
    ///
    /// VÌ SAO TỰ SINH:
    /// File <c>Grass.png</c> đi kèm bộ Stylized Nature KHÔNG phải texture mặt đất —
    /// nó là một BẢNG MÀU dạng dải, để các model low-poly tra màu qua toạ độ UV.
    /// Lát nó ra làm nền thì cho ra những vệt sọc chạy ngang màn hình.
    ///
    /// Texture này sinh bằng nhiễu nhiều tầng và được LÀM CHO LIỀN MẠCH:
    /// giá trị nhiễu lấy trên một vòng tròn trong không gian bốn chiều, nên mép trái nối
    /// khít mép phải và mép trên nối khít mép dưới, không thấy đường ghép khi lát.
    ///
    /// Chạy qua menu: Survival > Generate Ground Texture.
    /// </summary>
    public static class GroundTextureGenerator
    {
        private const string OutputPath = "Assets/_Project/Art/Environment/Generated/T_GroundGrass.png";

        [MenuItem("Survival/Generate Ground Texture")]
        public static void Generate()
        {
            string folder = Path.GetDirectoryName(OutputPath);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            const int size = 1024;
            var texture = new Texture2D(size, size, TextureFormat.RGB24, false);
            var pixels = new Color32[size * size];

            // Bảng màu lấy theo cách map trong video tham chiếu được vẽ: không phải một sắc xanh
            // đều tăm tắp, mà là những MẢNG MÀU LỚN xen kẽ nhau — chỗ cỏ non ngả vàng,
            // chỗ cỏ rậm xanh sẫm, chỗ đất trống lộ ra màu nâu.
            // Chính sự xen kẽ đó mới làm mặt sân trông như một khu đất thật.
            // Màu ở đây được chọn NGƯỢC từ kết quả mong muốn trên màn hình.
            // Ánh sáng trong cảnh nhân lên khoảng 1.3 lần, nên nếu lấy luôn màu xanh
            // mà mình muốn nhìn thấy thì kết quả sẽ sáng hơn và bị rửa trôi độ bão hoà.
            // Vì vậy các giá trị này cố tình TỐI HƠN và ÍT ĐỎ/XANH-LAM hơn:
            // giảm kênh đỏ và lam là cách làm màu xanh đậm đà lên, còn giảm đều cả ba kênh
            // thì chỉ làm nó xám đi chứ không bão hoà hơn.
            var grassMid = new Color(0.20f, 0.46f, 0.13f);
            var grassLight = new Color(0.34f, 0.62f, 0.16f);
            var grassDark = new Color(0.10f, 0.26f, 0.09f);
            var dirt = new Color(0.40f, 0.29f, 0.16f);
            var dirtDark = new Color(0.26f, 0.19f, 0.11f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Tầng RẤT LỚN quyết định vùng nào là cỏ non, vùng nào là cỏ rậm.
                    // Tần số thấp là điểm mấu chốt: mảng phải rộng bằng vài thân người
                    // thì mắt mới đọc ra là "khu đất có chỗ nọ chỗ kia",
                    // chứ nhiễu mịn chỉ cho ra một mặt phẳng lốm đốm nhìn như bị nhiễu hạt.
                    float zone = TileableNoise(x, y, size, 1.6f);

                    // Tầng vừa phá vỡ đường biên giữa các mảng cho đỡ tròn trịa giả tạo.
                    float mid = TileableNoise(x, y, size, 5f);

                    // Tầng mịn tạo lấm tấm như từng nhánh cỏ.
                    float fine = TileableNoise(x, y, size, 26f);

                    float blend = Mathf.Clamp01(zone * 0.72f + mid * 0.20f + fine * 0.08f);

                    Color color = blend < 0.45f
                        ? Color.Lerp(grassDark, grassMid, blend / 0.45f)
                        : Color.Lerp(grassMid, grassLight, (blend - 0.45f) / 0.55f);

                    // Vệt đất dùng một tầng nhiễu ĐỘC LẬP, không liên quan tới tầng cỏ.
                    // Nếu dùng chung thì đất sẽ luôn xuất hiện ở đúng chỗ cỏ sẫm nhất,
                    // tạo ra quy luật đều đặn mà mắt nhận ra ngay là đồ sinh bằng máy.
                    float dirtMask = TileableNoise(x, y, size, 2.3f);
                    float dirtAmount = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.62f, 0.80f, dirtMask));

                    if (dirtAmount > 0f)
                    {
                        Color dirtTone = Color.Lerp(dirtDark, dirt, fine);
                        color = Color.Lerp(color, dirtTone, dirtAmount * 0.85f);
                    }

                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            File.WriteAllBytes(OutputPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(OutputPath);
            var importer = AssetImporter.GetAtPath(OutputPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Trilinear;
                importer.mipmapEnabled = true;
                importer.anisoLevel = 4;   // giữ nét khi nhìn chếch, đúng góc camera của game
                importer.SaveAndReimport();
            }

            AssetDatabase.Refresh();
            Debug.Log($"[GroundTextureGenerator] Đã sinh {OutputPath}");
        }

        /// <summary>
        /// Nhiễu Perlin lát liền mạch.
        ///
        /// <c>Mathf.PerlinNoise</c> thường không lát khít được: mép trái và mép phải là hai
        /// vùng nhiễu khác nhau nên khi lát sẽ lộ ra một đường kẻ. Cách xử lý là lấy mẫu
        /// dọc theo một VÒNG TRÒN thay vì một đường thẳng — đi hết một vòng thì quay lại
        /// đúng điểm xuất phát, nên hai mép tự khớp nhau.
        /// </summary>
        private static float TileableNoise(int x, int y, int size, float frequency)
        {
            float u = (float)x / size;
            float v = (float)y / size;

            float angleU = u * Mathf.PI * 2f;
            float angleV = v * Mathf.PI * 2f;

            // Lấy bốn mẫu quanh vòng tròn rồi pha lại — cho ra kết quả liền mạch cả hai chiều.
            float nx = Mathf.PerlinNoise(Mathf.Cos(angleU) * frequency + 100f, Mathf.Sin(angleV) * frequency + 100f);
            float ny = Mathf.PerlinNoise(Mathf.Sin(angleU) * frequency + 200f, Mathf.Cos(angleV) * frequency + 200f);

            return Mathf.Clamp01((nx + ny) * 0.5f);
        }
    }
}

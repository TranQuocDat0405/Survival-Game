using System.IO;
using UnityEditor;
using UnityEngine;

namespace Survival.UI.EditorTools
{
    /// <summary>
    /// Tự sinh ảnh hình vành khuyên (vòng tròn rỗng ruột) dùng cho vòng hiển thị charge.
    ///
    /// Vì sao tự sinh thay vì đi tìm asset: bộ Kenney và Hyper Casual đều không có sẵn
    /// hình vành khuyên đúng tỉ lệ cần dùng. Tự sinh thì kiểm soát được chính xác
    /// độ dày, độ mượt viền, và không phải phụ thuộc vào một file ảnh bên ngoài
    /// mà người khác mở project ra có thể thiếu.
    ///
    /// Chạy qua menu: Survival > Generate UI Ring Sprites.
    /// Chỉ cần chạy một lần; ảnh sinh ra được lưu thành file trong project.
    /// </summary>
    public static class RingSpriteGenerator
    {
        private const string OutputFolder = "Assets/_Project/Art/UI/Generated";

        [MenuItem("Survival/Generate UI Ring Sprites")]
        public static void Generate()
        {
            if (!Directory.Exists(OutputFolder))
                Directory.CreateDirectory(OutputFolder);

            // Vành dày dùng cho vòng charge quanh nút bắn.
            WriteRing("Sprite_RingThick", size: 256, innerRadius01: 0.76f, outerRadius01: 0.98f);

            // Vành mảnh dùng cho viền cooldown của các nút phụ.
            WriteRing("Sprite_RingThin", size: 256, innerRadius01: 0.86f, outerRadius01: 0.98f);

            // Đĩa đặc dùng làm nền tròn cho nút.
            WriteRing("Sprite_Disc", size: 256, innerRadius01: 0f, outerRadius01: 0.98f);

            // Khung bo góc dùng cho nền thanh máu, nền bảng, nút chữ nhật.
            // Có viền 9-slice nên kéo dài ra bao nhiêu thì góc bo vẫn giữ nguyên hình dạng.
            WriteRoundedRect("Sprite_Panel", size: 64, cornerRadius: 16, border: 18);

            // Hình chữ nhật trắng trơn dùng cho phần TÔ ĐẦY của thanh.
            // Cố tình KHÔNG bo góc: ảnh đặt type = Filled sẽ bị cắt ngang theo tỉ lệ máu,
            // nếu ảnh có góc bo thì mép cắt sẽ lộ ra một đầu tròn một đầu vuông, nhìn rất kỳ.
            WriteRoundedRect("Sprite_FillPlain", size: 16, cornerRadius: 0, border: 0);

            AssetDatabase.Refresh();
            Debug.Log($"[RingSpriteGenerator] Đã sinh ảnh vào {OutputFolder}");
        }

        /// <summary>
        /// Sinh một hình chữ nhật trắng, bo góc tuỳ chọn, kèm thiết lập viền 9-slice.
        ///
        /// 9-slice nghĩa là Unity chia ảnh thành 9 ô: 4 góc giữ nguyên kích thước,
        /// 4 cạnh chỉ giãn theo một chiều, ô giữa giãn cả hai chiều.
        /// Nhờ vậy một ảnh 64x64 kéo thành thanh dài 470x46 mà góc bo vẫn tròn đều,
        /// không bị bóp méo thành hình bầu dục.
        /// </summary>
        private static void WriteRoundedRect(string fileName, int size, int cornerRadius, int border)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha = 1f;

                    if (cornerRadius > 0)
                    {
                        // Khoảng cách tới tâm của góc bo gần nhất.
                        float dx = Mathf.Max(cornerRadius - x, x - (size - 1 - cornerRadius), 0f);
                        float dy = Mathf.Max(cornerRadius - y, y - (size - 1 - cornerRadius), 0f);
                        float distance = Mathf.Sqrt(dx * dx + dy * dy);
                        alpha = Mathf.Clamp01((cornerRadius - distance) / 1.2f);
                        if (dx <= 0f || dy <= 0f)
                            alpha = 1f;   // nằm ngoài vùng góc thì luôn đặc
                    }

                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            string path = Path.Combine(OutputFolder, fileName + ".png");
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;

            if (border > 0)
            {
                // spriteBorder theo thứ tự trái, dưới, phải, trên.
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteBorder = new Vector4(border, border, border, border);
                importer.SetTextureSettings(settings);
            }

            importer.SaveAndReimport();
        }

        private static void WriteRing(string fileName, int size, float innerRadius01, float outerRadius01)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];

            float center = (size - 1) * 0.5f;
            float maxRadius = center;
            float inner = innerRadius01 * maxRadius;
            float outer = outerRadius01 * maxRadius;

            // Làm mượt viền trong khoảng một pixel, nếu không thì đường tròn sẽ bị răng cưa.
            const float feather = 1.2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    // Độ mờ giảm dần ở cả mép ngoài lẫn mép trong của vành.
                    float outerAlpha = Mathf.Clamp01((outer - distance) / feather);
                    float innerAlpha = inner <= 0f ? 1f : Mathf.Clamp01((distance - inner) / feather);
                    float alpha = Mathf.Min(outerAlpha, innerAlpha);

                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            string path = Path.Combine(OutputFolder, fileName + ".png");
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }
    }
}

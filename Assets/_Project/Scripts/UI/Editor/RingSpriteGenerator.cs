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

            // Hình chữ nhật trắng trơn. Hiện KHÔNG thanh nào còn dùng — mọi thanh đã chuyển sang
            // ảnh bo tròn hai đầu ở dưới. Vẫn sinh ra để dành cho những chỗ cần một mảng màu
            // đặc vuông vắn, ví dụ nền mờ hay gạch phân cách.
            WriteRoundedRect("Sprite_FillPlain", size: 16, cornerRadius: 0, border: 0);

            // ----- PHẦN TÔ ĐẦY CỦA CÁC THANH, BO TRÒN HAI ĐẦU -----
            //
            // Trước đây phần tô đầy dùng ảnh vuông trơn, vì lo rằng ảnh bo góc đặt type = Filled
            // sẽ bị cắt ngang và lộ ra một đầu tròn một đầu vuông. Nhưng chơi thật thì cái dở
            // hơn hẳn nằm ở lúc MÁU ĐẦY: bốn góc vuông của phần tô đầy thò hẳn ra khỏi góc bo
            // của nền, nhìn như một lỗi hiển thị.
            // Người chơi đã cân nhắc và chọn đánh đổi ngược lại: đầy máu thì khít nền, còn khi
            // vơi thì mép phải là một đường thẳng đứng — đó cũng là kiểu thanh máu phổ biến nhất.
            //
            // VÌ SAO PHẢI SINH RIÊNG MỘT ẢNH CHO TỪNG THANH:
            // type = Filled KHÔNG hỗ trợ 9-slice, nên ảnh bị kéo thẳng từ kích thước gốc sang
            // kích thước thật của thanh. Nếu tỉ lệ hai bên lệch nhau thì góc bo tròn biến thành
            // bầu dục. Sinh mỗi ảnh đúng tỉ lệ của thanh dùng nó thì góc luôn tròn đều.
            // Bán kính bo luôn bằng NỬA CHIỀU CAO, tức hai đầu là hai nửa hình tròn khít vào nền.
            WriteCapsuleFill("Sprite_FillRound_Health", width: 462, height: 38);
            WriteCapsuleFill("Sprite_FillRound_Exp",    width: 464, height: 28);
            WriteCapsuleFill("Sprite_FillRound_Enemy",  width: 324, height: 32);

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

        /// <summary>
        /// Sinh ảnh phần TÔ ĐẦY của một thanh: chữ nhật trắng bo tròn hai đầu (hình viên nang).
        ///
        /// Bán kính bo luôn bằng nửa chiều cao, nên hai đầu là hai nửa hình tròn hoàn chỉnh —
        /// khít đúng với góc bo của ảnh nền.
        ///
        /// Ảnh phải được sinh theo ĐÚNG tỉ lệ dài/cao của thanh sẽ dùng nó. Lý do: Image đặt
        /// type = Filled không dùng được 9-slice, Unity kéo thẳng ảnh cho vừa ô. Nếu ảnh vuông
        /// mà ô lại dài, hình tròn ở hai đầu sẽ bị kéo bẹt thành bầu dục.
        /// </summary>
        private static void WriteCapsuleFill(string fileName, int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];

            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;
            float radius = halfHeight;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Lấy tâm pixel rồi quy về góc phần tư thứ nhất — hình đối xứng cả hai trục
                    // nên chỉ cần tính một góc là suy ra được toàn bộ.
                    float px = Mathf.Abs(x + 0.5f - halfWidth);
                    float py = Mathf.Abs(y + 0.5f - halfHeight);

                    // Khoảng cách tới mép hình, tính từ tâm của đường bo gần nhất.
                    float qx = px - (halfWidth - radius);
                    float qy = py - (halfHeight - radius);

                    float outsideX = Mathf.Max(qx, 0f);
                    float outsideY = Mathf.Max(qy, 0f);
                    float distance = Mathf.Sqrt(outsideX * outsideX + outsideY * outsideY)
                                     + Mathf.Min(Mathf.Max(qx, qy), 0f)
                                     - radius;

                    // Chuyển khoảng cách thành độ mờ, trải đều trong đúng một pixel để mép mượt.
                    float alpha = Mathf.Clamp01(0.5f - distance);

                    pixels[y * width + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
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

            // Không nén: thanh chỉ cao vài chục pixel, nén khối sẽ làm mép bo lởm chởm
            // mà cũng chẳng tiết kiệm được bao nhiêu bộ nhớ.
            importer.textureCompression = TextureImporterCompression.Uncompressed;

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

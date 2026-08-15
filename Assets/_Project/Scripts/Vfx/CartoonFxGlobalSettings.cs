using UnityEngine;

namespace Survival.Vfx
{
    /// <summary>
    /// Tắt hai thứ mà bộ hiệu ứng Cartoon FX tự bật sẵn, và cả hai đều gây hại cho game này.
    ///
    /// 1. ĐÈN ĐỘNG — thủ phạm gây khựng hình.
    ///    Mỗi hiệu ứng nổ của bộ này kèm một đèn có hoạt ảnh. Dự án chạy Built-in RP dựng hình
    ///    kiểu forward, mà ở đó MỖI ĐÈN ĐỘNG bắt card đồ hoạ vẽ lại một lượt nữa lên toàn bộ vật
    ///    thể nó chiếu tới — sân này có hơn sáu nghìn vật trang trí.
    ///    Cú lướt nổ bốn quả cùng lúc, tức bốn đèn cùng bật, và đo được:
    ///
    ///        còn đèn : bình thường 19.9 ms  ->  lúc nổ 39.6 ms   (29/31 khung vượt 33 ms)
    ///        tắt đèn : bình thường 19.5 ms  ->  lúc nổ 19.6 ms   ( 0/60 khung vượt 33 ms)
    ///
    ///    Tức là khựng hình biến mất hoàn toàn, không còn dấu vết. Cái đánh đổi là mất một quầng
    ///    sáng nhỏ hắt xuống nền đất — trên nền rừng sáng của map này gần như không nhận ra.
    ///
    /// 2. RUNG CAMERA RIÊNG.
    ///    Bộ này tự rung camera theo cách của nó. Để nguyên thì rung đến từ hai nguồn, và những
    ///    hiệu ứng ta cố ý không muốn rung vẫn cứ rung. Toàn bộ việc rung phải do
    ///    <see cref="Survival.CameraRig.CameraShakeService"/> quyết, một nơi duy nhất.
    ///
    /// CHẠY TỰ ĐỘNG LÚC VÀO GAME nhờ <c>RuntimeInitializeOnLoadMethod</c>, không cần gắn vào
    /// scene nào cả. Đây là lựa chọn có chủ đích: nếu phải nhớ kéo thả một component thì sớm muộn
    /// cũng có lúc quên, và cái quên đó biểu hiện ra thành "tự nhiên game giật" — một triệu chứng
    /// chẳng gợi ý gì tới nguyên nhân thật.
    ///
    /// Gọi qua phản chiếu để dự án vẫn biên dịch được nếu sau này gỡ bộ Cartoon FX ra.
    /// </summary>
    public static class CartoonFxGlobalSettings
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Apply()
        {
            var type = FindCartoonFxEffectType();
            if (type == null)
                return;

            SetStaticFlag(type, "GlobalDisableLights", true);
            SetStaticFlag(type, "GlobalDisableCameraShake", true);
        }

        /// <summary>
        /// PHẢI duyệt qua mọi assembly chứ không đoán tên.
        ///
        /// Bộ Cartoon FX có asmdef riêng nên lớp của nó nằm trong <c>CFXRRuntime</c>, không phải
        /// <c>Assembly-CSharp</c> như script thường. Lần đầu tôi viết theo suy đoán đó và hàm chạy
        /// mà KHÔNG báo lỗi gì cả — chỉ là không có tác dụng. Kiểu lỗi im lặng đó tốn nhiều thời
        /// gian hơn hẳn một lỗi báo thẳng ra.
        /// </summary>
        private static System.Type FindCartoonFxEffectType()
        {
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

            for (int i = 0; i < assemblies.Length; i++)
            {
                var type = assemblies[i].GetType("CartoonFX.CFXR_Effect", throwOnError: false);
                if (type != null)
                    return type;
            }

            return null;
        }

        private static void SetStaticFlag(System.Type type, string fieldName, bool value)
        {
            var field = type.GetField(fieldName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            if (field != null && field.FieldType == typeof(bool))
                field.SetValue(null, value);
        }
    }
}

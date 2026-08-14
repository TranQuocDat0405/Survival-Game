using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Survival.EditorTools
{
    /// <summary>
    /// Dựng Animator Controller bằng code thay vì kéo thả tay trong cửa sổ Animator.
    ///
    /// Vì sao làm vậy:
    ///   - Dựng tay hàng chục state và transition rất dễ sót một đường chuyển, và lỗi kiểu đó
    ///     chỉ lộ ra khi chơi đúng vào tình huống hiếm.
    ///   - Chạy lại lệnh này là dựng lại y hệt, nên khi cần đổi clip hoặc thêm skill
    ///     thì sửa vài dòng ở đây rồi chạy lại, không phải nối lại cả sơ đồ.
    ///   - Người khác mở project ra đọc được chính xác luật chuyển trạng thái dưới dạng chữ.
    ///
    /// Chạy qua menu: Survival > Build Animator Controllers.
    /// </summary>
    public static class AnimatorBuilder
    {
        private const string OutputFolder = "Assets/_Project/Art/Characters/Animators";
        private const string AnimationFolder = "Assets/_Project/Art/Characters/Animations";

        [MenuItem("Survival/Build Animator Controllers")]
        public static void BuildAll()
        {
            if (!System.IO.Directory.Exists(OutputFolder))
                System.IO.Directory.CreateDirectory(OutputFolder);

            var clips = LoadClips();

            BuildPlayer(clips);
            BuildEnemy(clips, "AC_EnemyMelee", "Melee_1H_Attack_Slice_Diagonal");
            BuildEnemy(clips, "AC_EnemyRanged", "Ranged_Magic_Shoot");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AnimatorBuilder] Đã dựng xong Animator Controller.");
        }

        private static Dictionary<string, AnimationClip> LoadClips()
        {
            var result = new Dictionary<string, AnimationClip>();

            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { AnimationFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (!(asset is AnimationClip clip) || clip.name.StartsWith("__"))
                        continue;

                    if (!result.ContainsKey(clip.name))
                        result[clip.name] = clip;
                }
            }

            return result;
        }

        private static AnimationClip Get(Dictionary<string, AnimationClip> clips, string name)
        {
            if (clips.TryGetValue(name, out var clip))
                return clip;

            Debug.LogWarning($"[AnimatorBuilder] không tìm thấy clip '{name}'.");
            return null;
        }

        // ------------------------------------------------------------------ PLAYER

        private static void BuildPlayer(Dictionary<string, AnimationClip> clips)
        {
            string path = $"{OutputFolder}/AC_Player.controller";
            AssetDatabase.DeleteAsset(path);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);

            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

            // Nhân tốc độ PHÁT của clip chạy. Tách riêng khỏi "Speed" là có lý do:
            //   "Speed"          = pha trộn giữa đứng yên và chạy (0 tới 1)
            //   "LocomotionSpeed"= clip chạy được phát nhanh chậm bao nhiêu lần
            // Thiếu tham số thứ hai thì bước chân luôn giữ một nhịp cố định dù nhân vật
            // đi nhanh cỡ nào — đó chính là hiện tượng trượt băng.
            controller.AddParameter("LocomotionSpeed", AnimatorControllerParameterType.Float);

            controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dash", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Throw", AnimatorControllerParameterType.Trigger);

            var root = controller.layers[0].stateMachine;

            // Trạng thái nền: pha trộn giữa đứng yên và chạy theo tham số Speed.
            // Dùng Blend Tree chứ không phải hai state riêng, để lúc chuyển tốc độ
            // nhân vật hoà dần chứ không nhảy khựng giữa hai animation.
            var locomotion = CreateBlendTree(controller, root, "Locomotion", "Speed",
                Get(clips, "Idle_A"), Get(clips, "Running_HoldingBow"));
            root.defaultState = locomotion;

            // Cho tốc độ phát của trạng thái chạy được điều khiển bằng tham số, thay vì cố định 1.
            // Nhờ vậy code có thể ép nhịp bước khớp với quãng đường thật sự đi được.
            locomotion.speedParameterActive = true;
            locomotion.speedParameter = "LocomotionSpeed";

            var shoot = AddState(root, "Shoot", Get(clips, "Ranged_1H_Shoot"), new Vector3(320f, -120f, 0f));
            var dash  = AddState(root, "Dash",  Get(clips, "Dodge_Forward"),   new Vector3(320f, -40f, 0f));
            var throwState = AddState(root, "Throw", Get(clips, "Throw"),      new Vector3(320f, 40f, 0f));
            var hit   = AddState(root, "Hit",   Get(clips, "Hit_A"),           new Vector3(320f, 120f, 0f));
            var death = AddState(root, "Death", Get(clips, "Death_A"),         new Vector3(320f, 220f, 0f));

            // Ba skill và đòn trúng: vào bằng trigger, tự quay về khi clip chạy xong.
            LinkTrigger(locomotion, shoot, "Shoot");
            LinkTrigger(locomotion, dash, "Dash");
            LinkTrigger(locomotion, throwState, "Throw");
            LinkTrigger(locomotion, hit, "Hit");

            ReturnWhenFinished(shoot, locomotion);
            ReturnWhenFinished(dash, locomotion);
            ReturnWhenFinished(throwState, locomotion);
            ReturnWhenFinished(hit, locomotion);

            // Chết thì vào được từ BẤT KỲ trạng thái nào, vì player có thể chết ngay giữa
            // lúc đang bắn hoặc đang lướt. Dùng AnyState để không phải nối tay từng đường.
            var toDeath = root.AddAnyStateTransition(death);
            toDeath.AddCondition(AnimatorConditionMode.If, 0f, "Dead");
            toDeath.duration = 0.1f;
            toDeath.hasExitTime = false;
            toDeath.canTransitionToSelf = false;

            // Hồi sinh: cờ Dead tắt thì đứng dậy.
            var revive = death.AddTransition(locomotion);
            revive.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");
            revive.duration = 0.15f;
            revive.hasExitTime = false;

            EditorUtility.SetDirty(controller);
        }

        // ------------------------------------------------------------------ ENEMY

        private static void BuildEnemy(Dictionary<string, AnimationClip> clips, string fileName, string attackClipName)
        {
            string path = $"{OutputFolder}/{fileName}.controller";
            AssetDatabase.DeleteAsset(path);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);

            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

            // Tham số điều khiển TỐC ĐỘ PHÁT của clip tấn công.
            // Đây là thứ cho phép ép animation khớp với con số windup trong config.
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = "AttackSpeed",
                type = AnimatorControllerParameterType.Float,
                defaultFloat = 1f,
            });

            var root = controller.layers[0].stateMachine;

            var locomotion = CreateBlendTree(controller, root, "Locomotion", "Speed",
                Get(clips, "Idle_A"), Get(clips, "Running_A"));
            root.defaultState = locomotion;

            var attack = AddState(root, "Attack", Get(clips, attackClipName), new Vector3(320f, -60f, 0f));
            attack.speedParameterActive = true;
            attack.speedParameter = "AttackSpeed";

            var hit   = AddState(root, "Hit",   Get(clips, "Hit_A"),   new Vector3(320f, 40f, 0f));
            var death = AddState(root, "Death", Get(clips, "Death_A"), new Vector3(320f, 140f, 0f));

            LinkTrigger(locomotion, attack, "Attack");
            LinkTrigger(locomotion, hit, "Hit");
            ReturnWhenFinished(attack, locomotion);
            ReturnWhenFinished(hit, locomotion);

            var toDeath = root.AddAnyStateTransition(death);
            toDeath.AddCondition(AnimatorConditionMode.If, 0f, "Dead");
            toDeath.duration = 0.08f;
            toDeath.hasExitTime = false;
            toDeath.canTransitionToSelf = false;

            // Quái được tái sử dụng từ pool nên cũng cần đường đứng dậy khỏi tư thế chết.
            var revive = death.AddTransition(locomotion);
            revive.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");
            revive.duration = 0f;
            revive.hasExitTime = false;

            EditorUtility.SetDirty(controller);
        }

        // ------------------------------------------------------------------ tiện ích

        private static AnimatorState AddState(AnimatorStateMachine machine, string name, AnimationClip clip, Vector3 position)
        {
            var state = machine.AddState(name, position);
            state.motion = clip;
            state.writeDefaultValues = false;   // tránh các state ghi đè lẫn nhau khi hoà trộn
            return state;
        }

        private static AnimatorState CreateBlendTree(
            AnimatorController controller, AnimatorStateMachine machine,
            string name, string parameter, AnimationClip idle, AnimationClip run)
        {
            var state = machine.AddState(name, new Vector3(40f, 0f, 0f));
            state.writeDefaultValues = false;

            var tree = new BlendTree
            {
                name = name,
                blendType = BlendTreeType.Simple1D,
                blendParameter = parameter,
                useAutomaticThresholds = false,
            };

            AssetDatabase.AddObjectToAsset(tree, controller);

            if (idle != null) tree.AddChild(idle, 0f);
            if (run != null) tree.AddChild(run, 1f);

            state.motion = tree;
            return state;
        }

        private static void LinkTrigger(AnimatorState from, AnimatorState to, string trigger)
        {
            var transition = from.AddTransition(to);
            transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            transition.duration = 0.06f;   // rất ngắn: đòn đánh phải phản hồi tức thì
            transition.hasExitTime = false;
        }

        private static void ReturnWhenFinished(AnimatorState from, AnimatorState to)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = true;
            transition.exitTime = 0.85f;    // bắt đầu hoà về khi clip chạy được 85%
            transition.duration = 0.12f;
        }
    }
}

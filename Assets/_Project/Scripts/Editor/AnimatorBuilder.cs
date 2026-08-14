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

        /// <summary>
        /// Lấy controller ở đường dẫn cho sẵn, XOÁ RUỘT nhưng GIỮ NGUYÊN FILE.
        ///
        /// ĐÂY LÀ CHỖ ĐÃ GÂY RA MỘT SỰ CỐ ĐÁNG NHỚ, ghi lại để không ai lặp lại.
        ///
        /// Trước đây hàm dựng gọi <c>AssetDatabase.DeleteAsset</c> rồi tạo file mới. File mới
        /// mang một GUID MỚI, mà Unity tham chiếu asset bằng GUID chứ không phải bằng đường dẫn —
        /// nên mọi nơi đang trỏ tới controller cũ (Animator của player trong scene, Animator trong
        /// hai prefab quái) lập tức thành rỗng.
        ///
        /// Hậu quả khi chơi: nhân vật đứng chết một tư thế, không còn animation nào, và console
        /// đầy cảnh báo "Animator is not playing an AnimatorController".
        ///
        /// Điều nguy hiểm nhất là LỆNH DỰNG VẪN BÁO THÀNH CÔNG, không một lỗi nào —
        /// hỏng chỉ lộ ra khi bấm Play. Giữ nguyên file và chỉ xoá ruột thì GUID không đổi,
        /// mọi tham chiếu còn nguyên vẹn.
        /// </summary>
        private static AnimatorController CreateOrClear(string path)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
                return AnimatorController.CreateAnimatorControllerAtPath(path);

            while (controller.parameters.Length > 0)
                controller.RemoveParameter(0);

            while (controller.layers.Length > 1)
                controller.RemoveLayer(controller.layers.Length - 1);

            if (controller.layers.Length == 0)
                controller.AddLayer("Base Layer");

            var machine = controller.layers[0].stateMachine;

            foreach (var transition in machine.anyStateTransitions)
                machine.RemoveAnyStateTransition(transition);
            foreach (var transition in machine.entryTransitions)
                machine.RemoveEntryTransition(transition);
            foreach (var child in machine.stateMachines)
                machine.RemoveStateMachine(child.stateMachine);
            foreach (var child in machine.states)
                machine.RemoveState(child.state);

            // Blend Tree của lần dựng trước nằm lại làm asset con mồ côi trong chính file này.
            // Không dọn thì mỗi lần chạy lệnh file lại phình thêm một cây nữa.
            foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (sub is BlendTree)
                    Object.DestroyImmediate(sub, true);
            }

            return controller;
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
            var controller = CreateOrClear(path);

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

            // Cờ báo "đang lướt". Dùng để thoát khỏi trạng thái lướt ĐÚNG LÚC cú lướt kết thúc,
            // thay vì đoán theo thời lượng clip.
            //
            // Vì sao cần: clip Dodge_Forward dài 0.40 giây còn cú lướt theo spec kéo 0.50 giây.
            // Trước đây trạng thái thoát ra theo tỉ lệ clip (85% của 0.40 tức 0.34 giây), nên có
            // một khoảng nhân vật VẪN đang bị đẩy đi mà Animator đã về tư thế đứng — nhìn ra
            // thành trượt băng. Cứ chỉnh thời lượng dash trên config là lệch lại, nên buộc phải
            // đồng bộ theo trạng thái thật chứ không theo con số.
            controller.AddParameter("Dashing", AnimatorControllerParameterType.Bool);

            // BẬT IK PASS — bắt buộc, nếu không Unity KHÔNG BAO GIỜ gọi OnAnimatorIK.
            //
            // Đây là thứ dùng để ép bàn tay trái nắm vào báng nỏ (xem WeaponTwoHandIK).
            // Điều nguy hiểm là quên bật thì không có một lỗi hay cảnh báo nào cả —
            // bàn tay chỉ đơn giản là không nhúc nhích, và rất khó đoán ra nguyên nhân.
            //
            // layers[0] trả về một BẢN SAO chứ không phải tham chiếu tới layer thật,
            // nên phải sửa trên bản sao rồi GÁN NGƯỢC lại vào mảng thì thay đổi mới có hiệu lực.
            var layers = controller.layers;
            layers[0].iKPass = true;
            controller.layers = layers;

            var root = controller.layers[0].stateMachine;

            // Trạng thái nền: pha trộn giữa đứng yên và chạy theo tham số Speed.
            // Dùng Blend Tree chứ không phải hai state riêng, để lúc chuyển tốc độ
            // nhân vật hoà dần chứ không nhảy khựng giữa hai animation.
            // BỘ CLIP NỎ HAI TAY. Trước đây dùng Idle_A (đứng tay không) và Ranged_1H_Shoot
            // (bắn một tay), nên nhân vật cầm nỏ mà tư thế lại như đang cầm súng lục —
            // cây nỏ chĩa theo cánh tay chứ không chĩa ra trước.
            //
            // Bộ KayKit vốn có sẵn nhóm Ranged_2H_* làm riêng cho nỏ: hai tay ôm nỏ đưa
            // thẳng ra trước, và clip bắn đã có sẵn cú giật lùi. Dùng đúng bộ đó thì tư thế
            // khớp với vũ khí mà không phải chỉnh gì thêm.
            // DÙNG Ranged_2H_Shooting LÀM TƯ THẾ ĐỨNG, KHÔNG PHẢI Ranged_2H_Aiming.
            //
            // Tên nghe thì Aiming mới là tư thế đứng ngắm, nhưng đo ra mới biết nó là clip
            // CHUYỂN TIẾP: 0.3 giây đầu vung người vào tư thế ngắm, rồi giữ nguyên 1.3 giây.
            // Đặt lặp một clip như vậy thì cứ mỗi 1.6 giây nó giật ngược về tư thế ban đầu
            // rồi vung lại — người chơi thấy thân nhân vật giật đều đặn theo nhịp.
            //
            // Đo chỗ nối vòng lặp (so tư thế đầu clip với cuối clip):
            //   Ranged_2H_Aiming    ngực lệch 77.3 độ  -> giật mạnh
            //   Ranged_2H_Shooting  ngực lệch  0.0 độ  -> lặp sạch
            // Cùng bộ hai tay nên tư thế gần như nhau, chỉ khác là clip này lặp được.
            //
            // BÀI HỌC: trước khi dùng một clip để LẶP, phải kiểm tra tư thế đầu và cuối có
            // trùng nhau không. Tên clip không nói lên điều đó.
            var locomotion = CreateBlendTree(controller, root, "Locomotion", "Speed",
                Get(clips, "Ranged_2H_Shooting"), Get(clips, "Running_HoldingRifle"));
            root.defaultState = locomotion;

            // Cho tốc độ phát của trạng thái chạy được điều khiển bằng tham số, thay vì cố định 1.
            // Nhờ vậy code có thể ép nhịp bước khớp với quãng đường thật sự đi được.
            locomotion.speedParameterActive = true;
            locomotion.speedParameter = "LocomotionSpeed";

            // TẦNG GỐC chỉ giữ những động tác TOÀN THÂN: di chuyển, lăn người, gục chết.
            //
            // Bắn, ném bom và trúng đòn đã chuyển sang tầng thân trên (xem BuildUpperBodyLayer).
            // Lý do: chúng là động tác nửa người. Để ở tầng gốc thì lúc bắn cả clip chiếm luôn
            // hai chân, chân ngừng bước trong khi nhân vật vẫn trôi đi — nhìn ra thành trượt băng.
            // Dính độc còn tệ hơn vì mỗi giây một tick lại kích hoạt anim trúng đòn.
            var dash  = AddState(root, "Dash",  Get(clips, "Dodge_Forward"), new Vector3(320f, -40f, 0f));
            var death = AddState(root, "Death", Get(clips, "Death_A"),       new Vector3(320f, 220f, 0f));

            LinkTrigger(locomotion, dash, "Dash");

            // LƯỚT thoát ra theo TRẠNG THÁI THẬT chứ không theo thời lượng clip.
            //
            // Clip lướt dài 0.40 giây còn cú lướt theo spec kéo 0.50 giây. Thoát theo tỉ lệ clip
            // thì animation kết thúc sớm hơn cú lướt, và trong khoảng chênh đó nhân vật vẫn đang
            // bị đẩy đi trong khi Animator đã về tư thế đứng — đúng cảnh trượt băng người chơi thấy.
            //
            // Nối theo cờ Dashing thì hai thứ luôn kết thúc cùng lúc, và sau này có tune lại
            // thời lượng dash trên config bao nhiêu lần nữa cũng không phải sửa gì ở đây.
            var dashExit = dash.AddTransition(locomotion);
            dashExit.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dashing");
            dashExit.hasExitTime = false;
            dashExit.duration = 0.1f;

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

            // Tầng thân trên dựng SAU CÙNG, vì nó cần các tham số đã khai báo ở trên.
            BuildUpperBodyLayer(controller, clips);

            EditorUtility.SetDirty(controller);
        }

        // ------------------------------------------------------------------ ENEMY

        private static void BuildEnemy(Dictionary<string, AnimationClip> clips, string fileName, string attackClipName)
        {
            string path = $"{OutputFolder}/{fileName}.controller";
            var controller = CreateOrClear(path);

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

        // ------------------------------------------------------------------ tầng thân trên

        /// <summary>
        /// Tạo mặt nạ chỉ cho phép điều khiển THÂN TRÊN và HAI TAY.
        ///
        /// Đây là chìa khoá để vừa chạy vừa bắn. Nếu clip bắn chiếm cả người thì trong lúc bắn
        /// chân ngừng bước trong khi nhân vật vẫn trôi đi — nhìn ra thành trượt băng. Với mặt nạ
        /// này, tầng bắn chỉ ghi lên thân và tay, còn chân vẫn do tầng di chuyển điều khiển.
        ///
        /// Chân, bàn chân và gốc đều bị loại. Gốc đặc biệt quan trọng: cho phép nó thì clip bắn
        /// sẽ kéo cả vị trí nhân vật theo và đè lên chuyển động do vật lý điều khiển.
        /// </summary>
        private static AvatarMask CreateUpperBodyMask()
        {
            string path = $"{OutputFolder}/UpperBodyMask.mask";

            // Giữ nguyên file nếu đã có, vì Animator Controller tham chiếu tới nó bằng GUID —
            // xoá đi tạo lại sẽ làm tầng thân trên mất mặt nạ mà không có lỗi nào báo ra.
            var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(path);
            if (mask == null)
            {
                mask = new AvatarMask();
                AssetDatabase.CreateAsset(mask, path);
            }

            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Root, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFootIK, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFootIK, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftHandIK, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightHandIK, true);

            EditorUtility.SetDirty(mask);
            return mask;
        }

        /// <summary>
        /// Dựng tầng thân trên chứa các động tác chỉ dùng nửa người: bắn, ném bom, trúng đòn.
        ///
        /// Tầng này luôn bắt đầu ở một trạng thái RỖNG (không clip). Trạng thái rỗng nghĩa là
        /// tầng không ghi đè gì cả, nên bình thường nhân vật hoàn toàn theo tầng di chuyển.
        /// Khi có trigger thì nó nhảy sang động tác tương ứng, chạy xong lại về rỗng.
        ///
        /// Cú LƯỚT cố tình KHÔNG đặt ở đây mà để lại tầng gốc: lăn người là động tác toàn thân,
        /// tách nửa trên nửa dưới sẽ ra cảnh nửa người lăn còn hai chân vẫn chạy bộ.
        /// </summary>
        private static void BuildUpperBodyLayer(AnimatorController controller, Dictionary<string, AnimationClip> clips)
        {
            var mask = CreateUpperBodyMask();

            controller.AddLayer("UpperBody");
            var layers = controller.layers;
            var upper = layers[layers.Length - 1];
            upper.avatarMask = mask;
            upper.defaultWeight = 1f;
            upper.blendingMode = AnimatorLayerBlendingMode.Override;
            layers[layers.Length - 1] = upper;
            controller.layers = layers;

            var machine = upper.stateMachine;

            // Trạng thái rỗng: không có clip nên tầng này không ghi đè gì lên tầng dưới.
            var empty = machine.AddState("Empty", new Vector3(40f, 0f, 0f));
            empty.writeDefaultValues = false;
            machine.defaultState = empty;

            var shoot = AddState(machine, "Shoot", Get(clips, "Ranged_2H_Shoot"), new Vector3(320f, -80f, 0f));
            var throwState = AddState(machine, "Throw", Get(clips, "Throw"), new Vector3(320f, 0f, 0f));
            var hit = AddState(machine, "Hit", Get(clips, "Hit_A"), new Vector3(320f, 80f, 0f));

            LinkTrigger(empty, shoot, "Shoot");
            LinkTrigger(empty, throwState, "Throw");
            LinkTrigger(empty, hit, "Hit");

            ReturnWhenFinished(shoot, empty);
            ReturnWhenFinished(throwState, empty);
            ReturnWhenFinished(hit, empty);

            // Chết thì tầng trên phải NHẢ RA ngay, nếu không nửa người trên vẫn giữ tư thế bắn
            // trong khi nửa dưới đã gục xuống.
            var toEmpty = machine.AddAnyStateTransition(empty);
            toEmpty.AddCondition(AnimatorConditionMode.If, 0f, "Dead");
            toEmpty.duration = 0.1f;
            toEmpty.hasExitTime = false;
            toEmpty.canTransitionToSelf = false;
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

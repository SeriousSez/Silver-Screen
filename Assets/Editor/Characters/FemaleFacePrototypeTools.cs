using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SilverScreen.Presentation.Characters;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SilverScreen.Editor.Characters
{
    public static class FemaleFacePrototypeTools
    {
        public const string Root = "Assets/SilverScreen/Art/Characters/FemaleFacePrototype";
        public const string Review = "TestResults/FemaleFaceInvestigation";
        public const string Model = Root + "/Models/FemaleHybrid.fbx";
        public const string Prefab = Root + "/SS_FemaleFacePrototype.prefab";
        [Serializable] private class ExportData { public string[] face_bones; public ExportPose[] poses; }
        [Serializable] private class ExportPose { public string name; public float firstFrame; public float lastFrame; }

        [MenuItem("SilverScreen/Characters/Female Face Investigation/1 Import and build isolated hybrid")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Build in Edit Mode.");
            Directory.CreateDirectory(Review); AssetDatabase.Refresh();
            var data = JsonUtility.FromJson<ExportData>(File.ReadAllText(Review + "/hybrid-export.json"));
            var lines = new List<string>();
            foreach (var name in new[] { "FaceRigAll", "FaceRigDeform", "ShapeKeysAll", "ShapeKeysDeform" })
            {
                string path = Root + "/Variants/" + name + ".fbx";
                Import(path, null); Describe(path, lines);
            }
            Import(Model, data); Describe(Model, lines);
            File.WriteAllLines(Review + "/unity-variants.txt", lines);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Model);
            if (!asset.GetComponent<Animator>().avatar.isHuman || !asset.GetComponent<Animator>().avatar.isValid)
                throw new InvalidOperationException("Hybrid Avatar is invalid.");
            var clips = AssetDatabase.LoadAllAssetsAtPath(Model).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
            var controllerPath = Root + "/FacialSamples.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                foreach (var clip in clips) controller.layers[0].stateMachine.AddState(clip.name).motion = clip;
            }
            var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(HumanBasePrototypeTools.PrefabPath));
            try
            {
                go.name = "SS_FemaleFacePrototype";
                var presentation = go.GetComponent<HumanBasePrototypeVisual>();
                Transform visualRoot = presentation.Animator.transform.parent;
                var oldModel = presentation.Animator.gameObject;
                Vector3 scale = oldModel.transform.localScale, offset = oldModel.transform.localPosition;
                Object.DestroyImmediate(oldModel);
                var model = Object.Instantiate(asset, visualRoot); model.name = "AdultFemaleHybrid";
                model.transform.localScale = scale; model.transform.localPosition = offset;
                var animator = model.GetComponent<Animator>();
                animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.runtimeAnimatorController = controller;
                var all = model.GetComponentsInChildren<Transform>(true);
                var bones = data.face_bones.Select(n => all.Single(t => t.name == n)).ToArray();
                var bindings = new List<FemaleFacePrototype.PoseBinding>();
                foreach (var pose in data.poses)
                {
                    if (!Enum.TryParse(pose.name, out PrototypeFacePose semantic)) continue;
                    animator.Rebind(); animator.Play(pose.name, 0, .5f); animator.Update(0);
                    bindings.Add(new FemaleFacePrototype.PoseBinding { semantic = semantic,
                        bones = bones.Select(t => new FemaleFacePrototype.BonePose { position = t.localPosition, rotation = t.localRotation, scale = t.localScale }).ToArray() });
                }
                var skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
                skin.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(HumanBasePrototypeTools.Root + "/Materials/SuppliedNeutralURP.mat");
                skin.updateWhenOffscreen = true; skin.quality = SkinQuality.Auto;
                var face = go.AddComponent<FemaleFacePrototype>();
                face.Configure(bones, bindings.ToArray(), new[] {
                    Mouth(PrototypeMouthPose.SmileLeft,"mouthSmileLeft"), Mouth(PrototypeMouthPose.SmileRight,"mouthSmileRight"),
                    Mouth(PrototypeMouthPose.Smile,"mouthSmileLeft","mouthSmileRight"),
                    Mouth(PrototypeMouthPose.Frown,"mouthFrownLeft","mouthFrownRight"),
                    Mouth(PrototypeMouthPose.JawOpen,"jawOpen"), Mouth(PrototypeMouthPose.MouthPucker,"mouthPucker") }, skin);
                animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(HumanBasePrototypeTools.Root + "/Animation/PrototypeLocomotion.controller");
                animator.Rebind(); animator.Play("Idle",0,0); animator.Update(0);
                face.ResetNeutral(); presentation.Configure(animator,visualRoot,skin);
                PrefabUtility.SaveAsPrefabAsset(go,Prefab);
                File.WriteAllText(Review + "/binding-poses.json", JsonUtility.ToJson(new BindingEvidence { poses = bindings.ToArray() },true));
            }
            finally { Object.DestroyImmediate(go); }
            AssetDatabase.SaveAssets(); Debug.Log("Female facial hybrid built without replacing the original prototype.");
        }
        [Serializable] private class BindingEvidence { public FemaleFacePrototype.PoseBinding[] poses; }
        private static FemaleFacePrototype.MouthBinding Mouth(PrototypeMouthPose semantic,params string[] names)
            => new FemaleFacePrototype.MouthBinding { semantic=semantic,shapes=names };
        private static void Import(string path,ExportData data)
        {
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importBlendShapes=true;importer.importNormals=ModelImporterNormals.Import;
            importer.optimizeGameObjects=false;importer.importCameras=false;importer.importLights=false;
            importer.globalScale=1;importer.useFileScale=true;
            importer.skinWeights=ModelImporterSkinWeights.Custom;importer.maxBonesPerVertex=10;importer.minBoneWeight=.00001f;
            importer.importAnimation=data!=null;
            if (data!=null)
            {
                // Automatic mapping incorrectly chose a lower-eyelid bone for Jaw.
                // Reuse the already validated body mapping and drive all facial bones explicitly.
                var bodyImporter=(ModelImporter)AssetImporter.GetAtPath(HumanBasePrototypeTools.Root+"/Models/AdultFemale.fbx");
                var human=importer.humanDescription; human.human=bodyImporter.humanDescription.human; importer.humanDescription=human;
                string maskPath=Root+"/FacialImport.mask";
                var mask=AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
                if(mask==null){mask=new AvatarMask();AssetDatabase.CreateAsset(mask,maskPath);}
                mask.transformCount=0;mask.AddTransformPath(AssetDatabase.LoadAssetAtPath<GameObject>(path).transform,true);
                for(int i=0;i<mask.transformCount;i++)mask.SetTransformActive(i,true);
                for(int i=0;i<(int)AvatarMaskBodyPart.LastBodyPart;i++)mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i,true);
                EditorUtility.SetDirty(mask);AssetDatabase.SaveAssets();
                importer.animationCompression=ModelImporterAnimationCompression.Off;
                var clips=importer.defaultClipAnimations;
                float start=clips.Length>0?clips[0].firstFrame:0;
                importer.clipAnimations=data.poses.Select(p=>new ModelImporterClipAnimation { name=p.name,
                    takeName=clips.Length>0?clips[0].takeName:"FacialPoseSamples",firstFrame=start+p.firstFrame-1,lastFrame=start+p.lastFrame-1,
                    loopTime=false,lockRootRotation=true,lockRootPositionXZ=true,lockRootHeightY=true,
                    maskType=ClipAnimationMaskType.CopyFromOther,maskSource=mask }).ToArray();
            }
            importer.SaveAndReimport();
        }
        private static void Describe(string path,List<string> lines)
        {
            var go=AssetDatabase.LoadAssetAtPath<GameObject>(path);var avatar=go.GetComponent<Animator>()?.avatar;
            lines.Add(path+" human="+(avatar!=null&&avatar.isHuman)+" valid="+(avatar!=null&&avatar.isValid)+" transforms="+go.GetComponentsInChildren<Transform>(true).Length);
            foreach(var skin in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                lines.Add("  "+skin.name+" vertices="+skin.sharedMesh.vertexCount+" triangles="+skin.sharedMesh.triangles.Length/3+" bones="+skin.bones.Length+" shapes="+skin.sharedMesh.blendShapeCount+" materials="+skin.sharedMaterials.Length);
        }
    }
}

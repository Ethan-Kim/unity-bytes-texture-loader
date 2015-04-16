#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;
using LitJson;
using System.Collections;
using System.Collections.Generic;
using System.IO;

[CustomEditor (typeof (SpriteAnimationLoader))]
public class SpriteAnimationLoaderEditor : Editor {
	public string Prefix = "Anim_";

	public override void OnInspectorGUI () {
		DrawDefaultInspector ();

		SpriteAnimationLoader loader = (SpriteAnimationLoader)target;
		Animator animator = loader.gameObject.GetComponent <Animator> ();
		if (GUILayout.Button ("Make AnimatorName")) {
			loader.AnimatorName = animator.runtimeAnimatorController.name;
		}
		string metaFile = string.Empty;
		if (string.IsNullOrEmpty (loader.AnimatorName) == false) {
			metaFile = Application.dataPath + "/StreamingAssets/" + Prefix + loader.AnimatorName + ".json";
		}
		if (GUILayout.Button ("Create Animator Meta Data")) {
			var clips = AnimationUtility.GetAnimationClips (loader.gameObject);
			if (string.IsNullOrEmpty (loader.AnimatorName) == false && clips.Length > 0) {
				AnimatorMetaData metaData = new AnimatorMetaData (animator, clips);
                FileInfo metaFileinfo = new FileInfo (metaFile);
                if (metaFileinfo.Exists) {
                    using (var sw = new StreamWriter (File.Open (metaFile, FileMode.Truncate, FileAccess.Write))) {
                        sw.Write (JsonMapper.ToJson (metaData));
                    }
                } else {
                    using (var sw = new StreamWriter (File.Open (metaFile, FileMode.Create, FileAccess.Write))) {
                        sw.Write (JsonMapper.ToJson (metaData));
                    }
                }
				Debug.Log (string.Format ("created meta {0}", metaFile));
			}
		}
		if (GUILayout.Button ("Remove Sprite Animation Data")) {
			var clips = AnimationUtility.GetAnimationClips (loader.gameObject);
			if (string.IsNullOrEmpty (loader.AnimatorName) == false && clips.Length > 0) {
				for (int i = 0; i < clips.Length; ++i) {
					var bindings = AnimationUtility.GetObjectReferenceCurveBindings (clips [i]);
					for (int j = 0; j < bindings.Length; ++j) {
						var curve = AnimationUtility.GetObjectReferenceCurve (clips [i], bindings[j]);
						for (int k = 0; k < curve.Length; ++k) {
							if (curve [k].value is Sprite) {
								AnimationUtility.SetObjectReferenceCurve (clips [i], bindings [j], null);
								break;
							}
						}
					}
				}
			}
		}
		if (GUILayout.Button ("Restore Sprite Animation Data")) {
			using (var sw = new StreamReader (File.Open (metaFile, FileMode.Open, FileAccess.Read))) {
				string json = sw.ReadToEnd ();
				var metaData = JsonMapper.ToObject <AnimatorMetaData> (json);

				var clips = AnimationUtility.GetAnimationClips (loader.gameObject);
				for (int i = 0; i < clips.Length; ++i) {
					var clip = metaData.Clips.Find (c => c.Name == clips [i].name);
					if (clip == null) {
						continue;
					}
					for (int j = 0; j < clip.Bindings.Count; ++j) {
						var binding = clip.Bindings [j];
						EditorCurveBinding curveBinding = new EditorCurveBinding();
                        curveBinding.type = Types.GetType (binding.BindingType, binding.GetAssembly ());
						curveBinding.path = binding.Path;
						curveBinding.propertyName = "m_Sprite";
						ObjectReferenceKeyframe[] runFrames = new ObjectReferenceKeyframe [binding.Frames.Count];
						for (int k = 0; k < binding.Frames.Count; ++k) {
							runFrames [k] = new ObjectReferenceKeyframe ();
                            runFrames [k].time = binding.Frames[k].Time;
                            string spriteName = binding.Frames[k].SpriteName;
                            Sprite spr = null;
							var guids = AssetDatabase.FindAssets (spriteName + " t:texture2D", new string[] {"Assets/FindingButler"});
							string guid = Utils.findGuid (guids, spriteName);
							DebugUtils.Assert (guid != string.Empty);
							var path = AssetDatabase.GUIDToAssetPath(guid);
                            if (binding.Frames [k].Idx == -1) {
								spr = AssetDatabase.LoadAssetAtPath (path, typeof(Sprite)) as Sprite;
                            } else {
								var sprs = AssetDatabase.LoadAllAssetRepresentationsAtPath (path);
								spr = sprs [binding.Frames [k].Idx] as Sprite;
							}
							DebugUtils.Assert (spr != null);
							runFrames [k].value = spr;
						}
						if (runFrames.Length > 0) {
                            AnimationUtility.SetObjectReferenceCurve (clips[i], curveBinding, runFrames);
						}
					}
				}
			}
        }
        if (GUILayout.Button ("Clear")) {
            loader.AnimatorName = string.Empty;
            loader.gameObject.GetComponent <Animator> ().runtimeAnimatorController = null;
		}
		if (GUILayout.Button ("Finish")) {
			loader.gameObject.GetComponent <Animator> ().runtimeAnimatorController = null;
		}
	}
}
#endif

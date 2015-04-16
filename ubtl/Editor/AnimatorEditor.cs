using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections;

[CustomEditor (typeof (Animator))]
public class AnimatorEditor : Editor {

	public override void OnInspectorGUI () {
		DrawDefaultInspector ();

		Animator script = (Animator)target;
		if (GUILayout.Button ("Make First Frame")) {
			var clips = AnimationUtility.GetAnimationClips (script.gameObject);
			for (int i = 0; i < clips.Length; ++i) {
				AddFirstFrameToClipBinding (clips [i], string.Empty, script.gameObject);
			}
		}
	}

	public class AnimatorEditorTarget {
		GameObject Obj = null;

		public AnimatorEditorTarget (GameObject obj) {
			Obj = obj;
		}
		public SpriteRenderer Spr {
			get {
				return Obj.GetComponent <SpriteRenderer> ();
			}
		}
		public Image Img {
			get {
				return Obj.GetComponent <Image> ();
			}
		}
		public object Ref {
			get {
				if (Spr != null) {
					return Spr;
				}
				if (Img != null) {
					return Img;
				}
				return null;
			}
		}
		public Sprite Sprite {
			get {
				if (Spr != null) {
					return Spr.sprite;
				}
				if (Img != null) {
					return Img.sprite;
				}
				return null;
			}
		}
	}

	void AddFirstFrameToClipBinding (AnimationClip clip, string root, GameObject obj) {
		string path = root;
		AnimatorEditorTarget target = new AnimatorEditorTarget (obj);
		if (target.Ref != null) {
			var bindings = AnimationUtility.GetObjectReferenceCurveBindings (clip);
			bool sprExists = false;
			for (int j = 0; j < bindings.Length; ++j) {
				if (bindings [j].path == path && bindings [j].propertyName == "m_Sprite") {
					sprExists = true;
					break;
				}
			}
			if (sprExists == false) {
				AddCurveBinding (clip, path, target);
			}
		}
		foreach (Transform t in obj.transform) {
			path = root == string.Empty ? t.gameObject.name : string.Format ("{0}/{1}", root, t.gameObject.name);
			AddFirstFrameToClipBinding (clip, path, t.gameObject);
		}
	}

	void AddCurveBinding (AnimationClip clip, string path, AnimatorEditorTarget target) {
		EditorCurveBinding curveBinding = new EditorCurveBinding();
		curveBinding.type = target.Ref.GetType ();
		curveBinding.path = path;
		curveBinding.propertyName = "m_Sprite";
		ObjectReferenceKeyframe[] runFrames = new ObjectReferenceKeyframe [1];
		runFrames [0] = new ObjectReferenceKeyframe ();
		runFrames [0].time = 0f;
		runFrames [0].value = target.Sprite;
		AnimationUtility.SetObjectReferenceCurve (clip, curveBinding, runFrames);
	}
}

using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class AnimatorMetaData {
    public List<ClipMetaData> Clips;

    public AnimatorMetaData () { }
#if UNITY_EDITOR
    public AnimatorMetaData (Animator animator, AnimationClip[] clips) {
        Clips = new List<ClipMetaData> ();
        for (int i = 0; i < clips.Length; ++i) {
			var clip = new ClipMetaData (animator, clips[i]);
            Clips.Add (clip);
        }
    }
#endif
}

public class ClipMetaData {
    public string Name;
	public int StateHash;
    public List<BindingMetaData> Bindings;
	public bool IsLoop;

    public ClipMetaData () { }
#if UNITY_EDITOR
	public ClipMetaData (Animator animator, AnimationClip clip) {
		Name = clip.name;
		StateHash = getThisClipStateHash (animator, clip);
		IsLoop = clip.isLooping;
		Bindings = new List<BindingMetaData> ();
		var bindings = AnimationUtility.GetObjectReferenceCurveBindings (clip);
		for (int i = 0; i < bindings.Length; ++i) {
			if (bindings[i].propertyName == "m_Sprite") {
				var binding = new BindingMetaData (clip, bindings[i]);
				Bindings.Add (binding);
			}
		}
	}
	static int getThisClipStateHash (Animator animator, AnimationClip clip) {
		var controller = animator.runtimeAnimatorController as UnityEditorInternal.AnimatorController;
		for (int i = 0; i < controller.layerCount; ++i) {
			var sm = controller.GetLayer (i).stateMachine;
			for (int j = 0; j < sm.stateCount; ++j) {
				var state = sm.GetState (j);
				var m = state.GetMotion ();
				if (m != null && (AnimationClip)m == clip) {
					return state.uniqueNameHash;
				}
			}
		}
		return 0;
	}
#endif
}

public class BindingMetaData {
    public string BindingType;
    public string Path;
    public List<SpriteAnimationKeyMetaData> Frames;

    public BindingMetaData () { }
#if UNITY_EDITOR
    public BindingMetaData (AnimationClip clip, EditorCurveBinding binding) {
        BindingType = binding.type.ToString ();
        Path = binding.path;
        Frames = new List<SpriteAnimationKeyMetaData> ();
        var curve = AnimationUtility.GetObjectReferenceCurve (clip, binding);
        if (curve.Length > 0) {
            for (int i = 0; i < curve.Length; ++i) {
                if (curve[i].value is Sprite) {
                    var frame = new SpriteAnimationKeyMetaData (curve[i]);
                    Frames.Add (frame);
                }
            }
        }
    }
#endif
    public string GetAssembly () {
        return BindingType.Substring (0, BindingType.LastIndexOf ('.'));
    }
}

public class SpriteAnimationKeyMetaData {
    public float Time;
    public string SpriteName;
    protected Sprite Sprite;
    public int Idx;

    public SpriteAnimationKeyMetaData () { }
#if UNITY_EDITOR
    public SpriteAnimationKeyMetaData (ObjectReferenceKeyframe frame) {
        Time = frame.time;
        var assetPath = AssetDatabase.GetAssetPath ((Sprite)frame.value);
        FileInfo info = null;
        TextureImporter importer = null;
		if (string.IsNullOrEmpty (assetPath) == false) {
			info = new FileInfo (assetPath);
			importer = (TextureImporter)AssetImporter.GetAtPath (assetPath);
		}
        if (importer.spriteImportMode == SpriteImportMode.Multiple) {
            SpriteName = info.Name.Substring (0, info.Name.IndexOf ('.'));
            for (int i = 0; i < importer.spritesheet.Length; ++i) {
                var sheet = importer.spritesheet [i];
                if (sheet.name == frame.value.name) {
                    Idx = i;
                    break;
                }
            }
        } else {
            SpriteName = frame.value.name;
            Idx = -1;
        }
    }
#endif
    public void SetSprite (Sprite spr) {
        Sprite = spr;
    }
    public Sprite GetSprite () {
        return Sprite;
    }
}

public class SpriteAnimationLoader : MonoBehaviour {
	[ShowOnly]
	public string AnimatorName;
    Animator Target = null;
    public AnimatorMetaData Data = null;
    public bool ColorChangeWhenComplete = false;
	public bool AutoLoad = false;
	public bool LoadInRuntime = true;
	public bool AudoEnable = true;

	void OnEnable () {
		Current = null;
		AnimationStartTime = 0f;
		CreateTarget ();
		if (Target.runtimeAnimatorController == null) {
			if (AutoLoad) {
				Load ();
			}
		}
	}

	void CreateTarget () {
		Target = gameObject.GetComponent<Animator> ();
		DebugUtils.Assert (Target != null);
	}

	public void Load () {
		CreateTarget ();
		if (Target != null)
			StartCoroutine (LoadAnimator ());
	}

    IEnumerator LoadAnimator () {
        while (SpriteAnimationPool.Instance == null) {
            yield return null;
        }
        Target.enabled = false;
		yield return StartCoroutine (SpriteAnimationPool.Instance.Create (AnimatorName, LoadInRuntime));
        Data = SpriteAnimationPool.Instance.Get (AnimatorName);
        Replacements = new List<SpriteTextureReplacement> ();
        for (int i = 0; i < Data.Clips.Count; ++i) {
            var clip = Data.Clips [i];
            Replacements.Add (new SpriteTextureReplacement (gameObject, clip));
        }
		Target.runtimeAnimatorController = SpriteAnimationPool.Instance.CreateAnimator (AnimatorName);
		Target.enabled = AudoEnable;

        if (ColorChangeWhenComplete) {
            yield return StartCoroutine (LoadAnimationCompleted ());
        }
    }

    class SpriteTextureReplacement {
        public string Clip;
		public int StateHash;
        public List<SpriteTextureReplacementBinding> Targets;
        public float Length;

        protected SpriteTextureReplacement () { }
        public SpriteTextureReplacement (GameObject go, ClipMetaData data) {
            Clip = data.Name;
			StateHash = data.StateHash;
            Targets = new List<SpriteTextureReplacementBinding> ();
            for (int i = 0; i < data.Bindings.Count; ++i) {
                var binding = data.Bindings[i];
                var bindingData = new SpriteTextureReplacementBinding (go, binding);
                Targets.Add (bindingData);
            }
            Length = 0;
            for (int i = 0; i < Targets.Count; ++i) {
                Length = Mathf.Max (Length, Targets [i].Length);
            }
        }

        public void Play (float elapsed) {
            for (int i = 0; i < Targets.Count; ++i) {
                Targets [i].Play (elapsed);
            }
        }
    }

    class SpriteTextureReplacementBinding {
        public string Path;
        public SpriteTextureLoader.LoaderTarget Target;
        public List<SpriteAnimationKeyMetaData> Frames;
        public float Length;
        SpriteAnimationKeyMetaData LastKey = null;
        Color SavedColor;

        protected SpriteTextureReplacementBinding () { }
        public SpriteTextureReplacementBinding (GameObject go, BindingMetaData data) {
            Path = data.Path;
            if (string.IsNullOrEmpty (Path)) {
                SpriteTextureLoader loader = go.GetComponent<SpriteTextureLoader> ();
                Target = loader.Target;
            } else {
                if (data.BindingType.IndexOf (".Image") != -1) {
                    Image img = go.transform.Find (data.Path).gameObject.GetComponent<Image> ();
                    DebugUtils.Assert (img != null);
                    SpriteTextureLoader.LoaderTarget target = new SpriteTextureLoader.LoaderTarget (img);
                    Target = target;
                } else if (data.BindingType.IndexOf (".SpriteRenderer") != -1) {
                    SpriteRenderer spr = go.transform.Find (data.Path).gameObject.GetComponent<SpriteRenderer> ();
                    DebugUtils.Assert (spr != null);
                    SpriteTextureLoader.LoaderTarget target = new SpriteTextureLoader.LoaderTarget (spr);
                    Target = target;
                } else {
                    DebugUtils.Assert (false);
                }
            }
            SavedColor = Target.Color;
            Frames = data.Frames;
            Length = data.Frames [Frames.Count - 1].Time;
        }

        SpriteAnimationKeyMetaData getKeyData (float elapsed) {
			var key = Frames.FindLast (f => f.Time <= elapsed);
            return key;
        }

        public void Play (float elapsed) {
            var key = getKeyData (elapsed);
			if (key != null && key != LastKey) {
                Target.Sprite = key.GetSprite ();
                LastKey = key;
            }
        }

        public Color getSavedColor () {
            return SavedColor;
        }
    }
    List <SpriteTextureReplacement> Replacements = null;
    SpriteTextureReplacement Current = null;
    float AnimationStartTime = 0f;

	float getCurrentTime (Animator animator) {
		if (Target.updateMode == AnimatorUpdateMode.UnscaledTime) {
			return Time.realtimeSinceStartup;
		} else {
			return Time.time;
		}
	}

    void Update () {
        if (Target == null || Data == null) {
            return;
        }
        if (Target.enabled == false) {
            return;
        }
		var info = Target.GetCurrentAnimatorStateInfo (0);
        if (Current == null) {
			Current = Replacements.Find (r => r.StateHash == info.nameHash);
        }
        if (Current == null) {
            return;
		}
		if (AnimationStartTime <= 0f) {
			AnimationStartTime = getCurrentTime (Target);
		}
		if (Current.StateHash != info.nameHash) { // change
			Current = Replacements.Find (r => r.StateHash == info.nameHash);
			AnimationStartTime = getCurrentTime (Target);
//			Debug.Log (string.Format ("Change to {0}", Current.Clip));
        }
		float currentTime = getCurrentTime (Target);
		float elapsed = (currentTime - AnimationStartTime) * Target.speed;
		var clip = Data.Clips.Find (c => c.Name == Current.Clip);
		if (clip != null && clip.IsLoop && elapsed >= Current.Length) {
            elapsed = 0f;
			AnimationStartTime = getCurrentTime (Target);
        }
        for (int i = 0; i < Current.Targets.Count; ++i) {
            Current.Play (elapsed);
        }
     }

    //public void OnClickSpeedUp () {
    //    Target.speed += 0.2f;
    //}

    //public void OnClickSpeedDown () {
    //    Target.speed -= 0.2f;
    //    Target.speed = Mathf.Max (0f, Target.speed);
    //}

    IEnumerator LoadAnimationCompleted () {
        while (Current == null) {
            yield return null;
        }
        var repl = Current;
        float startTime = Time.time;
        float t = (Time.time - startTime) * 2f;
		for (int i = 0; i < repl.Targets.Count; ++i) {
			var path = repl.Targets [i];
			path.Target.End ();
		}
        while (t <= 1f && Current != null) {
			for (int i = 0; i < repl.Targets.Count; ++i) {
				var path = repl.Targets[i];
				var color = path.getSavedColor ();
				path.Target.Color = new Color (color.r, color.g, color.b, t);
                t = (Time.time - startTime) * 2f;
            }
            yield return null;
        }
        for (int i = 0; i < repl.Targets.Count; ++i) {
			var path = repl.Targets[i];
			var color = path.getSavedColor ();
			path.Target.Color = color;
        }
    }

	public void ChangeTo (string animatorName) {
		AnimatorName = animatorName;
		Current = null;
	}
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LitJson;

public class SpriteAnimationPool : MonoBehaviour {
    public static SpriteAnimationPool Instance;
    public Dictionary<string, AnimatorMetaData> AnimationPool = new Dictionary<string, AnimatorMetaData> ();
    Object PoolLock = new Object ();
	public List <RuntimeAnimatorController> RuntimeAnimatorPrefabs = new List<RuntimeAnimatorController> ();
	public List <string> StreamingAnimators = new List<string> ();

	void Awake () {
		if (Instance == null) {
			DontDestroyOnLoad (gameObject);
			Instance = this;
#if UNITY_EDITOR
			DataInitialized = true;
#else
			if (Application.loadedLevelName != "Title") {
				StartCoroutine (Preload ());
			}
#endif
		} else if (Instance != this) {
			Destroy (gameObject);
		}
	}
	
	public bool DataInitialized = false;
	
	public IEnumerator Preload () {
		DataInitialized = false;
		for (int i = 0; i < StreamingAnimators.Count; ++i) {
			yield return StartCoroutine (Create (StreamingAnimators [i], false));
		}
		DataInitialized = true;
	}

    public IEnumerator Create (string animname, bool runtime) {
		while (runtime == true && DataInitialized == false) {
			yield return null;
		}
        if (AnimationPool.ContainsKey (animname) == false) {
            // meta - ani
			string aniUrl = Utils.getStreamingUrlOf ("Anim_" + animname + ".json");
            WWW metaAni = new WWW (aniUrl);
            yield return metaAni;

            string jsonAni = metaAni.text;
            var metaAniData = JsonMapper.ToObject<AnimatorMetaData> (jsonAni);

            List<string> RequestTexs = new List<string> ();
			for (int i = 0; i < metaAniData.Clips.Count; ++i) {
				var clip = metaAniData.Clips[i];
                for (int j = 0; j < clip.Bindings.Count; ++j) {
                    var binding = clip.Bindings[j];
                    for (int k = 0; k < binding.Frames.Count; ++k) {
                        var frame = binding.Frames[k];
                        if (RequestTexs.Find (tex => tex == frame.SpriteName) == null) {
                            RequestTexs.Add (frame.SpriteName);
                        }
                    }
                }
            }
            for (int i = 0; i < RequestTexs.Count; ++i) {
                yield return StartCoroutine (SpriteTexturePool.Instance.Create (RequestTexs[i], true));
            }
			for (int i = 0; i < metaAniData.Clips.Count; ++i) {
				var clip = metaAniData.Clips[i];
                for (int j = 0; j < clip.Bindings.Count; ++j) {
                    var binding = clip.Bindings[j];
                    for (int k = 0; k < binding.Frames.Count; ++k) {
                        var frame = binding.Frames[k];
                        Sprite spr = SpriteTexturePool.Instance.Get (frame.SpriteName, frame.Idx);
                        DebugUtils.Assert (spr != null);
                        frame.SetSprite (spr);
                    }
                }
            }
            lock (PoolLock) {
                if (AnimationPool.ContainsKey (animname) == false) {
					AnimationPool.Add (animname, metaAniData);
                }
            }
			metaAni.Dispose ();
        }
    }

    public AnimatorMetaData Get (string animname) {
		string key = animname;
		DebugUtils.Assert (AnimationPool.ContainsKey (key));
		AnimatorMetaData ret = null;
		AnimationPool.TryGetValue (key, out ret);
		return ret;
	}

	public RuntimeAnimatorController CreateAnimator (string animname) {
		var prefab = RuntimeAnimatorPrefabs.Find (c => c.name == animname);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
		if (prefab == null) {
			Debug.Log ("missing " + animname);
		}
#endif
		return Instantiate (prefab) as RuntimeAnimatorController;
	}
}
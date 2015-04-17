using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LitJson;

public class SpriteTexturePool : MonoBehaviour {
	public static SpriteTexturePool Instance;
	public Dictionary <string, SpriteSheet> TexturePool = new Dictionary<string, SpriteSheet> ();
	Object PoolLock = new Object ();
	public List <string> StreamingTextures = new List<string> ();

	public class SpriteSheet {
		public Sprite Single = null;
		public Dictionary <int, Sprite> Multi = null;
#if UNITY_EDITOR
		public Dictionary <int, Texture2D> PreviewTex = null;
#endif
		public void setSingle (Sprite spr) {
			Single = spr;
		}

		public void add (int idx, Sprite spr) {
			if (Multi == null) {
				Multi = new Dictionary<int, Sprite> ();
			}
			if (Multi.ContainsKey (idx) == false) {
				Multi.Add (idx, spr);
			}
#if UNITY_EDITOR
			if (PreviewTex == null) {
				PreviewTex = new Dictionary <int, Texture2D> ();
			}
			if (PreviewTex.ContainsKey (idx) == false) {
				Texture2D tex = new Texture2D ((int)spr.rect.width, (int)spr.rect.height, spr.texture.format, false);
				for (int x = (int)spr.rect.x; x < spr.rect.x + spr.rect.width; ++x) {
					for (int y = (int)spr.rect.y; y < spr.rect.y + spr.rect.height; ++y) {
						var c = spr.texture.GetPixel (x, y);
						tex.SetPixel (x - (int)spr.rect.x, y - (int)spr.rect.y, c);
					}
				}
				tex.Apply ();
				PreviewTex.Add (idx, tex);
            }
#endif
		}
		public Sprite get (int idx) {
			if (idx == -1) {
				return Single;
			} else {
				Sprite spr = null;
				Multi.TryGetValue (idx, out spr);
				return spr;
			}
		}
#if UNITY_EDITOR
		public Texture2D getPreviewTex (int idx) {
			Texture2D tex = null;
			PreviewTex.TryGetValue (idx, out tex);
			return tex;
		}
#endif
	}

	void Awake () {
		if (Instance == null) {
			DontDestroyOnLoad (gameObject);
			Instance = this;
#if UNITY_EDITOR
			DataInitialized = true;
#else
// if you want to preload all textures please modify scene name below.
//			if (Application.loadedLevelName != "Title") {
//				StartCoroutine (Preload ());
//			}
#endif
		} else if (Instance != this) {
			Destroy (gameObject);
		}
	}

	public bool DataInitialized = false;

	public IEnumerator Preload () {
		DataInitialized = false;
		for (int i = 0; i < StreamingTextures.Count; ++i) {
			yield return StartCoroutine (Create (StreamingTextures [i], false));
		}
		DataInitialized = true;
	}

	public IEnumerator Create (string imageName, bool runtime) {
		while (runtime == true && DataInitialized == false) {
			yield return null;
		}
//		float startTime = Time.time;
        if (string.IsNullOrEmpty (imageName)) {
            Debug.Log ("NULL STRING");
        } else {
            if (TexturePool.ContainsKey (imageName) == false) {
                // raw
                WWW bytes = new WWW (Utils.getStreamingUrlOf (imageName + ".bytes"));
				while (bytes.isDone == false) {
					yield return null;
				}
                // meta - image
				string jsonUrl = Utils.getStreamingUrlOf (imageName + ".json");
                WWW meta = new WWW (jsonUrl);
				while (meta.isDone == false) {
					yield return null;
				}
                string json = meta.text;
                var metaData = JsonMapper.ToObject<TextureMetaData> (json);
#if UNITY_EDITOR
                Texture2D texture = bytes.texture;
#else
				Texture2D texture = bytes.textureNonReadable;
#endif
				texture.filterMode = FilterMode.Bilinear;
				lock (PoolLock) {
					if (TexturePool.ContainsKey (imageName) == false) {
		                if (metaData.SheetDatas == null) { // single
		                    var tex = Sprite.Create (texture, new Rect (0, 0, texture.width, texture.height), metaData.Pivot (), metaData.PixelsPerUnits, 1, SpriteMeshType.Tight, metaData.Border ());
							SpriteSheet sheet = new SpriteSheet ();
							sheet.setSingle (tex);
							TexturePool.Add (imageName, sheet);
		                } else { // multiple
							SpriteSheet sheet = new SpriteSheet ();
		                    for (int i = 0; i < metaData.SheetDatas.Count; ++i) {
		                        var data = metaData.SheetDatas[i];
		                        var tex = Sprite.Create (texture, data.Rect (), data.Pivot (), metaData.PixelsPerUnits, 1, SpriteMeshType.Tight, data.Border ());
								sheet.add (i, tex);
							}
							TexturePool.Add (imageName, sheet);
						}
					}
				}
				bytes.Dispose ();
				meta.Dispose ();
            }
        }
//		Debug.Log (string.Format ("{0}, Elapsed : {1}", Time.time - startTime, imageName));
	}

	public Sprite Get (string imageName, int idx) {
		string key = imageName;
//		Debug.Log (string.Format ("Get {0}_{1}", imageName, idx));
//		DebugUtils.Assert (TexturePool.ContainsKey (key));
		SpriteSheet sheet = null;
		Sprite ret = null;
		if (TexturePool.TryGetValue (key, out sheet)) {
			ret = sheet.get (idx);
		}
		return ret;
	}
}

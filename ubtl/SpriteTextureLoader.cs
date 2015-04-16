using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class TextureMetaData {
	public float PixelsPerUnits;
	public string pivot;
	public string border;
	public List <SpriteSheetData> SheetDatas;

	public TextureMetaData () { }
#if UNITY_EDITOR
	public TextureMetaData (TextureImporter importer) {
		PixelsPerUnits = importer.spritePixelsPerUnit;
		if (importer.spriteImportMode == SpriteImportMode.Single) {
			pivot = importer.spritePivot.ToString ();
			border = importer.spriteBorder.ToString ();
		} else {
			SheetDatas = new List<SpriteSheetData> ();
			for (int i = 0; i < importer.spritesheet.Length; ++i) {
				SpriteSheetData data = new SpriteSheetData (importer.spritesheet [i]);
				SheetDatas.Add (data);
			}
		}
	}
#endif

	public Vector2 Pivot () {
		string[] data = pivot.Split (new char[] {',','(',')'}, System.StringSplitOptions.RemoveEmptyEntries);
		DebugUtils.Assert (data.Length == 2);
		return new Vector2 (float.Parse (data[0]), float.Parse (data[1]));
	}

	public Vector4 Border () {
		string[] data = border.Split (new char[] {',','(',')'}, System.StringSplitOptions.RemoveEmptyEntries);
		DebugUtils.Assert (data.Length == 4);
		return new Vector4 (float.Parse (data[0]), float.Parse (data[1]), float.Parse (data[2]), float.Parse (data[3]));
	}
}

public class SpriteSheetData {
	public string name;
	public string rect;
	public int alignment;
	public string pivot;
	public string border;

	public SpriteSheetData () { }
#if UNITY_EDITOR
	public SpriteSheetData (SpriteMetaData data) {
		name = data.name;
		rect = string.Format ("({0:0.00}, {1:0.00}, {2:0.00}, {3:0.00})", data.rect.x, data.rect.y, data.rect.width, data.rect.height);
		alignment = data.alignment;

		// Center = 0, TopLeft = 1, TopCenter = 2, 
		// TopRight = 3, LeftCenter = 4, RightCenter = 5, 
		// BottomLeft = 6, BottomCenter = 7, BottomRight = 8, Custom = 9.
		Vector2 _pivot = Vector2.zero;
		switch (alignment) {
		case 0:
			_pivot.x = 0.5f;
			_pivot.y = 0.5f;
			break;
		case 1:
			_pivot.x = 0.0f;
			_pivot.y = 1.0f;
			break;
		case 2:
			_pivot.x = 0.5f;
			_pivot.y = 1.0f;
			break;
		case 3:
			_pivot.x = 1.0f;
			_pivot.y = 1.0f;
			break;
		case 4:
			_pivot.x = 0.0f;
			_pivot.y = 0.5f;
			break;
		case 5:
			_pivot.x = 1.0f;
			_pivot.y = 0.5f;
			break;
		case 6:
			_pivot.x = 0.0f;
			_pivot.y = 0.0f;
			break;
		case 7:
			_pivot.x = 0.5f;
			_pivot.y = 0.0f;
			break;
		case 8:
			_pivot.x = 1.0f;
			_pivot.y = 0.0f;
			break;
		case 9:
			_pivot = data.pivot;
			break;
		}
		pivot = _pivot.ToString ();
		border = data.border.ToString ();
	}
#endif

	public Rect Rect () {
		string[] data = rect.Split (new char[] {',','(',')'}, System.StringSplitOptions.RemoveEmptyEntries);
		DebugUtils.Assert (data.Length == 4);
		return new Rect (float.Parse (data[0]), float.Parse (data[1]), float.Parse (data[2]), float.Parse (data[3]));
	}
	
	public Vector2 Pivot () {
		string[] data = pivot.Split (new char[] {',','(',')'}, System.StringSplitOptions.RemoveEmptyEntries);
		DebugUtils.Assert (data.Length == 2);
		return new Vector2 (float.Parse (data[0]), float.Parse (data[1]));
	}
	
	public Vector4 Border () {
		string[] data = border.Split (new char[] {',','(',')'}, System.StringSplitOptions.RemoveEmptyEntries);
		DebugUtils.Assert (data.Length == 4);
		return new Vector4 (float.Parse (data[0]), float.Parse (data[1]), float.Parse (data[2]), float.Parse (data[3]));
	}
}

public class SpriteTextureLoader : MonoBehaviour {
	[ShowOnly]
	public string ImageName;
	[ShowOnly]
	public int ImageIdx = -1;
	public enum SpriteType {
		SprRenderer,
		UUIImage,
	}
	public SpriteType Type = SpriteType.SprRenderer;

	public class LoaderTarget {
		SpriteRenderer Spr;
		Image Img;

		private LoaderTarget () { }
		public LoaderTarget (SpriteRenderer spr) { Spr = spr; }
		public LoaderTarget (Image img) { Img = img; }

		public Sprite Sprite {
			get {
				if (Spr != null) {
					return Spr.sprite;
				}
				if (Img != null) {
					return Img.sprite;
				}
				Debug.Break ();
				return null;
			}
			set {
				if (Spr != null) {
					Spr.sprite = value;
				}
				if (Img != null) {
					Img.sprite = value;
				}
			}
		}

		public Color Color {
			get {
				if (Spr != null) {
					return Spr.color;
				}
				if (Img != null) {
					return Img.color;
				}
				Debug.Break ();
				return Color.white;
			}
			set {
				if (Spr != null) {
					Spr.color = value;
				}
				if (Img != null) {
					Img.color = value;
				}
			}
		}

		public void Begin () {
			if (Spr != null) {
				Spr.enabled = false;
			}
			if (Img != null) {
				Img.enabled = false;
			}
		}

		public void End () {
			if (Spr != null) {
				Spr.enabled = true;
			}
			if (Img != null) {
				Img.enabled = true;
			}
		}
	}
	public LoaderTarget Target = null;
	public bool ColorChangeWhenComplete = false;
	public bool AutomaticLoad = true;

	Color SavedColor = Color.white;

	void OnDisable () {
		if (Target != null && ColorChangeWhenComplete) {
			Target.Color = SavedColor;
		}
	}

	public bool LoadInRuntime = true;

	private IEnumerator LoadSprite () {
		if (ImageName == string.Empty) {
			yield break;
		}
		CreateTarget ();
		DebugUtils.Assert (Target != null);
		SavedColor = Target.Color;
		if (ColorChangeWhenComplete) {
			Target.Color = new Color (SavedColor.r, SavedColor.g, SavedColor.b, 0f);
		}
		Target.Begin ();
		if (SpriteTexturePool.Instance == null) {
			yield return null; // wait for pool creation
		}
		yield return StartCoroutine (SpriteTexturePool.Instance.Create (ImageName, LoadInRuntime));
		Target.Sprite = SpriteTexturePool.Instance.Get (ImageName, ImageIdx);
		Target.End ();

		if (ColorChangeWhenComplete) {
			yield return StartCoroutine (LoadSpriteCompleted ());
		}
        if (SpriteTextureLoaded != null) {
            SpriteTextureLoaded ();
        }
	}

    public delegate void SpriteTextureLoaderEvent ();
    public event SpriteTextureLoaderEvent SpriteTextureLoaded;

	void CreateTarget () {
		if (Type == SpriteType.SprRenderer) {
			Target = new LoaderTarget (GetComponent<SpriteRenderer> ());
		} else {
			Target = new LoaderTarget (GetComponent<Image> ());
		}
	}

	void Awake() {
		CreateTarget ();
	}

	void OnEnable () {
		if (Target.Sprite == null) {
			if (AutomaticLoad) {
				Load ();
			}
		}
	}

	IEnumerator LoadSpriteCompleted () {
		float startTime = Time.time;
		float t = (Time.time - startTime) * 2f;
		while (t <= 1f) {
			Target.Color = new Color (SavedColor.r, SavedColor.g, SavedColor.b, t);
			t = (Time.time - startTime) * 2f;
			yield return null;
		}
		Target.Color = SavedColor;
	}

	public void Load () {
		StartCoroutine (LoadSprite ());
	}

	public void ChangeIndexTo (int index) {
		if (ImageIdx == index) {
			return;
		}
		ImageIdx = index;
		var spr = SpriteTexturePool.Instance.Get (ImageName, index);
		if (spr == null) {
//			DebugUtils.Assert (spr != null);
			return;
		}
		Target.Sprite = spr;
	}

	void Invalidate () {
		if (Target != null) {
			Target.Sprite = null;
			if (gameObject.activeSelf == true) {
				Load ();
			}
		}
	}

	public void ChangeTo (string imageName, int idx) {
		if (ImageName == imageName) {
			if (ImageIdx != idx) {
				ChangeIndexTo (idx);
			}
		} else {
			ImageName = imageName;
			ImageIdx = idx;
			Invalidate ();
		}
	}
}

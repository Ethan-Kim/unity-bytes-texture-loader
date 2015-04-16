#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using LitJson;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public static class EditorToggle {
	public static bool Toggle (bool myToggle, int height, int width) {
		string boxChecked;
		if (myToggle) {
			boxChecked = "O";
		} else {
			boxChecked = "";
		}
		if (GUILayout.Button(boxChecked, GUILayout.Height(height), GUILayout.Width(width))) {
			myToggle = !myToggle;
		}
		return myToggle;
	}
}

[CustomEditor (typeof (SpriteTextureLoader))]
public class SpriteTextureLoaderEditor : Editor {
	string ImageName = string.Empty;
	bool ClearWhenCreate = false;

	class EditorTarget {
		SpriteRenderer Spr;
		Image Img;
		
		private EditorTarget () { }
		public EditorTarget (SpriteRenderer spr) { Spr = spr; }
		public EditorTarget (Image img) { Img = img; }
		
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
	}
	EditorTarget Target = null;

	public override void OnInspectorGUI () {
		DrawDefaultInspector ();
		
		SpriteTextureLoader script = (SpriteTextureLoader)target;
		if (Target == null) {
			if (script.Type == SpriteTextureLoader.SpriteType.SprRenderer) {
				Target = new EditorTarget (script.GetComponent<SpriteRenderer> ());
			} else {
				Target = new EditorTarget (script.GetComponent<Image> ());
			}
		}

		TextureImporter importer = null;
		string assetPath = string.Empty;
		FileInfo info = null;
		if (Target != null) {
			assetPath = AssetDatabase.GetAssetPath (Target.Sprite);
			if (string.IsNullOrEmpty (assetPath) == false) {
				info = new FileInfo (assetPath);
				importer = (TextureImporter)AssetImporter.GetAtPath (assetPath);
			}
		}
		if (GUILayout.Button ("Make ImageName")) {
			if (string.IsNullOrEmpty (assetPath) == false) {
				ImageName = info.Name.Substring (0, info.Name.LastIndexOf ('.'));
				script.ImageName = ImageName;
				var idxString = Target.Sprite.name.Replace (ImageName, string.Empty);
				if (string.IsNullOrEmpty (idxString) == false) { 
					idxString = idxString.Replace ("_", string.Empty);
					script.ImageIdx = int.Parse (idxString);
				}
			}
		}
		if (GUILayout.Button ("Create Meta Data")) {
			if (importer != null && string.IsNullOrEmpty (ImageName) == false) {
				TextureMetaData metaData = new TextureMetaData (importer);
				string datFile = ImageName + ".json";
				string datPath = Application.dataPath + "/StreamingAssets/" + datFile;
                FileInfo metaFileInfo = new FileInfo (datPath);
                if (metaFileInfo.Exists) {
                    using (var sw = new StreamWriter (File.Open (datPath, FileMode.Truncate, FileAccess.Write))) {
                        sw.Write (JsonMapper.ToJson (metaData));
                    }
                } else {
                    using (var sw = new StreamWriter (File.Open (datPath, FileMode.Create, FileAccess.Write))) {
                        sw.Write (JsonMapper.ToJson (metaData));
                    }
                }
				Debug.Log (string.Format ("created meta {0}", datPath));
			}
		}
		EditorGUILayout.BeginHorizontal ();
		GUILayout.Label ("Clear When Create");
		ClearWhenCreate = EditorToggle.Toggle (ClearWhenCreate, 16, 32);
		EditorGUILayout.EndHorizontal ();
		if (GUILayout.Button ("Create .bytes Texture")) {
			if (Target != null && string.IsNullOrEmpty (ImageName) == false) {
				var textureFile = assetPath.Substring (assetPath.IndexOf ('/') + 1); // remove "Assets/"
				var bytesFile = ImageName + ".bytes"; // baz.bytes
				string src = Application.dataPath + "/" + textureFile; //"foo/bar/baz.png"
				string dst = Application.dataPath + "/StreamingAssets/" + bytesFile;
				FileUtil.ReplaceFile (src, dst); // copy aaa.png -> aaa.bytes
				Debug.Log (string.Format ("created bytes {0}", dst));
			}
			if (ClearWhenCreate) {
				script.ImageName = string.Empty;
				script.ImageIdx = -1;
				ImageName = string.Empty;
			}
		}
		if (GUILayout.Button ("Clear")) {
			script.ImageName = string.Empty;
			script.ImageIdx = -1;
			ImageName = string.Empty;
		}
		if (GUILayout.Button ("Finish")) {
			Target.Sprite = null;
		}
	}

	[MenuItem("Assets/Create .bytes Texture")]
	private static void CreateBytesTextureOnAssetWindow () {
		for (int i = 0; i < Selection.objects.Length; ++i) {
			var selected = Selection.objects [i];
			var assetPath = AssetDatabase.GetAssetPath (selected); //"foo/bar/baz.png"
			FileInfo info = new FileInfo (assetPath); // for baz.png
			var textureFile = assetPath.Substring (assetPath.IndexOf ('/') + 1); // remove "Assets/"
			var bytesFile = info.Name.Substring (0, info.Name.LastIndexOf ('.')) + ".bytes"; // baz.bytes
			string src = Application.dataPath + "/" + textureFile; //"foo/bar/baz.png"
			string dst = Application.dataPath + "/StreamingAssets/" + bytesFile;
			FileUtil.ReplaceFile (src, dst); // copy aaa.png -> aaa.bytes

			var importer = (TextureImporter)AssetImporter.GetAtPath (assetPath);
			var metaData = new TextureMetaData (importer);
			var metaFile = info.Name.Substring (0, info.Name.LastIndexOf ('.')) + ".json";
			string jsonPath = Application.dataPath + "/StreamingAssets/" + metaFile;
			var metaFileInfo = new FileInfo (jsonPath);
			if (metaFileInfo.Exists) {
				using (var sw = new StreamWriter (File.Open (jsonPath, FileMode.Truncate, FileAccess.Write))) {
					sw.Write (JsonMapper.ToJson (metaData));
				}
			} else {
				using (var sw = new StreamWriter (File.Open (jsonPath, FileMode.Create, FileAccess.Write))) {
					sw.Write (JsonMapper.ToJson (metaData));
				}
			}
			Debug.Log (string.Format ("created bytes {0}", dst));
			Debug.Log (string.Format ("created meta {0}", jsonPath));
		}
	}

	[MenuItem("Assets/Create .bytes Texture", true)]
	private static bool ValidateCreateBytesTextureOnAssetWindow () {
		if (Selection.objects.Length <= 0) {
			return false;
		}
		for (int i = 0; i < Selection.objects.Length; ++i) {
			if (Selection.objects [i].GetType () != typeof(Texture2D)) {
				return false;
			}
		}
		return true;
	}
}

#endif
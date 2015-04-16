#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;

[CustomEditor (typeof (SpriteTexturePool))]
public class SpriteTexturePoolEditor : Editor {

	public override void OnInspectorGUI () {
		DrawDefaultInspector ();
		
		SpriteTexturePool script = (SpriteTexturePool)target;

		if (GUILayout.Button ("Make Streaming Texture List")) {
			script.StreamingTextures.Clear ();
			DirectoryInfo info = new DirectoryInfo (Application.streamingAssetsPath);
			foreach (var f in info.GetFiles ()) {
				if (f.Extension == ".bytes") {
					script.StreamingTextures.Add (Path.GetFileNameWithoutExtension (f.Name));
				}
			}
			script.StreamingTextures.Sort ();
		}

		EditorGUILayout.BeginHorizontal ();
		GUILayout.Label ("Count");
		GUILayout.Label (script.TexturePool.Count.ToString ());
		EditorGUILayout.EndHorizontal ();

		foreach (var p in script.TexturePool) {
			GUILayout.Label (p.Key);
			if (p.Value.Multi == null) {
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label (p.Value.Single.texture, GUILayout.MinHeight(64), GUILayout.MinWidth(64));
				EditorGUILayout.EndHorizontal ();
			} else {
				EditorGUILayout.BeginHorizontal ();
				foreach (var m in p.Value.Multi) {
					GUILayout.Label (m.Key.ToString ());
					GUILayout.Label (p.Value.getPreviewTex (m.Key), GUILayout.MinHeight(64), GUILayout.MinWidth(64));
				}
				EditorGUILayout.EndHorizontal ();
			}
		}
	}
}
#endif
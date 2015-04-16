#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

[CustomEditor (typeof(SpriteAnimationPool))]
public class SpriteAnimationPoolEditor : Editor {
	
	public override void OnInspectorGUI () {
		DrawDefaultInspector ();
		
		SpriteAnimationPool script = (SpriteAnimationPool)target;
		
		if (GUILayout.Button ("Make Streaming Animator List")) {
			script.StreamingAnimators.Clear ();
			DirectoryInfo info = new DirectoryInfo (Application.streamingAssetsPath);
			foreach (var f in info.GetFiles ()) {
				if (f.Name.StartsWith ("Anim_") && f.Extension == ".json" ) {
					var json = Path.GetFileNameWithoutExtension (f.Name);
					script.StreamingAnimators.Add (json.Remove (0, 5));
				}
			}
			script.StreamingAnimators.Sort ();
		}

		foreach (var kvp in script.AnimationPool) {
			GUILayout.BeginHorizontal ();
			GUILayout.Label (kvp.Key);
			GUILayout.EndHorizontal ();
		}
	}
}
#endif
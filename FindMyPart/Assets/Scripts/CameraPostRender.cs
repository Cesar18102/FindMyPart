using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class CameraPostRender : MonoBehaviour {
    private Camera _Camera { get; set; }
    private Shader _Shader { get; set; }

	// Use this for initialization
	void Start () {
        _Camera = this.GetComponent<Camera>();
        _Shader = Shader.Find("Custom/ColoredWatchShader");

        Shader.SetGlobalColor(Shader.PropertyToID("_Light"), new Color(1.0f, 0.0f, 0.0f, 0.1f));
        _Camera.SetReplacementShader(_Shader, null);
	}
	
	// Update is called once per frame
	void Update () {
        
	}
}

using UnityEngine;

public class CameraPostRender : MonoBehaviour {
    private Camera _Camera { get; set; }
    private Shader _Shader { get; set; }

	// Use this for initialization
	void Start () {
        _Camera = this.GetComponent<Camera>();
        _Shader = Shader.Find("Custom/ColoredWatchShader");
        
        Shader.SetGlobalColor(Shader.PropertyToID("_Light"), Color.white);
        _Camera.SetReplacementShader(_Shader, null);
	}
	
	// Update is called once per frame
	void Update () {
        
	}

    public void SetColor(Color? color)
    {
        Shader.SetGlobalColor(Shader.PropertyToID("_Light"), color.HasValue ? color.Value : Color.white);
    }
}

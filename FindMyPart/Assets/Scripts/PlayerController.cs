using System;
using UnityEngine;

public class PlayerController : MonoBehaviour 
{
    public float FORWARD_SPEED = 500f;
    public float ROTATE_SPEED = 50f;

    public bool IsIdle = true;
    public bool IsGoing = false;
    public bool IsRotating = false;

    public Color? CurrentColor { get; set; }

    private Animator _Animator { get; set; }
    private CameraPostRender _CameraPostRender { get; set; }

    public event EventHandler<EventArgs> OnColorChanged;
    public event EventHandler<EventArgs> TryTakeDetail;
    public event EventHandler<EventArgs> OnMoved;
    public event EventHandler<EventArgs> OnStopped;

    // Use this for initialization
    void Start () 
    {
        this._Animator = this.GetComponent<Animator>();
        this._CameraPostRender = this.GetComponentInChildren<CameraPostRender>();
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (Input.GetKeyDown(KeyCode.R))
            this.SetColor(Color.red);
        else if (Input.GetKeyDown(KeyCode.G))
            this.SetColor(Color.green);
        else if (Input.GetKeyDown(KeyCode.B))
            this.SetColor(Color.blue);

        if (Input.GetKeyDown(KeyCode.E))
            this.TryTakeDetail?.Invoke(this, new EventArgs());

        float move = Input.GetAxis("Vertical");
        float rotate = Input.GetAxis("Horizontal");

        if ((move != 0 && rotate != 0) || (move == 0 && rotate == 0))
        {
            this.SetMoveState(true, false, false);
            this.OnStopped?.Invoke(this, new EventArgs());
            return;
        }

        if (move != 0)
        {
            this.SetMoveState(false, true, false);

            Vector3 moveVector = this.gameObject.transform.forward.normalized * move * Time.deltaTime * FORWARD_SPEED;
            this.gameObject.transform.position = this.gameObject.transform.position + moveVector;
        }

        if(rotate != 0)
        {
            this.SetMoveState(false, false, true);
            this.gameObject.transform.forward = Quaternion.AngleAxis(rotate * Time.deltaTime * ROTATE_SPEED, Vector3.up) * this.gameObject.transform.forward;
        }

        this.OnMoved?.Invoke(this, new EventArgs());
    }

    private void SetMoveState(bool isIdle, bool isGoing, bool isRotating)
    {
        IsIdle = isIdle;
        IsGoing = isGoing;
        IsRotating = isRotating;

        this._Animator.SetBool("IsGoing", this.IsGoing);
        this._Animator.SetBool("IsRotating", this.IsRotating);
    }

    private void SetColor(Color color)
    {
        if(this.CurrentColor == null || this.CurrentColor != color)
        {
            this._CameraPostRender.SetColor(color);
            this.CurrentColor = color;

            this.OnColorChanged?.Invoke(this, new EventArgs());
        }
        else if(this.CurrentColor == color)
        {
            this._CameraPostRender.SetColor(null);
            this.CurrentColor = null;

            this.OnColorChanged?.Invoke(this, new EventArgs());
        }
    }
}

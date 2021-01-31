using UnityEngine;

public class PlayerController : MonoBehaviour 
{
    public float FORWARD_SPEED = 5f;
    public float ROTATE_SPEED = 25f;

    public bool IsIdle = true;
    public bool IsGoing = false;
    public bool IsRotating = false;

    private Animator _Animator { get; set; }

    // Use this for initialization
    void Start () 
    {
        this._Animator = this.GetComponent<Animator>();
    }
	
	// Update is called once per frame
	void Update ()
    {
        float move = Input.GetAxis("Vertical");
        float rotate = Input.GetAxis("Horizontal");

        if ((move != 0 && rotate != 0) || (move == 0 && rotate == 0))
        {
            this.SetMoveState(true, false, false);
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
    }

    private void SetMoveState(bool isIdle, bool isGoing, bool isRotating)
    {
        IsIdle = isIdle;
        IsGoing = isGoing;
        IsRotating = isRotating;

        this._Animator.SetBool("IsGoing", this.IsGoing);
        this._Animator.SetBool("IsRotating", this.IsRotating);
    }
}

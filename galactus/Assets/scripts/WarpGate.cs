using UnityEngine;
using System.Collections;

public class WarpGate : MonoBehaviour {
    // TODO warping should cost energy. like, radius * distance * teleportationConstant or something.
    private float jumpButtonHeld = 0;

    public PlayerForce owner;
    public Transform direction;
    public ParticleSystem endPoint;
    ParticleSystem startPoint;
    public UnityEngine.UI.Text text;

    public void Setup(PlayerForce owner, Transform direction) {
        this.owner = owner;
        this.direction = direction;
        if (text) { text.color = owner.GetResourceEater().GetCurrentColor(); }
    }

    void Start() {
        startPoint = GetComponent<ParticleSystem>();
        startPoint.EnableEmission(false);
        endPoint.EnableEmission(false);
    }

    public void UpdateKeypress () {
        if (Input.GetButton("Jump")) {
            if (Input.GetButtonDown("Jump")) {
                jumpButtonHeld = 0;
                startPoint.EnableEmission(true);
                endPoint.EnableEmission(true);
                if (text) text.enabled = true;
                Vector3 s = owner.transform.lossyScale;
                startPoint.transform.localScale = s;
                endPoint.transform.localScale = s;
                //text.transform.localScale = new Vector3(1 / s.x, 1 / s.y, 1 / s.z);
            }
            jumpButtonHeld += Time.deltaTime;
            float emit = 20 + jumpButtonHeld;
            startPoint.SetEmissionRate(emit);
            endPoint.SetEmissionRate(emit);
        }
        if (Input.GetButtonUp("Jump")) {
            owner.transform.position = endPoint.transform.position;
            jumpButtonHeld = 0;
            startPoint.EnableEmission(false);
            endPoint.EnableEmission(false);
            if (text) text.enabled = false;
        }
    }

    void LateUpdate() {
        if (jumpButtonHeld > 0) {
            float d = owner.GetResourceEater().GetRadius() * jumpButtonHeld;
            startPoint.transform.position = owner.transform.position;
            startPoint.transform.rotation = direction.rotation;
            endPoint.transform.position = owner.transform.position + direction.forward * d;
            endPoint.transform.rotation = direction.rotation;
            if (text) {
                text.transform.position = endPoint.transform.position;
                text.transform.rotation = endPoint.transform.rotation;
                float s = 0.01f;
                s *= d / 2.0f;
                text.transform.localScale = new Vector3(s, s, s);
                text.text = "\n"+((int)(jumpButtonHeld * d)).ToString();
            }
        }
    }
}

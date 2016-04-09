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

    public static float CalculateRate(float mass) {
        float massScore = (mass - World.TELEPORT_IDEAL_SIZE);
        float easeScore = (World.TELEPORT_VIABILITY_RANGE * World.TELEPORT_VIABILITY_RANGE);
        return (massScore * massScore) / easeScore + (1/World.TELEPORT_MINIMUM_COST_EFFICIENCY);
    }

    public float CalculateTeleportRate() { return CalculateRate(owner.GetResourceEater().GetMass()); }

    public void DoJump() {
        ResourceEater re = owner.GetResourceEater();
        //int count = (int)re.GetRadius();
        float cost = CalculateRate(re.GetMass());
        cost *= Vector3.Distance(startPoint.transform.position, endPoint.transform.position);
        re.Eject(false, 1, cost, null, 0, -1);
        owner.transform.position = endPoint.transform.position;
        jumpButtonHeld = 0;
        startPoint.EnableEmission(false);
        endPoint.EnableEmission(false);
        if (text) text.enabled = false;
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
            DoJump();
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
                text.text = "\n\n\n\n\n"+((int)d).ToString();
            }
        }
    }
}

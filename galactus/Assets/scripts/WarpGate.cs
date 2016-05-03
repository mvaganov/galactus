using UnityEngine;
using System.Collections;

public class WarpGate : MonoBehaviour {
    // TODO warping should cost energy. like, radius * distance * teleportationConstant or something.
    private float jumpButtonHeld = 0;
    private float distance;
    bool jumping = false;

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

    public float CalculateCost(float distance)
    {
        ResourceEater re = owner.GetResourceEater();
        float m = re.GetMass();
        float increment;
        float totalCost = 0;
        float distanceCalculated = 0;
        for(int i = 0; i < distance; ++i) {
            increment = CalculateRate(m);
            m -= increment;
            totalCost += increment;
            distanceCalculated += 1;
        }
        increment = CalculateRate(m);
        totalCost += (distance - distanceCalculated) * increment;
        return totalCost;
    }

    public float CalculateTeleportRate() { return CalculateRate(owner.GetResourceEater().GetMass()); }

    public void DoJump() {
        ResourceEater re = owner.GetResourceEater();
        //int count = (int)re.GetRadius();
        //float cost = CalculateRate(re.GetMass());
        //cost *= Vector3.Distance(startPoint.transform.position, endPoint.transform.position);
        float cost = CalculateCost(distance);
        re.Eject(false, 1, cost, null, 0, -1);
        owner.transform.position = endPoint.transform.position;
    }

    public bool UpdateKeypress () {
        if (Input.GetButton("Jump")) {
            if (!jumping) {
                jumping = true;
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
            return true;
        } else {
            if (Input.GetKey(KeyCode.Backspace) || Input.GetButton("Fire2")) {
                jumpButtonHeld -= Time.deltaTime;
                if (jumpButtonHeld < 0) { CancelWarp(); } else {
                    float emit = 20 + jumpButtonHeld;
                    startPoint.SetEmissionRate(emit);
                    endPoint.SetEmissionRate(emit);
                }
                return true;
            } else if (Input.GetButton("Fire1")) {
                return true; // prevents shoot from happening at the same time.
            } else if (Input.GetButtonUp("Fire1")) {
                DoJump();
                CancelWarp();
                return true;
            }
        }
        return false;
    }

    public void CancelWarp() {
        jumping = false;
        jumpButtonHeld = 0;
        startPoint.EnableEmission(false);
        endPoint.EnableEmission(false);
        if (text) text.enabled = false;
    }

    void LateUpdate() {
        if (jumpButtonHeld > 0) {
            ResourceEater re = owner.GetResourceEater();
            distance = re.GetRadius() * jumpButtonHeld;
            startPoint.transform.position = owner.transform.position;
            startPoint.transform.rotation = direction.rotation;
            endPoint.transform.position = owner.transform.position + direction.forward * distance;
            endPoint.transform.rotation = direction.rotation;
            if (text) {
                text.transform.position = endPoint.transform.position;
                text.transform.rotation = endPoint.transform.rotation;
                float s = 0.01f;
                s *= distance / 2.0f;
                text.transform.localScale = new Vector3(s, s, s);
                float cost = CalculateCost(distance);
                text.text = "\n\n\n\n\n"+((int)distance).ToString()+"\nnew mass:"+((int)re.GetMass()-cost);
            }
        }
    }
}
